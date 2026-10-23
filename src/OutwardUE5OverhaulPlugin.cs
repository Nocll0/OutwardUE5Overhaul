using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using OutwardUE5Overhaul.Presets;
using OutwardUE5Overhaul.Pipeline;
using OutwardUE5Overhaul.UI;

namespace OutwardUE5Overhaul
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class OutwardUE5OverhaulPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.argon.outward.ue5overhaul";
        public const string NAME = "Outward UE5 Graphics Overhaul (2026 Edition)";
        public const string VERSION = "1.6.2";

        public static OutwardUE5OverhaulPlugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        // Config Entries (Also visible in Outward Config Manager F5)
        private ConfigEntry<UE5PresetType> _cfgPreset;
        private ConfigEntry<KeyCode> _cfgStudioHotkey;
        private ConfigEntry<KeyCode> _cfgABToggleHotkey;
        private ConfigEntry<float> _cfgExposureOffset;
        private ConfigEntry<float> _cfgLightRangeMult;
        private ConfigEntry<float> _cfgIndoorAmbientBoost;
        private ConfigEntry<float> _cfgShadowDistance;
        private ConfigEntry<float> _cfgTextureMipBias;
        private ConfigEntry<bool> _cfgEnableDoF;
        private ConfigEntry<bool> _cfgEnableMotionBlur;
        private ConfigEntry<bool> _cfgBorderlessAltTabProtection;

        // Active Runtime State
        private UE5PresetType _currentPresetType = UE5PresetType.UE5_Lumen_Nanite_2026;
        private UE5PresetType _lastNonVanillaPreset = UE5PresetType.UE5_Lumen_Nanite_2026;
        private UE5RenderProfile _activeProfile;
        private bool _isOverhaulEnabled = true;

        // Engines
        public LumenGIEngine LumenEngine { get; private set; }
        public VSMShadowAndAOEngine ShadowAOEngine { get; private set; }
        public UE5PostProcessEngine PostProcessEngine { get; private set; }
        public VolumetricSkyAndWaterEngine SkyWaterEngine { get; private set; }
        public NaniteAndPBRMaterialEngine NanitePBREngine { get; private set; }
        public UE5StudioWindow StudioUI { get; private set; }

        private Harmony _harmony;
        private bool _wasLoadingDone = false;
        private bool _wasInInterior = false;
        private bool _isAppFocused = true;
        private float _focusSafeTime = 0f;

        public UE5RenderProfile ActiveProfile
        {
            get { return _activeProfile; }
        }

        public UE5PresetType CurrentPresetType
        {
            get { return _currentPresetType; }
        }

        public bool IsOverhaulEnabled
        {
            get { return _isOverhaulEnabled && _currentPresetType != UE5PresetType.Vanilla_Original; }
        }

        public bool IsD3D11FocusSafe
        {
            get { return _isAppFocused && Application.isFocused && Time.unscaledTime >= _focusSafeTime; }
        }

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            InitConfig();
            EnsureBorderlessFullscreenSafeMode();

            LumenEngine = new LumenGIEngine();
            ShadowAOEngine = new VSMShadowAndAOEngine();
            PostProcessEngine = new UE5PostProcessEngine();
            SkyWaterEngine = new VolumetricSkyAndWaterEngine();
            NanitePBREngine = new NaniteAndPBRMaterialEngine();
            StudioUI = new UE5StudioWindow();

            _currentPresetType = _cfgPreset.Value;
            if (_currentPresetType != UE5PresetType.Vanilla_Original)
            {
                _lastNonVanillaPreset = _currentPresetType;
            }
            _activeProfile = UE5VisualPresets.GetPreset(_currentPresetType);
            SyncConfigOverridesToProfile();

            _harmony = new Harmony(GUID);
            ApplyHarmonyPatches();

            SceneManager.sceneLoaded += OnSceneLoaded;

            Log.LogInfo(string.Format("[{0} v{1}] Initialized! Active Preset: {2}. Press {3} for UE5 Studio UI, {4} for Instant Vanilla A/B Toggle.",
                NAME, VERSION, _activeProfile.Name, _cfgStudioHotkey.Value, _cfgABToggleHotkey.Value));
        }

        private void InitConfig()
        {
            _cfgPreset = Config.Bind("1. Preset & Controls", "VisualPreset", UE5PresetType.UE5_Lumen_Nanite_2026,
                "Select the overall Unreal Engine 5 (2026) visual preset.");
            _cfgStudioHotkey = Config.Bind("1. Preset & Controls", "StudioUIHotkey", KeyCode.F10,
                "Hotkey to open/close the in-game Unreal Engine 5 Graphics Studio live tuning window.");
            _cfgABToggleHotkey = Config.Bind("1. Preset & Controls", "InstantVanillaABToggleHotkey", KeyCode.F11,
                "Hotkey to instantly toggle between UE5 2026 Overhaul and Vanilla graphics mid-game.");
            _cfgBorderlessAltTabProtection = Config.Bind("1. Preset & Controls", "BorderlessAltTabProtection", true,
                "Automatically use DXGI Flip-Model Borderless Fullscreen (FullScreenWindow) instead of ExclusiveFullScreen to prevent D3D11 driver crashes when Alt-Tabbing or pressing Print Screen.");

            _cfgExposureOffset = Config.Bind("2. Quick Tuning", "PostExposureOffset", 0.15f,
                new ConfigDescription("ACES Filmic Post-Exposure offset (EV).", new AcceptableValueRange<float>(-1.0f, 1.2f)));
            _cfgLightRangeMult = Config.Bind("2. Quick Tuning", "PointLightRangeMultiplier", 1.65f,
                new ConfigDescription("Point, Lava & Spot Light Range Multiplier (Light Carry across valleys, interiors, and streets).", new AcceptableValueRange<float>(1.0f, 2.5f)));
            _cfgIndoorAmbientBoost = Config.Bind("2. Quick Tuning", "IndoorAmbientBoost", 0.08f,
                new ConfigDescription("UE5 Lumen Indoor Room Ambient GI Boost.", new AcceptableValueRange<float>(0.0f, 0.70f)));
            _cfgShadowDistance = Config.Bind("2. Quick Tuning", "ShadowDistance", 280f,
                new ConfigDescription("4-Cascade Directional Shadow Distance (meters).", new AcceptableValueRange<float>(100f, 450f)));
            _cfgTextureMipBias = Config.Bind("2. Quick Tuning", "TextureMipMapBias", -0.32f,
                new ConfigDescription("World Texture MipMap Bias (-0.32 = 4K Neural Ray-Reconstruction Clarity, 0.0 = Vanilla).", new AcceptableValueRange<float>(-1.0f, 0.0f)));
            _cfgEnableDoF = Config.Bind("2. Quick Tuning", "EnableDynamicAutofocusDoF", false,
                "Enable Dynamic Raycast Autofocus Physical Bokeh Depth of Field.");
            _cfgEnableMotionBlur = Config.Bind("2. Quick Tuning", "EnableMotionBlur", false,
                "Enable Per-Frame Shutter Camera Motion Blur.");

            _cfgPreset.SettingChanged += delegate { LoadPreset(_cfgPreset.Value); };
            _cfgExposureOffset.SettingChanged += delegate { if (_activeProfile != null) { _activeProfile.PostExposureOffset = _cfgExposureOffset.Value; ApplyAllGraphicsImmediate(false); } };
            _cfgLightRangeMult.SettingChanged += delegate { if (_activeProfile != null) { _activeProfile.PointLightRangeMultiplier = _cfgLightRangeMult.Value; ApplyAllGraphicsImmediate(false); } };
            _cfgIndoorAmbientBoost.SettingChanged += delegate { if (_activeProfile != null) { _activeProfile.IndoorAmbientBoost = _cfgIndoorAmbientBoost.Value; ApplyAllGraphicsImmediate(false); } };
            _cfgShadowDistance.SettingChanged += delegate { if (_activeProfile != null) { _activeProfile.ShadowDistance = _cfgShadowDistance.Value; ApplyAllGraphicsImmediate(false); } };
            _cfgTextureMipBias.SettingChanged += delegate { if (_activeProfile != null) { _activeProfile.TextureMipMapBias = _cfgTextureMipBias.Value; ApplyAllGraphicsImmediate(true); } };
            _cfgEnableDoF.SettingChanged += delegate { if (_activeProfile != null) { _activeProfile.EnableDynamicAutofocusDoF = _cfgEnableDoF.Value; ApplyAllGraphicsImmediate(false); } };
            _cfgEnableMotionBlur.SettingChanged += delegate { if (_activeProfile != null) { _activeProfile.EnableMotionBlur = _cfgEnableMotionBlur.Value; ApplyAllGraphicsImmediate(false); } };
        }

        private void EnsureBorderlessFullscreenSafeMode()
        {
            if (_cfgBorderlessAltTabProtection != null && _cfgBorderlessAltTabProtection.Value)
            {
                if (Screen.fullScreen && Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
                {
                    Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _isAppFocused = hasFocus;
            if (hasFocus)
            {
                // Give D3D11 swapchain 0.5s after Alt-Tab return before touching render targets or LUTs
                _focusSafeTime = Time.unscaledTime + 0.5f;
                EnsureBorderlessFullscreenSafeMode();
            }
        }

        private void SyncConfigOverridesToProfile()
        {
            if (_activeProfile == null || _currentPresetType == UE5PresetType.Vanilla_Original) return;
            _activeProfile.PostExposureOffset = _cfgExposureOffset.Value;
            _activeProfile.PointLightRangeMultiplier = _cfgLightRangeMult.Value;
            _activeProfile.IndoorAmbientBoost = _cfgIndoorAmbientBoost.Value;
            _activeProfile.ShadowDistance = _cfgShadowDistance.Value;
            _activeProfile.TextureMipMapBias = _cfgTextureMipBias.Value;
            _activeProfile.EnableDynamicAutofocusDoF = _cfgEnableDoF.Value;
            _activeProfile.EnableMotionBlur = _cfgEnableMotionBlur.Value;
        }

        public void SaveProfileToConfig()
        {
            if (_activeProfile == null) return;
            _cfgPreset.Value = _currentPresetType;
            _cfgExposureOffset.Value = _activeProfile.PostExposureOffset;
            _cfgLightRangeMult.Value = _activeProfile.PointLightRangeMultiplier;
            _cfgIndoorAmbientBoost.Value = _activeProfile.IndoorAmbientBoost;
            _cfgShadowDistance.Value = _activeProfile.ShadowDistance;
            _cfgTextureMipBias.Value = _activeProfile.TextureMipMapBias;
            _cfgEnableDoF.Value = _activeProfile.EnableDynamicAutofocusDoF;
            _cfgEnableMotionBlur.Value = _activeProfile.EnableMotionBlur;
            Config.Save();
        }

        public void LoadPreset(UE5PresetType preset)
        {
            _currentPresetType = preset;
            if (preset != UE5PresetType.Vanilla_Original)
            {
                _lastNonVanillaPreset = preset;
                _isOverhaulEnabled = true;
            }
            else
            {
                _isOverhaulEnabled = false;
            }

            _activeProfile = UE5VisualPresets.GetPreset(preset);
            ApplyAllGraphicsImmediate(true);

            if (StudioUI != null)
            {
                StudioUI.ShowToast(string.Format("UE5 Preset Loaded: {0}  [{1}: Studio UI | {2}: A/B Toggle]",
                    _activeProfile.Name, _cfgStudioHotkey.Value, _cfgABToggleHotkey.Value));
            }
        }

        public void ToggleVanillaABComparison()
        {
            if (_currentPresetType != UE5PresetType.Vanilla_Original)
            {
                _lastNonVanillaPreset = _currentPresetType;
                LoadPreset(UE5PresetType.Vanilla_Original);
            }
            else
            {
                LoadPreset(_lastNonVanillaPreset);
            }
        }

        private void Update()
        {
            if (!IsD3D11FocusSafe) return;

            if (StudioUI != null)
            {
                StudioUI.UpdateMetrics();
            }

            if (Input.GetKeyDown(_cfgStudioHotkey.Value))
            {
                StudioUI.ToggleWindow();
            }

            if (Input.GetKeyDown(_cfgABToggleHotkey.Value))
            {
                ToggleVanillaABComparison();
            }

            // Detect when Outward's NetworkLevelLoader finishes streaming an area/dungeon
            if (NetworkLevelLoader.Instance != null)
            {
                bool loadingDone = NetworkLevelLoader.Instance.IsOverallLoadingDone;
                if (loadingDone && !_wasLoadingDone)
                {
                    _wasLoadingDone = true;
                    StartCoroutine(DelayedFullSceneOverhaulRoutine(0.6f));
                }
                else if (!loadingDone)
                {
                    _wasLoadingDone = false;
                }
            }

            // Detect indoor/outdoor transitions within the same scene (e.g., entering/exiting Berg Inn or houses)
            Camera mainCam = LumenGIEngine.GetActiveGameplayCamera();
            if (mainCam != null && _activeProfile != null)
            {
                bool isInInterior = LumenGIEngine.IsCameraInInterior(mainCam);
                if (isInInterior != _wasInInterior)
                {
                    _wasInInterior = isInInterior;
                    LumenEngine.ApplyAtmosphereGI(_activeProfile);
                    RefreshAllActiveCameras(_activeProfile);
                }
            }

            // Real-time Dynamic Autofocus Bokeh DoF (only if explicitly enabled by user)
            if (_activeProfile != null && _activeProfile.EnableDynamicAutofocusDoF)
            {
                PostProcessEngine.UpdateDynamicAutofocus(_activeProfile);
            }
        }

        private void OnGUI()
        {
            if (!IsD3D11FocusSafe) return;

            if (StudioUI != null)
            {
                StudioUI.OnGUI(this);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureBorderlessFullscreenSafeMode();
            StartCoroutine(DelayedFullSceneOverhaulRoutine(0.5f));
            StartCoroutine(DelayedFullSceneOverhaulRoutine(2.0f));
        }

        private IEnumerator DelayedFullSceneOverhaulRoutine(float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
            while (!IsD3D11FocusSafe)
            {
                yield return null;
            }
            ApplyAllGraphicsImmediate(true);
        }

        public void ApplyAllGraphicsImmediate(bool includeHeavyMaterialScan)
        {
            if (_activeProfile == null || !IsD3D11FocusSafe) return;

            try
            {
                EnsureBorderlessFullscreenSafeMode();
                ShadowAOEngine.ApplyGlobalShadowSettings(_activeProfile);
                SkyWaterEngine.ApplySkyAndAtmosphere(_activeProfile);
                LumenEngine.ApplyAtmosphereGI(_activeProfile);
                LumenEngine.ApplySceneLightsOverhaul(_activeProfile);
                NanitePBREngine.ApplyNaniteWorldAndFoliage(_activeProfile);
                SkyWaterEngine.ApplyWaterOverhaul(_activeProfile);
                RefreshAllActiveCameras(_activeProfile);

                if (includeHeavyMaterialScan)
                {
                    NanitePBREngine.ApplyPBRMaterialsAndTextures(_activeProfile);
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning("[OutwardUE5Overhaul] ApplyAllGraphicsImmediate warning: " + ex.Message);
            }
        }

        public void RefreshAllActiveCameras(UE5RenderProfile profile)
        {
            if (profile == null || !IsD3D11FocusSafe) return;

            CameraQuality[] cameras = UnityEngine.Object.FindObjectsOfType<CameraQuality>();
            if (cameras == null) return;

            for (int i = 0; i < cameras.Length; i++)
            {
                CameraQuality cq = cameras[i];
                if (cq == null) continue;
                ApplySingleCameraOverhaul(cq, profile);
            }
        }

        public void ApplySingleCameraOverhaul(CameraQuality cq, UE5RenderProfile profile)
        {
            if (cq == null || profile == null || !IsD3D11FocusSafe) return;
            ShadowAOEngine.ApplyCameraShadowAndAO(cq, profile);
            PostProcessEngine.ApplyCameraPostProcessing(cq, profile);
            SkyWaterEngine.ApplyCameraVolumetricsAndSSR(cq, profile);
        }

        private void ApplyHarmonyPatches()
        {
            try
            {
                BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

                // 1. CameraQuality.Start, RefreshQuality, RefreshBrightness, DelayedEnabledSSShadows
                Type cqType = typeof(CameraQuality);
                PatchMethod(cqType.GetMethod("Start", flags), null, typeof(OutwardUE5OverhaulPlugin).GetMethod("Postfix_CameraQuality_Refresh", flags));
                PatchMethod(cqType.GetMethod("RefreshQuality", flags), null, typeof(OutwardUE5OverhaulPlugin).GetMethod("Postfix_CameraQuality_Refresh", flags));
                PatchMethod(cqType.GetMethod("RefreshBrightness", flags), null, typeof(OutwardUE5OverhaulPlugin).GetMethod("Postfix_CameraQuality_Refresh", flags));
                PatchMethod(cqType.GetMethod("DelayedEnabledSSShadows", flags), null, typeof(OutwardUE5OverhaulPlugin).GetMethod("Postfix_CameraQuality_Refresh", flags));

                // 2. EnvironmentConditions.UpdateAtmosphere
                Type envType = typeof(EnvironmentConditions);
                PatchMethod(envType.GetMethod("UpdateAtmosphere", flags), null, typeof(OutwardUE5OverhaulPlugin).GetMethod("Postfix_EnvironmentConditions_UpdateAtmosphere", flags));

                // 3. LightQuality.Start & InitLightSettings (for extended Light Range Carry and fixing -1 vertical culling)
                PatchMethod(typeof(LightQuality).GetMethod("Start", flags), null, typeof(OutwardUE5OverhaulPlugin).GetMethod("Postfix_LightQuality_InitLightSettings", flags));
                PatchMethod(typeof(LightQuality).GetMethod("InitLightSettings", flags), null, typeof(OutwardUE5OverhaulPlugin).GetMethod("Postfix_LightQuality_InitLightSettings", flags));

                // 4. TerrainQualityManager.ProcessQuality & TerrainMeshQualityManager.ProcessQuality & WaterQuality.ProcessQuality
                PatchMethod(typeof(TerrainQualityManager).GetMethod("ProcessQuality", flags), null, typeof(OutwardUE5OverhaulPlugin).GetMethod("Postfix_TerrainQuality_ProcessQuality", flags));
                PatchMethod(typeof(TerrainMeshQualityManager).GetMethod("ProcessQuality", flags), null, typeof(OutwardUE5OverhaulPlugin).GetMethod("Postfix_TerrainQuality_ProcessQuality", flags));
                PatchMethod(typeof(WaterQuality).GetMethod("ProcessQuality", flags), null, typeof(OutwardUE5OverhaulPlugin).GetMethod("Postfix_WaterQuality_ProcessQuality", flags));
            }
            catch (Exception ex)
            {
                Log.LogError("[OutwardUE5Overhaul] Failed applying Harmony patches: " + ex);
            }
        }

        private void PatchMethod(MethodInfo target, MethodInfo prefix, MethodInfo postfix)
        {
            if (target == null) return;
            HarmonyMethod hmPre = (prefix != null) ? new HarmonyMethod(prefix) : null;
            HarmonyMethod hmPost = (postfix != null) ? new HarmonyMethod(postfix) : null;
            _harmony.Patch(target, hmPre, hmPost);
        }

        private static void Postfix_CameraQuality_Refresh(CameraQuality __instance)
        {
            if (Instance == null || !Instance.IsD3D11FocusSafe || Instance.ActiveProfile == null || __instance == null) return;
            Instance.ApplySingleCameraOverhaul(__instance, Instance.ActiveProfile);
        }

        private static void Postfix_EnvironmentConditions_UpdateAtmosphere()
        {
            if (Instance == null || !Instance.IsD3D11FocusSafe || Instance.ActiveProfile == null) return;
            Instance.LumenEngine.ApplyAtmosphereGI(Instance.ActiveProfile);
        }

        private static void Postfix_LightQuality_InitLightSettings(LightQuality __instance)
        {
            if (Instance == null || !Instance.IsD3D11FocusSafe || Instance.ActiveProfile == null || __instance == null) return;
            Instance.LumenEngine.ProcessLightQualityInstance(__instance, Instance.ActiveProfile);
        }

        private static void Postfix_TerrainQuality_ProcessQuality()
        {
            if (Instance == null || !Instance.IsD3D11FocusSafe || Instance.ActiveProfile == null) return;
            Instance.NanitePBREngine.ApplyNaniteWorldAndFoliage(Instance.ActiveProfile);
        }

        private static void Postfix_WaterQuality_ProcessQuality()
        {
            if (Instance == null || !Instance.IsD3D11FocusSafe || Instance.ActiveProfile == null) return;
            Instance.SkyWaterEngine.ApplyWaterOverhaul(Instance.ActiveProfile);
        }
    }
}
