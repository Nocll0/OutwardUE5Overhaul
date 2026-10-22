using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityStandardAssets.ImageEffects;
using OutwardUE5Overhaul.Presets;

namespace OutwardUE5Overhaul.Pipeline
{
    public class VolumetricSkyAndWaterEngine
    {
        private struct CachedGlobalFogProps
        {
            public bool DistanceFog;
            public bool HeightFog;
            public float StartDistance;
        }

        private readonly FieldInfo _fiCQRays;
        private readonly FieldInfo _fiCQScattering;
        private readonly FieldInfo _fiCQProfile;
        private readonly FieldInfo _fiCQCamera;

        private bool _hasCachedSky = false;
        private float _origRayleigh = 1f;
        private float _origMie = 1f;
        private float _origDirectionality = 0.7f;
        private float _origContrast = 1f;
        private float _origCloudSharpness = 1f;
        private float _origCloudScattering = 1f;
        private float _origCloudAttenuation = 1f;

        private readonly Dictionary<int, CachedGlobalFogProps> _origGlobalFog = new Dictionary<int, CachedGlobalFogProps>();

        public VolumetricSkyAndWaterEngine()
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Type cqType = typeof(CameraQuality);
            _fiCQRays = cqType.GetField("m_rays", flags);
            _fiCQScattering = cqType.GetField("m_scattering", flags);
            _fiCQProfile = cqType.GetField("m_profile", flags);
            _fiCQCamera = cqType.GetField("m_camera", flags);
        }

        public void ApplySkyAndAtmosphere(UE5RenderProfile profile)
        {
            if (profile == null) return;

            TOD_Sky sky = TOD_Sky.Instance;
            if (sky == null) return;

            if (!_hasCachedSky && sky.Atmosphere != null && sky.Clouds != null)
            {
                _origRayleigh = sky.Atmosphere.RayleighMultiplier;
                _origMie = sky.Atmosphere.MieMultiplier;
                _origDirectionality = sky.Atmosphere.Directionality;
                _origContrast = sky.Atmosphere.Contrast;
                _origCloudSharpness = sky.Clouds.Sharpness;
                _origCloudScattering = sky.Clouds.Scattering;
                _origCloudAttenuation = sky.Clouds.Attenuation;
                _hasCachedSky = true;
            }

            if (!profile.EnableVolumetricSkyOverhaul)
            {
                if (_hasCachedSky && sky.Atmosphere != null && sky.Clouds != null)
                {
                    sky.Atmosphere.RayleighMultiplier = _origRayleigh;
                    sky.Atmosphere.MieMultiplier = _origMie;
                    sky.Atmosphere.Directionality = _origDirectionality;
                    sky.Atmosphere.Contrast = _origContrast;
                    sky.Clouds.Sharpness = _origCloudSharpness;
                    sky.Clouds.Scattering = _origCloudScattering;
                    sky.Clouds.Attenuation = _origCloudAttenuation;
                }
                return;
            }

            sky.SkyQuality = TOD_SkyQualityType.PerPixel;
            sky.CloudQuality = TOD_CloudQualityType.High;
            sky.MeshQuality = TOD_MeshQualityType.High;
            sky.StarQuality = TOD_StarQualityType.High;
            sky.ColorRange = TOD_ColorRangeType.HDR;
            sky.ColorOutput = TOD_ColorOutputType.Dithered;

            if (sky.Atmosphere != null)
            {
                sky.Atmosphere.RayleighMultiplier = _origRayleigh * Mathf.Clamp(profile.RayleighScatteringBoost, 0.7f, 1.6f);
                sky.Atmosphere.MieMultiplier = _origMie * Mathf.Clamp(profile.MieScatteringBoost, 0.7f, 1.6f);
                sky.Atmosphere.Directionality = Mathf.Clamp(profile.SunDirectionality, 0.5f, 0.90f);
                sky.Atmosphere.Contrast = _origContrast * 1.04f;
            }

            if (sky.Clouds != null)
            {
                sky.Clouds.Sharpness = _origCloudSharpness * Mathf.Clamp(profile.CloudSharpness, 0.5f, 2.0f);
                sky.Clouds.Scattering = _origCloudScattering * Mathf.Clamp(profile.CloudScattering, 0.5f, 2.0f);
            }
        }

        public void ApplyCameraVolumetricsAndSSR(CameraQuality cq, UE5RenderProfile profile)
        {
            if (cq == null || profile == null) return;

            Camera cam = (_fiCQCamera != null) ? (_fiCQCamera.GetValue(cq) as Camera) : cq.GetComponent<Camera>();
            PostProcessProfile ppProfile = (_fiCQProfile != null) ? (_fiCQProfile.GetValue(cq) as PostProcessProfile) : null;
            bool isInterior = LumenGIEngine.IsCameraInInterior(cam);

            // Push back outdoor GlobalFog distance inside interiors so rooms/halls have crystal-clear air
            // and distant doors/stone walls aren't veiled in outdoor blue-grey fog!
            if (cam != null)
            {
                GlobalFog gFog = cam.GetComponent<GlobalFog>();
                if (gFog != null)
                {
                    int gId = gFog.GetInstanceID();
                    CachedGlobalFogProps origFog;
                    if (!_origGlobalFog.TryGetValue(gId, out origFog))
                    {
                        origFog = new CachedGlobalFogProps
                        {
                            DistanceFog = gFog.distanceFog,
                            HeightFog = gFog.heightFog,
                            StartDistance = gFog.startDistance
                        };
                        _origGlobalFog[gId] = origFog;
                    }

                    gFog.heightFog = origFog.HeightFog;
                    gFog.distanceFog = origFog.DistanceFog;

                    if (profile.EnableLumenGI && isInterior)
                    {
                        gFog.startDistance = Mathf.Max(origFog.StartDistance, 65.0f);
                    }
                    else
                    {
                        gFog.startDistance = origFog.StartDistance;
                    }
                }
            }

            // 1. Volumetric God Rays (TOD_Rays)
            TOD_Rays rays = (_fiCQRays != null) ? (_fiCQRays.GetValue(cq) as TOD_Rays) : cq.GetComponent<TOD_Rays>();
            if (rays != null)
            {
                bool vanillaRays = (OptionManager.Instance != null && OptionManager.Instance.CurrentGraphicSettings != null)
                    ? (OptionManager.Instance.CurrentGraphicSettings.RayQuality > 0)
                    : true;

                if (profile.EnableVolumetricSkyOverhaul && profile.GodRayIntensity > 0.01f && vanillaRays && !isInterior)
                {
                    rays.enabled = true;
                    rays.Resolution = TOD_ImageEffect.ResolutionType.High;
                    rays.UseDepthTexture = true;
                    rays.BlurIterations = 3;
                    rays.BlurRadius = 2.2f;
                    rays.Intensity = Mathf.Clamp(profile.GodRayIntensity, 0.0f, 1.2f);
                }
                else
                {
                    rays.enabled = vanillaRays && !isInterior;
                    rays.Resolution = TOD_ImageEffect.ResolutionType.Normal;
                    rays.BlurIterations = 2;
                    rays.Intensity = 0.4f;
                }
            }

            // 2. Screen-Space Reflections (SSR) — Using stable High/Downsampled preset
            if (ppProfile != null)
            {
                ScreenSpaceReflections ssr;
                if (ppProfile.TryGetSettings<ScreenSpaceReflections>(out ssr) && ssr != null)
                {
                    bool vanillaSSR = (OptionManager.Instance != null && OptionManager.Instance.CurrentGraphicSettings != null)
                        ? (OptionManager.Instance.CurrentGraphicSettings.SSR > 0)
                        : true;

                    if (!profile.EnableScreenSpaceReflections)
                    {
                        ssr.enabled.Override(false);
                    }
                    else
                    {
                        ssr.enabled.Override(true);
                        ssr.preset.Override(ScreenSpaceReflectionPreset.High);
                        ssr.resolution.Override(ScreenSpaceReflectionResolution.Downsampled);
                        ssr.maximumMarchDistance.Override(Mathf.Clamp(profile.SSRMaxMarchDistance, 40f, 180f));
                        ssr.distanceFade.Override(0.25f);
                        ssr.vignette.Override(0.20f);
                    }
                }
            }
        }

        public void ApplyWaterOverhaul(UE5RenderProfile profile)
        {
            if (profile == null) return;

            int targetRes = profile.EnableVolumetricSkyOverhaul
                ? Mathf.Clamp(profile.PlanarWaterResolution, 256, 1024)
                : 256;

            WaterRender[] waterRenders = UnityEngine.Object.FindObjectsOfType<WaterRender>();
            if (waterRenders != null)
            {
                for (int i = 0; i < waterRenders.Length; i++)
                {
                    WaterRender wr = waterRenders[i];
                    if (wr == null) continue;
                    if (wr.gameObject.name.Contains("DEPTH")) continue;

                    wr.ReflectionTexSize = targetRes;
                    wr.RefractionTexSize = targetRes;
                }
            }
        }
    }
}
