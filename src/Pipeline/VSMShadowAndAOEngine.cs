using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using OutwardUE5Overhaul.Presets;

namespace OutwardUE5Overhaul.Pipeline
{
    public class VSMShadowAndAOEngine
    {
        private readonly FieldInfo _fiCQCamera;
        private readonly FieldInfo _fiCQSSAO;
        private readonly FieldInfo _fiCQSSShadows;
        private readonly FieldInfo _fiCQProfile;

        public VSMShadowAndAOEngine()
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Type cqType = typeof(CameraQuality);
            _fiCQCamera = cqType.GetField("m_camera", flags);
            _fiCQSSAO = cqType.GetField("m_ssao", flags);
            _fiCQSSShadows = cqType.GetField("m_ssShadows", flags);
            _fiCQProfile = cqType.GetField("m_profile", flags);
        }

        public void ApplyGlobalShadowSettings(UE5RenderProfile profile)
        {
            if (profile == null) return;

            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowDistance = Mathf.Clamp(profile.ShadowDistance, 80f, 450f);
            QualitySettings.shadowCascade4Split = new Vector3(
                Mathf.Clamp(profile.CascadeSplit0, 0.015f, 0.15f),
                Mathf.Clamp(profile.CascadeSplit1, 0.05f, 0.30f),
                Mathf.Clamp(profile.CascadeSplit2, 0.15f, 0.60f)
            );

            if (EnvironmentConditions.Instance != null)
            {
                float shadowDist = QualitySettings.shadowDistance;
                float envMult = profile.UncapEnvironmentShadowCulling ? 1.0f : 0.5f;
                float[] layerShadowCulls = new float[32];
                layerShadowCulls[0] = shadowDist;
                layerShadowCulls[11] = shadowDist * envMult;

                Light sun = EnvironmentConditions.Instance.SunLight;
                if (sun != null)
                {
                    sun.layerShadowCullDistances = layerShadowCulls;
                }

                Light moon = EnvironmentConditions.Instance.MoonLight;
                if (moon != null)
                {
                    moon.layerShadowCullDistances = layerShadowCulls;
                }
            }
        }

        public void ApplyCameraShadowAndAO(CameraQuality cq, UE5RenderProfile profile)
        {
            if (cq == null || profile == null) return;

            Camera cam = (_fiCQCamera != null) ? (_fiCQCamera.GetValue(cq) as Camera) : cq.GetComponent<Camera>();
            bool isInterior = LumenGIEngine.IsCameraInInterior(cam);

            // 1. Environment Layer Shadow Culling & Camera Layer Culling
            if (profile.UncapEnvironmentShadowCulling)
            {
                cq.DefaultLayerShadowDistMult = 1.0f;
                cq.EnvironmentLayerShadowDistMult = 1.0f;
                cq.EnvironmentMaxDist = Mathf.Max(200f, profile.CameraEnvironmentCullDistance);
            }
            else
            {
                cq.DefaultLayerShadowDistMult = 1.0f;
                cq.EnvironmentLayerShadowDistMult = 0.5f;
                cq.EnvironmentMaxDist = 200f;
            }

            if (cam != null)
            {
                float[] cullDists = new float[32];
                cullDists[2] = 200f;
                cullDists[11] = profile.UncapEnvironmentShadowCulling
                    ? Mathf.Max(200f, profile.CameraEnvironmentCullDistance)
                    : 200f;
                cam.layerCullDistances = cullDists;
            }

            // 2. Virtual Shadow Maps (VSM) Micro-Detail Contact Shadows (SEScreenSpaceShadows)
            SEScreenSpaceShadows sss = (_fiCQSSShadows != null) ? (_fiCQSSShadows.GetValue(cq) as SEScreenSpaceShadows) : cq.GetComponent<SEScreenSpaceShadows>();
            if (sss != null)
            {
                ConfigureContactShadows(sss, profile, isInterior);
            }

            // 3. Clean Single-Pass Ambient Occlusion in PostProcessProfile
            PostProcessProfile ppProfile = (_fiCQProfile != null) ? (_fiCQProfile.GetValue(cq) as PostProcessProfile) : null;
            if (ppProfile != null)
            {
                AmbientOcclusion ao;
                if (ppProfile.TryGetSettings<AmbientOcclusion>(out ao) && ao != null)
                {
                    ao.enabled.Override(true);
                    ao.mode.Override(AmbientOcclusionMode.ScalableAmbientObscurance);
                    ao.quality.Override(AmbientOcclusionQuality.High);
                    float targetAO = profile.EnableEnhancedAO ? Mathf.Clamp(profile.AOIntensity, 0.2f, 2.0f) : 0.8f;
                    if (isInterior && profile.EnableEnhancedAO)
                    {
                        targetAO *= 0.80f;
                    }
                    ao.intensity.Override(targetAO);
                    ao.radius.Override(profile.EnableEnhancedAO ? Mathf.Clamp(profile.AORadius, 0.1f, 1.0f) : 0.3f);

                    bool isDeferred = (cam != null && cam.actualRenderingPath == RenderingPath.DeferredShading);
                    ao.ambientOnly.Override(isDeferred);
                }
            }

            // Ensure SSAOPro stays disabled so it doesn't double-darken interiors on top of PostProcess AO
            SSAOPro ssao = (_fiCQSSAO != null) ? (_fiCQSSAO.GetValue(cq) as SSAOPro) : cq.GetComponent<SSAOPro>();
            if (ssao != null)
            {
                ssao.enabled = false;
            }
        }

        public void ConfigureContactShadows(SEScreenSpaceShadows sss, UE5RenderProfile profile, bool isInterior)
        {
            if (sss == null || profile == null) return;

            bool vanillaSSShadows = (OptionManager.Instance != null && OptionManager.Instance.CurrentGraphicSettings != null)
                ? (OptionManager.Instance.CurrentGraphicSettings.SSShadows == 1)
                : true;

            if (!profile.EnableVSMContactShadows)
            {
                sss.enabled = vanillaSSShadows;
                sss.samples = 32;
                sss.range = 0.7f;
                sss.zThickness = 0.1f;
                sss.blendStrength = 1.0f;
                return;
            }

            sss.samples = Mathf.Clamp(profile.ContactShadowSamples, 16, 48);
            sss.nearSampleQuality = 1.25f;
            // Inside interiors, SEScreenSpaceShadows traces along the outdoor sun vector, so keep range/blend subtle indoors
            sss.range = isInterior
                ? Mathf.Min(0.45f, profile.ContactShadowRange)
                : Mathf.Clamp(profile.ContactShadowRange, 0.3f, 2.5f);
            sss.zThickness = Mathf.Clamp(profile.ContactShadowZThickness, 0.04f, 0.3f);
            sss.traceBias = 0.02f;
            sss.blendStrength = isInterior
                ? Mathf.Clamp01(profile.ContactShadowBlendStrength * 0.55f)
                : Mathf.Clamp01(profile.ContactShadowBlendStrength);
            sss.accumulation = 0.90f;
            sss.lengthFade = 0.70f;
            sss.stochasticSampling = false;
            sss.leverageTemporalAA = false;
            sss.bilateralBlur = true;
            sss.blurPasses = 2;
            sss.blurDepthTolerance = 0.1f;
        }
    }
}
