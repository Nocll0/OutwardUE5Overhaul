using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using OutwardUE5Overhaul.Presets;

namespace OutwardUE5Overhaul.UI
{
    public class UE5StudioWindow
    {
        private bool _isOpen = false;
        private Rect _windowRect = new Rect(40f, 40f, 680f, 600f);
        private int _selectedTab = 0;
        private Vector2 _scrollPos = Vector2.zero;

        private string _toastMessage = string.Empty;
        private float _toastExpireTime = -1f;

        private GUIStyle _windowStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _activeButtonStyle;
        private GUIStyle _toastStyle;
        private bool _stylesInitialized = false;

        private Texture2D _texBg;
        private Texture2D _texPanel;
        private Texture2D _texBtn;
        private Texture2D _texBtnActive;
        private Texture2D _texToast;

        private float _smoothedDeltaTime = 0.016f;
        private bool _pendingSliderApply = false;
        private float _nextSliderApplyTime = 0f;

        private static readonly string[] TabNames = new string[]
        {
            "1. Lumen GI",
            "2. VSM & AO",
            "3. ACES & Lens",
            "4. Sky & Water",
            "5. Nanite & PBR"
        };

        public bool IsOpen
        {
            get { return _isOpen; }
            set { _isOpen = value; }
        }

        public void ToggleWindow()
        {
            _isOpen = !_isOpen;
        }

        public void ShowToast(string message, float duration = 3.5f)
        {
            _toastMessage = message;
            _toastExpireTime = Time.unscaledTime + duration;
        }

        public void UpdateMetrics()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0.0001f && dt < 1.0f)
            {
                _smoothedDeltaTime = Mathf.Lerp(_smoothedDeltaTime, dt, 0.08f);
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized && _texBg != null) return;

            _texBg = MakeSolidTex(new Color(0.08f, 0.09f, 0.11f, 0.96f));
            _texPanel = MakeSolidTex(new Color(0.13f, 0.14f, 0.18f, 0.95f));
            _texBtn = MakeSolidTex(new Color(0.18f, 0.20f, 0.25f, 1.0f));
            _texBtnActive = MakeSolidTex(new Color(0.14f, 0.42f, 0.85f, 1.0f));
            _texToast = MakeSolidTex(new Color(0.07f, 0.09f, 0.13f, 0.92f));

            _windowStyle = new GUIStyle(GUI.skin.window);
            _windowStyle.normal.background = _texBg;
            _windowStyle.onNormal.background = _texBg;
            _windowStyle.normal.textColor = new Color(0.92f, 0.95f, 1.0f);
            _windowStyle.fontSize = 14;
            _windowStyle.fontStyle = FontStyle.Bold;
            _windowStyle.padding = new RectOffset(14, 14, 28, 14);

            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.fontSize = 15;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.normal.textColor = new Color(0.38f, 0.72f, 1.0f);

            _subHeaderStyle = new GUIStyle(GUI.skin.label);
            _subHeaderStyle.fontSize = 12;
            _subHeaderStyle.normal.textColor = new Color(0.72f, 0.78f, 0.86f);

            _sectionStyle = new GUIStyle(GUI.skin.box);
            _sectionStyle.normal.background = _texPanel;
            _sectionStyle.padding = new RectOffset(10, 10, 8, 8);
            _sectionStyle.margin = new RectOffset(0, 0, 6, 6);

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 12;
            _labelStyle.normal.textColor = new Color(0.90f, 0.92f, 0.96f);

            _valueStyle = new GUIStyle(GUI.skin.label);
            _valueStyle.fontSize = 12;
            _valueStyle.fontStyle = FontStyle.Bold;
            _valueStyle.alignment = TextAnchor.MiddleRight;
            _valueStyle.normal.textColor = new Color(0.45f, 0.82f, 1.0f);

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.normal.background = _texBtn;
            _buttonStyle.hover.background = _texBtnActive;
            _buttonStyle.active.background = _texBtnActive;
            _buttonStyle.normal.textColor = Color.white;
            _buttonStyle.hover.textColor = Color.white;
            _buttonStyle.fontSize = 12;
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.padding = new RectOffset(8, 8, 6, 6);

            _activeButtonStyle = new GUIStyle(_buttonStyle);
            _activeButtonStyle.normal.background = _texBtnActive;

            _toastStyle = new GUIStyle(GUI.skin.box);
            _toastStyle.normal.background = _texToast;
            _toastStyle.normal.textColor = new Color(0.48f, 0.84f, 1.0f);
            _toastStyle.fontSize = 14;
            _toastStyle.fontStyle = FontStyle.Bold;
            _toastStyle.alignment = TextAnchor.MiddleCenter;
            _toastStyle.padding = new RectOffset(16, 16, 8, 8);

            _stylesInitialized = true;
        }

        private static Texture2D MakeSolidTex(Color col)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixels = new Color[] { col, col, col, col };
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        public void OnGUI(OutwardUE5OverhaulPlugin plugin)
        {
            if (plugin == null) return;
            InitStyles();

            if (Time.unscaledTime < _toastExpireTime && !string.IsNullOrEmpty(_toastMessage))
            {
                float w = 660f;
                float h = 38f;
                Rect toastRect = new Rect((Screen.width - w) * 0.5f, 18f, w, h);
                GUI.Box(toastRect, _toastMessage, _toastStyle);
            }

            if (!_isOpen) return;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            _windowRect = GUI.Window(202605, _windowRect, delegate(int id)
            {
                DrawWindowContents(plugin);
                GUI.DragWindow(new Rect(0f, 0f, 10000f, 28f));
            }, "UNREAL ENGINE 5 GRAPHICS STUDIO (OUTWARD 2026 OVERHAUL v1.6.2)", _windowStyle);
        }

        private void DrawWindowContents(OutwardUE5OverhaulPlugin plugin)
        {
            UE5RenderProfile p = plugin.ActiveProfile;
            if (p == null) return;

            float fps = 1.0f / Mathf.Max(0.0001f, _smoothedDeltaTime);
            float ms = _smoothedDeltaTime * 1000.0f;
            string sceneName = SceneManager.GetActiveScene().name;
            float tod = (EnvironmentConditions.Instance != null) ? EnvironmentConditions.Instance.TimeOfDay : 12.0f;

            GUILayout.BeginVertical(_sectionStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("GPU: {0}  |  FPS: {1:0.0} ({2:0.0} ms)", SystemInfo.graphicsDeviceName, fps, ms), _headerStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(plugin.IsOverhaulEnabled ? "F11: A/B Toggle [UE5 ACTIVE]" : "F11: A/B Toggle [VANILLA]", plugin.IsOverhaulEnabled ? _activeButtonStyle : _buttonStyle, GUILayout.Width(210f)))
            {
                plugin.ToggleVanillaABComparison();
            }
            if (GUILayout.Button("Close [F10]", _buttonStyle, GUILayout.Width(90f)))
            {
                _isOpen = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(string.Format("Active Preset: {0}  |  Scene: {1}  |  TOD: {2:00.00}h", p.Name, sceneName, tod), _subHeaderStyle);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(_sectionStyle);
            GUILayout.Label("QUICK VISUAL PRESETS:", _subHeaderStyle);
            GUILayout.BeginHorizontal();
            DrawPresetButton(plugin, UE5PresetType.UE5_Lumen_Nanite_2026, "UE5 Lumen 2026");
            DrawPresetButton(plugin, UE5PresetType.UE5_Dark_Fantasy_Grim, "Dark Fantasy");
            DrawPresetButton(plugin, UE5PresetType.UE5_Photorealistic_Natural, "Photorealistic");
            DrawPresetButton(plugin, UE5PresetType.UE5_High_FPS_Gameplay, "High-FPS 140+");
            DrawPresetButton(plugin, UE5PresetType.Vanilla_Original, "Vanilla");
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            for (int i = 0; i < TabNames.Length; i++)
            {
                if (GUILayout.Button(TabNames[i], _selectedTab == i ? _activeButtonStyle : _buttonStyle))
                {
                    _selectedTab = i;
                }
            }
            GUILayout.EndHorizontal();

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, _sectionStyle, GUILayout.ExpandHeight(true));
            bool changed = false;

            switch (_selectedTab)
            {
                case 0:
                    changed |= DrawLumenTab(p);
                    break;
                case 1:
                    changed |= DrawShadowsAndAOTab(p);
                    break;
                case 2:
                    changed |= DrawACESAndCameraTab(p);
                    break;
                case 3:
                    changed |= DrawSkyAndWaterTab(p);
                    break;
                case 4:
                    changed |= DrawNaniteAndPBRTab(p);
                    break;
            }

            GUILayout.EndScrollView();

            if (changed)
            {
                _pendingSliderApply = true;
            }

            if (_pendingSliderApply && (!Input.GetMouseButton(0) || Time.unscaledTime >= _nextSliderApplyTime))
            {
                _pendingSliderApply = false;
                _nextSliderApplyTime = Time.unscaledTime + 0.18f;
                plugin.ApplyAllGraphicsImmediate(false);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Force Re-Scan Scene Materials & Textures", _buttonStyle))
            {
                plugin.ApplyAllGraphicsImmediate(true);
                ShowToast("Re-scanned & upgraded all scene materials and textures.");
            }
            if (GUILayout.Button("Save Current Settings to Config", _activeButtonStyle, GUILayout.Width(230f)))
            {
                plugin.SaveProfileToConfig();
                ShowToast("Saved UE5 Graphics Overhaul settings to BepInEx config.");
            }
            GUILayout.EndHorizontal();
        }

        private void DrawPresetButton(OutwardUE5OverhaulPlugin plugin, UE5PresetType preset, string label)
        {
            bool isCurrent = (plugin.CurrentPresetType == preset);
            if (GUILayout.Button(label, isCurrent ? _activeButtonStyle : _buttonStyle))
            {
                plugin.LoadPreset(preset);
            }
        }

        private bool DrawLumenTab(UE5RenderProfile p)
        {
            bool c = false;
            GUILayout.Label("LUMEN GLOBAL ILLUMINATION, LIGHT CARRY & INDOOR AMBIENT", _headerStyle);
            c |= DrawToggle("Enable Lumen Multi-Bounce GI & Extended Light Carry", ref p.EnableLumenGI);
            c |= DrawSlider("Point / Spot Light Range Multiplier (Light Carry)", ref p.PointLightRangeMultiplier, 1.0f, 2.2f);
            c |= DrawSlider("Indoor Room Ambient GI Boost", ref p.IndoorAmbientBoost, 0.0f, 0.70f);
            c |= DrawSlider("Indoor Exposure Adaptation Boost (EV)", ref p.IndoorExposureBoost, 0.0f, 0.65f);

            GUILayout.Space(8f);
            GUILayout.Label("HEMISPHERICAL TRILIGHT GI & SOLAR BOUNCE", _headerStyle);
            c |= DrawSlider("Zenith Sky / Floor Bounce Intensity", ref p.SkyBounceIntensity, 0.5f, 1.6f);
            c |= DrawSlider("Wall / Horizon Equator Bounce Intensity", ref p.EquatorBounceIntensity, 0.5f, 1.6f);
            c |= DrawSlider("Ceiling / Ground Albedo Bounce Intensity", ref p.GroundBounceIntensity, 0.4f, 1.5f);
            c |= DrawSlider("Local Lava/Fire/Crystal Chromatic GI Bounce", ref p.IndirectSolarBounceIntensity, 0.0f, 0.45f);
            c |= DrawSlider("Specular Reflection Intensity", ref p.ReflectionSpecularityBoost, 0.5f, 1.5f);
            return c;
        }

        private bool DrawShadowsAndAOTab(UE5RenderProfile p)
        {
            bool c = false;
            GUILayout.Label("VIRTUAL SHADOW MAPS (VSM) & CASCADE OVERHAUL", _headerStyle);
            c |= DrawSlider("4-Cascade Shadow Distance (m)", ref p.ShadowDistance, 100f, 450f);
            c |= DrawToggle("Uncap Layer 11 (Environment) 50% Shadow Culling", ref p.UncapEnvironmentShadowCulling);

            GUILayout.Space(8f);
            GUILayout.Label("MICRO-DETAIL SCREEN-SPACE CONTACT SHADOWS", _headerStyle);
            c |= DrawToggle("Enable Stochastic TAA Contact Shadows", ref p.EnableVSMContactShadows);
            c |= DrawIntSlider("Contact Shadow Ray Samples", ref p.ContactShadowSamples, 16, 48);
            c |= DrawSlider("Contact Shadow Trace Range (m)", ref p.ContactShadowRange, 0.4f, 2.5f);
            c |= DrawSlider("Contact Shadow Z-Thickness", ref p.ContactShadowZThickness, 0.04f, 0.25f);
            c |= DrawSlider("Contact Shadow Blend Strength", ref p.ContactShadowBlendStrength, 0.1f, 1.0f);

            GUILayout.Space(8f);
            GUILayout.Label("AMBIENT OCCLUSION", _headerStyle);
            c |= DrawToggle("Enable Enhanced Cavity Ambient Occlusion", ref p.EnableEnhancedAO);
            c |= DrawSlider("Ambient Occlusion Intensity", ref p.AOIntensity, 0.2f, 2.0f);
            c |= DrawSlider("Ambient Occlusion Radius", ref p.AORadius, 0.1f, 0.8f);
            return c;
        }

        private bool DrawACESAndCameraTab(UE5RenderProfile p)
        {
            bool c = false;
            GUILayout.Label("ACES FILMIC HDR TONEMAPPER & COLOR SCIENCE", _headerStyle);
            c |= DrawToggle("Enable UE5 ACES Filmic HDR Grading", ref p.EnableACESFilmicGrading);
            c |= DrawSlider("Post-Exposure Offset (EV)", ref p.PostExposureOffset, -1.0f, 1.0f);
            c |= DrawSlider("Filmic Contrast", ref p.Contrast, -30f, 40f);
            c |= DrawSlider("Color Saturation", ref p.Saturation, -30f, 40f);
            c |= DrawSlider("White Balance Temperature", ref p.Temperature, -25f, 25f);

            GUILayout.Space(8f);
            GUILayout.Label("PHYSICAL BLOOM, AUTOFOCUS BOKEH DoF & LENS", _headerStyle);
            c |= DrawToggle("Enable Physical HDR Bloom", ref p.EnablePhysicalBloom);
            c |= DrawSlider("HDR Bloom Intensity", ref p.BloomIntensity, 0.0f, 2.0f);
            c |= DrawSlider("HDR Bloom Threshold (Higher = Less Glow)", ref p.BloomThreshold, 0.9f, 2.2f);
            c |= DrawSlider("HDR Bloom Diffusion Scatter", ref p.BloomDiffusion, 3.0f, 9.5f);
            c |= DrawToggle("Enable Dynamic Raycast Autofocus Bokeh DoF", ref p.EnableDynamicAutofocusDoF);
            c |= DrawSlider("Camera Aperture (f-stop)", ref p.DoFAperture, 1.8f, 12.0f);
            c |= DrawSlider("Camera Focal Length (mm)", ref p.DoFFocalLength, 20.0f, 85.0f);
            c |= DrawToggle("Enable Per-Frame Shutter Motion Blur", ref p.EnableMotionBlur);
            c |= DrawSlider("Vignette Intensity", ref p.VignetteIntensity, 0.0f, 0.4f);
            c |= DrawSlider("Chromatic Aberration", ref p.ChromaticAberrationIntensity, 0.0f, 0.2f);
            c |= DrawSlider("Anti-Banding Film Grain", ref p.FilmGrainIntensity, 0.0f, 0.15f);
            return c;
        }

        private bool DrawSkyAndWaterTab(UE5RenderProfile p)
        {
            bool c = false;
            GUILayout.Label("VOLUMETRIC ATMOSPHERE, CLOUDS & GOD RAYS", _headerStyle);
            c |= DrawToggle("Enable Per-Pixel HDR Sky & Cloud Overhaul", ref p.EnableVolumetricSkyOverhaul);
            c |= DrawSlider("Rayleigh Sky Scattering", ref p.RayleighScatteringBoost, 0.7f, 1.6f);
            c |= DrawSlider("Mie Horizon Scattering", ref p.MieScatteringBoost, 0.7f, 1.6f);
            c |= DrawSlider("Cloud Silver-Lining Scattering", ref p.CloudScattering, 0.5f, 2.0f);
            c |= DrawSlider("Cloud Edge Sharpness", ref p.CloudSharpness, 0.5f, 2.0f);
            c |= DrawSlider("Volumetric Sun/Moon God Ray Intensity", ref p.GodRayIntensity, 0.0f, 1.2f);

            GUILayout.Space(8f);
            GUILayout.Label("SCREEN-SPACE REFLECTIONS (SSR)", _headerStyle);
            c |= DrawToggle("Enable Screen-Space Reflections (SSR)", ref p.EnableScreenSpaceReflections);
            c |= DrawSlider("SSR Max March Distance (m)", ref p.SSRMaxMarchDistance, 40f, 180f);
            return c;
        }

        private bool DrawNaniteAndPBRTab(UE5RenderProfile p)
        {
            bool c = false;
            GUILayout.Label("ANTI-ALIASING (ULTRA SMAA FLICKER-FREE DEFAULT vs TAA)", _headerStyle);
            c |= DrawToggle("Use Temporal AA (OFF = Ultra SMAA Flicker-Free on Glass)", ref p.EnableTemporalAA);
            if (p.EnableTemporalAA)
            {
                c |= DrawSlider("TAA Reconstruction Sharpness", ref p.TAASharpness, 0.0f, 1.2f);
                c |= DrawSlider("TAA Micro-Jitter Spread (Low = Less Shimmer)", ref p.TAAJitterSpread, 0.10f, 0.32f);
            }

            GUILayout.Space(8f);
            GUILayout.Label("NANITE GEOMETRY LOD, TERRAIN TESSELLATION & FOLIAGE", _headerStyle);
            c |= DrawSlider("Mesh LOD Bias (Higher = Less Pop-In)", ref p.MeshLODBias, 1.0f, 4.0f);
            c |= DrawSlider("Camera Environment Cull Distance (m)", ref p.CameraEnvironmentCullDistance, 200f, 1500f);
            c |= DrawSlider("Terrain Heightmap Pixel Error (1.0 = Ultra)", ref p.TerrainPixelError, 1.0f, 10.0f);
            c |= DrawSlider("Grass Density Multiplier", ref p.GrassDensityMultiplier, 0.5f, 1.5f);
            c |= DrawSlider("Grass Draw Distance (m)", ref p.GrassDrawDistance, 100f, 280f);

            GUILayout.Space(8f);
            GUILayout.Label("PBR MATERIAL, NORMAL RELIEF & TEXTURE SHARPNESS", _headerStyle);
            c |= DrawToggle("Enable PBR Material, Normal & Texture Overhaul", ref p.EnablePBRMaterialOverhaul);
            c |= DrawSlider("Texture MipMap Bias (-0.55 = 4K Sharpness)", ref p.TextureMipMapBias, -1.0f, 0.0f);
            c |= DrawSlider("Normal Map 3D Relief Boost", ref p.NormalMapReliefBoost, 0.8f, 2.0f);
            c |= DrawSlider("PBR Specular / Micro-Smoothness Boost", ref p.PBRMicroSmoothnessBoost, 0.8f, 1.4f);
            c |= DrawSlider("UBER Subsurface Translucency Strength", ref p.SubsurfaceTranslucencyStrength, 1.0f, 7.0f);
            return c;
        }

        private bool DrawToggle(string label, ref bool val)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(390f));
            bool newVal = GUILayout.Toggle(val, val ? " ENABLED" : " DISABLED", _buttonStyle, GUILayout.Width(120f));
            GUILayout.EndHorizontal();
            if (newVal != val)
            {
                val = newVal;
                return true;
            }
            return false;
        }

        private bool DrawSlider(string label, ref float val, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(310f));
            float newVal = GUILayout.HorizontalSlider(val, min, max, GUILayout.Width(210f));
            GUILayout.Label(string.Format("{0:0.00}", newVal), _valueStyle, GUILayout.Width(65f));
            GUILayout.EndHorizontal();
            if (Mathf.Abs(newVal - val) > 0.0005f)
            {
                val = newVal;
                return true;
            }
            return false;
        }

        private bool DrawIntSlider(string label, ref int val, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(310f));
            int newVal = Mathf.RoundToInt(GUILayout.HorizontalSlider(val, min, max, GUILayout.Width(210f)));
            GUILayout.Label(newVal.ToString(), _valueStyle, GUILayout.Width(65f));
            GUILayout.EndHorizontal();
            if (newVal != val)
            {
                val = newVal;
                return true;
            }
            return false;
        }
    }
}
