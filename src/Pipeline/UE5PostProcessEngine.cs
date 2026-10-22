using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using OutwardUE5Overhaul.Presets;

namespace OutwardUE5Overhaul.Pipeline
{
    public class UE5PostProcessEngine
    {
        private readonly FieldInfo _fiCQCamera;
        private readonly FieldInfo _fiCQProfile;
        private readonly FieldInfo _fiCQBloom;

        private float _currentFocusDistance = 6.5f;

        private bool _hasCachedVanillaGrading = false;
        private GradingMode _vanillaGradingMode = GradingMode.HighDefinitionRange;
        private Tonemapper _vanillaTonemapper = Tonemapper.ACES;
        private float _vanillaContrast = 0f;
        private float _vanillaSaturation = 0f;
        private float _vanillaTemp = 0f;
        private float _vanillaTint = 0f;
        private Vector4 _vanillaLift = new Vector4(1f, 1f, 1f, 0f);
        private Vector4 _vanillaGamma = new Vector4(1f, 1f, 1f, 0f);
        private Vector4 _vanillaGain = new Vector4(1f, 1f, 1f, 0f);

        private bool _hasCachedVanillaBloom = false;
        private float _vanillaBloomIntensity = 0.8f;
        private float _vanillaBloomThreshold = 1.1f;
        private float _vanillaBloomSoftKnee = 0.5f;
        private float _vanillaBloomDiffusion = 7.0f;

        public UE5PostProcessEngine()
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Type cqType = typeof(CameraQuality);
            _fiCQCamera = cqType.GetField("m_camera", flags);
            _fiCQProfile = cqType.GetField("m_profile", flags);
            _fiCQBloom = cqType.GetField("m_bloom", flags);
        }

        public void ApplyCameraPostProcessing(CameraQuality cq, UE5RenderProfile profile)
        {
            if (cq == null || profile == null) return;

            Camera cam = (_fiCQCamera != null) ? (_fiCQCamera.GetValue(cq) as Camera) : cq.GetComponent<Camera>();
            PostProcessProfile ppProfile = (_fiCQProfile != null) ? (_fiCQProfile.GetValue(cq) as PostProcessProfile) : null;
            bool isInterior = LumenGIEngine.IsCameraInInterior(cam);

            if (cam != null)
            {
                cam.allowHDR = true;
                cam.allowMSAA = false;

                // Prevent Unity from allocating 2560x1440 fmt 27 (MotionVectors) RenderTextures when TAA/SSR/MotionBlur are off
                if (!profile.EnableTemporalAA && !profile.EnableScreenSpaceReflections && !profile.EnableMotionBlur)
                {
                    cam.depthTextureMode &= ~DepthTextureMode.MotionVectors;
                }

                PostProcessLayer ppLayer = cam.GetComponent<PostProcessLayer>();
                if (ppLayer != null)
                {
                    ConfigurePostProcessLayer(ppLayer, profile);
                }
            }

            // Ensure SENaturalBloomAndDirtyLens is disabled so it never causes full-screen haze/glow
            SENaturalBloomAndDirtyLens seBloom = (_fiCQBloom != null) ? (_fiCQBloom.GetValue(cq) as SENaturalBloomAndDirtyLens) : cq.GetComponent<SENaturalBloomAndDirtyLens>();
            if (seBloom != null)
            {
                seBloom.enabled = false;
            }

            if (ppProfile == null) return;

            // Ensure AutoExposure is always disabled so screen brightness NEVER pulses/oscillates
            AutoExposure ae;
            if (ppProfile.TryGetSettings<AutoExposure>(out ae) && ae != null)
            {
                ae.enabled.Override(false);
            }

            // 1. ACES Filmic HDR Color Grading (with Indoor Exposure & Shadow Detail preservation)
            ConfigureColorGrading(ppProfile, profile, isInterior);

            // 2. Physical HDR Bloom (Strict threshold so only real light sources bloom)
            ConfigureBloom(ppProfile, profile);

            // 3. Dynamic Autofocus Bokeh Depth of Field
            ConfigureDepthOfField(ppProfile, profile);

            // 4. Shutter Motion Blur, Vignette, Chromatic Aberration & Film Grain
            ConfigureLensAndFilmEffects(ppProfile, profile);
        }

        private void ConfigurePostProcessLayer(PostProcessLayer ppLayer, UE5RenderProfile profile)
        {
            ppLayer.stopNaNPropagation = true;

            if (profile.EnableTemporalAA)
            {
                ppLayer.antialiasingMode = PostProcessLayer.Antialiasing.TemporalAntialiasing;
                if (ppLayer.temporalAntialiasing != null)
                {
                    // Constrain jitter spread to micro-jitter so refractive/stained glass doesn't shimmer
                    ppLayer.temporalAntialiasing.jitterSpread = Mathf.Clamp(profile.TAAJitterSpread, 0.10f, 0.32f);
                    ppLayer.temporalAntialiasing.stationaryBlending = Mathf.Clamp(profile.TAAStationaryBlending, 0.88f, 0.98f);
                    ppLayer.temporalAntialiasing.motionBlending = Mathf.Clamp(profile.TAAMotionBlending, 0.75f, 0.95f);
                    ppLayer.temporalAntialiasing.sharpness = Mathf.Clamp(profile.TAASharpness, 0.0f, 1.2f);
                }
            }
            else if (profile.EnableACESFilmicGrading || profile.EnablePBRMaterialOverhaul)
            {
                // Default UE5 Flicker-Free Ultra SMAA: zero subpixel projection jitter on stained glass, zero fmt 27 MotionVector crash on Alt-Tab
                ppLayer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
                if (ppLayer.subpixelMorphologicalAntialiasing != null)
                {
                    ppLayer.subpixelMorphologicalAntialiasing.quality = SubpixelMorphologicalAntialiasing.Quality.High;
                }
            }
            else
            {
                int aaQuality = (OptionManager.Instance != null && OptionManager.Instance.CurrentGraphicSettings != null)
                    ? OptionManager.Instance.CurrentGraphicSettings.AntiAliasingQuality
                    : 1;
                ppLayer.antialiasingMode = (aaQuality > 0)
                    ? PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing
                    : PostProcessLayer.Antialiasing.None;
            }
        }

        private void ConfigureColorGrading(PostProcessProfile ppProfile, UE5RenderProfile profile, bool isInterior)
        {
            ColorGrading cg;
            if (!ppProfile.TryGetSettings<ColorGrading>(out cg) || cg == null) return;

            if (!_hasCachedVanillaGrading)
            {
                _vanillaGradingMode = cg.gradingMode.value;
                _vanillaTonemapper = cg.tonemapper.value;
                _vanillaContrast = cg.contrast.value;
                _vanillaSaturation = cg.saturation.value;
                _vanillaTemp = cg.temperature.value;
                _vanillaTint = cg.tint.value;
                _vanillaLift = cg.lift.value;
                _vanillaGamma = cg.gamma.value;
                _vanillaGain = cg.gain.value;
                _hasCachedVanillaGrading = true;
            }

            float baseBrightness = 0f;
            if (OptionManager.Instance != null && OptionManager.Instance.CurrentGraphicSettings != null)
            {
                baseBrightness = OptionManager.Instance.CurrentGraphicSettings.Brightness;
            }

            GradingMode targetMode;
            Tonemapper targetTonemapper;
            float targetExposure;
            float targetContrast;
            float targetSaturation;
            float targetTemp;
            float targetTint;
            Vector4 targetLift;
            Vector4 targetGamma;
            Vector4 targetGain;

            if (!profile.EnableACESFilmicGrading)
            {
                targetMode = _vanillaGradingMode;
                targetTonemapper = _vanillaTonemapper;
                targetExposure = baseBrightness;
                targetContrast = _vanillaContrast;
                targetSaturation = _vanillaSaturation;
                targetTemp = _vanillaTemp;
                targetTint = _vanillaTint;
                targetLift = _vanillaLift;
                targetGamma = _vanillaGamma;
                targetGain = _vanillaGain;
            }
            else
            {
                float indoorEV = isInterior ? profile.IndoorExposureBoost : 0f;
                float effectiveContrast = profile.Contrast;

                targetLift = profile.Lift;
                targetGamma = profile.Gamma;
                targetGain = profile.Gain;

                targetMode = GradingMode.HighDefinitionRange;
                targetTonemapper = Tonemapper.ACES;
                targetExposure = baseBrightness + profile.PostExposureOffset + indoorEV;
                targetContrast = Mathf.Clamp(_vanillaContrast + effectiveContrast, -40f, 60f);
                targetSaturation = Mathf.Clamp(_vanillaSaturation + profile.Saturation, -40f, 60f);
                targetTemp = Mathf.Clamp(_vanillaTemp + profile.Temperature, -40f, 40f);
                targetTint = Mathf.Clamp(_vanillaTint + profile.Tint, -40f, 40f);
            }

            // Only override ColorGrading parameters if they actually changed, preventing unnecessary
            // D3D11 128x2 LUT staging texture allocations (d3d11: failed to create staging 2D texture w=128 h=2)
            if (!cg.enabled.value) cg.enabled.Override(true);
            if (cg.gradingMode.value != targetMode) cg.gradingMode.Override(targetMode);
            if (cg.tonemapper.value != targetTonemapper) cg.tonemapper.Override(targetTonemapper);
            if (Mathf.Abs(cg.postExposure.value - targetExposure) > 0.001f) cg.postExposure.Override(targetExposure);
            if (Mathf.Abs(cg.contrast.value - targetContrast) > 0.01f) cg.contrast.Override(targetContrast);
            if (Mathf.Abs(cg.saturation.value - targetSaturation) > 0.01f) cg.saturation.Override(targetSaturation);
            if (Mathf.Abs(cg.temperature.value - targetTemp) > 0.01f) cg.temperature.Override(targetTemp);
            if (Mathf.Abs(cg.tint.value - targetTint) > 0.01f) cg.tint.Override(targetTint);
            if ((cg.lift.value - targetLift).sqrMagnitude > 0.00001f) cg.lift.Override(targetLift);
            if ((cg.gamma.value - targetGamma).sqrMagnitude > 0.00001f) cg.gamma.Override(targetGamma);
            if ((cg.gain.value - targetGain).sqrMagnitude > 0.00001f) cg.gain.Override(targetGain);
        }

        private void ConfigureBloom(PostProcessProfile ppProfile, UE5RenderProfile profile)
        {
            Bloom bloom;
            if (!ppProfile.TryGetSettings<Bloom>(out bloom) || bloom == null) return;

            if (!_hasCachedVanillaBloom)
            {
                _vanillaBloomIntensity = bloom.intensity.value;
                _vanillaBloomThreshold = bloom.threshold.value;
                _vanillaBloomSoftKnee = bloom.softKnee.value;
                _vanillaBloomDiffusion = bloom.diffusion.value;
                _hasCachedVanillaBloom = true;
            }

            if (!profile.EnablePhysicalBloom)
            {
                bool vanillaBloom = (OptionManager.Instance != null && OptionManager.Instance.CurrentGraphicSettings != null)
                    ? (OptionManager.Instance.CurrentGraphicSettings.Bloom > 0)
                    : true;
                bloom.enabled.Override(vanillaBloom);
                bloom.intensity.Override(_vanillaBloomIntensity);
                bloom.threshold.Override(_vanillaBloomThreshold);
                bloom.softKnee.Override(_vanillaBloomSoftKnee);
                bloom.diffusion.Override(_vanillaBloomDiffusion);
                return;
            }

            bloom.enabled.Override(true);
            bloom.intensity.Override(Mathf.Clamp(profile.BloomIntensity, 0.0f, 2.5f));
            bloom.threshold.Override(Mathf.Clamp(profile.BloomThreshold, 0.8f, 2.5f));
            bloom.softKnee.Override(Mathf.Clamp01(profile.BloomSoftKnee));
            bloom.diffusion.Override(Mathf.Clamp(profile.BloomDiffusion, 1.0f, 9.5f));
            bloom.clamp.Override(16.0f);
        }

        private void ConfigureDepthOfField(PostProcessProfile ppProfile, UE5RenderProfile profile)
        {
            DepthOfField dof;
            if (!ppProfile.TryGetSettings<DepthOfField>(out dof) || dof == null) return;

            if (!profile.EnableDynamicAutofocusDoF)
            {
                bool vanillaDof = (OptionManager.Instance != null && OptionManager.Instance.CurrentGraphicSettings != null)
                    ? (OptionManager.Instance.CurrentGraphicSettings.DepthOfField > 0)
                    : false;
                dof.enabled.Override(vanillaDof);
                return;
            }

            dof.enabled.Override(true);
            dof.focusDistance.Override(_currentFocusDistance);
            dof.aperture.Override(Mathf.Clamp(profile.DoFAperture, 1.8f, 16.0f));
            dof.focalLength.Override(Mathf.Clamp(profile.DoFFocalLength, 15.0f, 90.0f));
            dof.kernelSize.Override(KernelSize.Large);
        }

        private void ConfigureLensAndFilmEffects(PostProcessProfile ppProfile, UE5RenderProfile profile)
        {
            // Motion Blur
            UnityEngine.Rendering.PostProcessing.MotionBlur mb;
            if (ppProfile.TryGetSettings<UnityEngine.Rendering.PostProcessing.MotionBlur>(out mb) && mb != null)
            {
                mb.enabled.Override(profile.EnableMotionBlur && profile.MotionBlurShutterAngle > 1f);
                mb.shutterAngle.Override(Mathf.Clamp(profile.MotionBlurShutterAngle, 0f, 360f));
                mb.sampleCount.Override(10);
            }

            // Vignette
            Vignette vig;
            if (!ppProfile.TryGetSettings<Vignette>(out vig))
            {
                vig = ppProfile.AddSettings<Vignette>();
            }
            if (vig != null)
            {
                vig.enabled.Override(profile.VignetteIntensity > 0.005f);
                vig.mode.Override(VignetteMode.Classic);
                vig.intensity.Override(Mathf.Clamp01(profile.VignetteIntensity));
                vig.smoothness.Override(0.45f);
                vig.roundness.Override(1.0f);
                vig.rounded.Override(false);
                vig.color.Override(new Color(0.02f, 0.02f, 0.03f, 1f));
            }

            // Chromatic Aberration
            ChromaticAberration ca;
            if (!ppProfile.TryGetSettings<ChromaticAberration>(out ca))
            {
                ca = ppProfile.AddSettings<ChromaticAberration>();
            }
            if (ca != null)
            {
                ca.enabled.Override(profile.ChromaticAberrationIntensity > 0.005f);
                ca.intensity.Override(Mathf.Clamp01(profile.ChromaticAberrationIntensity));
                ca.fastMode.Override(true);
            }

            // Anti-Banding Film Grain
            Grain grain;
            if (!ppProfile.TryGetSettings<Grain>(out grain))
            {
                grain = ppProfile.AddSettings<Grain>();
            }
            if (grain != null)
            {
                grain.enabled.Override(profile.FilmGrainIntensity > 0.005f);
                grain.colored.Override(false);
                grain.intensity.Override(Mathf.Clamp01(profile.FilmGrainIntensity));
                grain.size.Override(0.8f);
                grain.lumContrib.Override(0.75f);
            }
        }

        public void UpdateDynamicAutofocus(UE5RenderProfile profile)
        {
            if (profile == null || !profile.EnableDynamicAutofocusDoF) return;
            if (CharacterCamera.CharCamList == null || CharacterCamera.CharCamList.Count == 0) return;

            for (int i = 0; i < CharacterCamera.CharCamList.Count; i++)
            {
                CharacterCamera charCam = CharacterCamera.CharCamList[i];
                if (charCam == null || charCam.CameraScript == null) continue;

                Camera cam = charCam.CameraScript;
                CameraQuality cq = cam.GetComponent<CameraQuality>();
                if (cq == null) continue;

                PostProcessProfile ppProfile = (_fiCQProfile != null) ? (_fiCQProfile.GetValue(cq) as PostProcessProfile) : null;
                if (ppProfile == null) continue;

                DepthOfField dof;
                if (!ppProfile.TryGetSettings<DepthOfField>(out dof) || dof == null) continue;

                float minSafePlayerDist = 4.5f;
                if (charCam.TargetCharacter != null)
                {
                    minSafePlayerDist = Vector3.Distance(cam.transform.position, charCam.TargetCharacter.CenterPosition);
                }

                float targetDist = minSafePlayerDist + 2.5f;
                RaycastHit hit;
                int mask = ~(1 << 5);
                Vector3 rayOrigin = cam.transform.position + cam.transform.forward * (minSafePlayerDist * 0.85f);
                if (Physics.SphereCast(rayOrigin, 0.30f, cam.transform.forward, out hit, 120f, mask, QueryTriggerInteraction.Ignore))
                {
                    float totalHitDist = (minSafePlayerDist * 0.85f) + hit.distance;
                    targetDist = Mathf.Max(minSafePlayerDist, totalHitDist);
                }
                else
                {
                    targetDist = 25.0f;
                }

                float nextFocus = Mathf.Lerp(_currentFocusDistance, targetDist, Time.unscaledDeltaTime * 7.5f);
                if (Mathf.Abs(nextFocus - _currentFocusDistance) > 0.05f)
                {
                    _currentFocusDistance = nextFocus;
                    dof.focusDistance.Override(_currentFocusDistance);
                }
            }
        }
    }
}
