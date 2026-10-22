using System;
using UnityEngine;

namespace OutwardUE5Overhaul.Presets
{
    public enum UE5PresetType
    {
        UE5_Lumen_Nanite_2026 = 0,
        UE5_Dark_Fantasy_Grim = 1,
        UE5_Photorealistic_Natural = 2,
        UE5_High_FPS_Gameplay = 3,
        Vanilla_Original = 4
    }

    [Serializable]
    public class UE5RenderProfile
    {
        public string Name;
        public string Subtitle;
        public string Description;

        // 1. Lumen Global Illumination, Light Carry & Local Chromatic Bounce
        public bool EnableLumenGI;
        public float SkyBounceIntensity;
        public float EquatorBounceIntensity;
        public float GroundBounceIntensity;
        public float IndirectSolarBounceIntensity;
        public float ReflectionSpecularityBoost;
        public float PointLightRangeMultiplier;
        public float IndoorAmbientBoost;
        public float IndoorExposureBoost;

        // 2. Virtual Shadow Maps (VSM) & Contact Shadows
        public float ShadowDistance;
        public float CascadeSplit0;
        public float CascadeSplit1;
        public float CascadeSplit2;
        public bool UncapEnvironmentShadowCulling;
        public bool EnableVSMContactShadows;
        public int ContactShadowSamples;
        public float ContactShadowRange;
        public float ContactShadowZThickness;
        public float ContactShadowBlendStrength;

        // 3. Ambient Occlusion
        public bool EnableEnhancedAO;
        public float AOIntensity;
        public float AORadius;

        // 4. ACES Filmic HDR Color Pipeline
        public bool EnableACESFilmicGrading;
        public float PostExposureOffset;
        public float Contrast;
        public float Saturation;
        public float Temperature;
        public float Tint;
        public Vector4 Lift;   // Shadows (R, G, B, Offset)
        public Vector4 Gamma;  // Midtones (R, G, B, Offset)
        public Vector4 Gain;   // Highlights (R, G, B, Offset)

        // 5. Physical Camera, Bloom, DoF & Lens
        public bool EnablePhysicalBloom;
        public float BloomIntensity;
        public float BloomThreshold;
        public float BloomSoftKnee;
        public float BloomDiffusion;
        public bool EnableDynamicAutofocusDoF;
        public float DoFAperture;
        public float DoFFocalLength;
        public bool EnableMotionBlur;
        public float MotionBlurShutterAngle;
        public float VignetteIntensity;
        public float ChromaticAberrationIntensity;
        public float FilmGrainIntensity;

        // 6. Anti-Aliasing
        public bool EnableTemporalAA;
        public float TAAJitterSpread;
        public float TAAStationaryBlending;
        public float TAAMotionBlending;
        public float TAASharpness;

        // 7. Sky, Atmosphere, SSR & Water
        public bool EnableVolumetricSkyOverhaul;
        public float RayleighScatteringBoost;
        public float MieScatteringBoost;
        public float SunDirectionality;
        public float CloudSharpness;
        public float CloudScattering;
        public float GodRayIntensity;
        public bool EnableScreenSpaceReflections;
        public float SSRMaxMarchDistance;
        public int PlanarWaterResolution;

        // 8. Nanite Geometry, Foliage & PBR Material/Texture Overhaul
        public float MeshLODBias;
        public float CameraEnvironmentCullDistance;
        public float TerrainPixelError;
        public float GrassDensityMultiplier;
        public float GrassDrawDistance;
        public bool EnablePBRMaterialOverhaul;
        public float TextureMipMapBias;
        public float NormalMapReliefBoost;
        public float PBRMicroSmoothnessBoost;
        public float SubsurfaceTranslucencyStrength;

        public UE5RenderProfile Clone()
        {
            return (UE5RenderProfile)this.MemberwiseClone();
        }
    }

    public static class UE5VisualPresets
    {
        public static UE5RenderProfile GetPreset(UE5PresetType preset)
        {
            switch (preset)
            {
                case UE5PresetType.UE5_Lumen_Nanite_2026:
                    return CreateLumenNanite2026();
                case UE5PresetType.UE5_Dark_Fantasy_Grim:
                    return CreateDarkFantasyGrim();
                case UE5PresetType.UE5_Photorealistic_Natural:
                    return CreatePhotorealisticNatural();
                case UE5PresetType.UE5_High_FPS_Gameplay:
                    return CreateHighFPSGameplay();
                case UE5PresetType.Vanilla_Original:
                default:
                    return CreateVanillaOriginal();
            }
        }

        public static UE5RenderProfile CreateLumenNanite2026()
        {
            UE5RenderProfile p = new UE5RenderProfile();
            p.Name = "UE5 Lumen & Neural Ray-Reconstruction 2026";
            p.Subtitle = "Path-Traced Emissive/Lava Light Carry, Chromatic GI & 4K PBR";
            p.Description = "Unclamped 1.65x light carry (volcano lava, fire, lanterns), real-time chromatic local GI bounce, 32 pixel lights, and 4K optical micro-detail.";

            p.EnableLumenGI = true;
            p.SkyBounceIntensity = 1.15f;
            p.EquatorBounceIntensity = 1.18f;
            p.GroundBounceIntensity = 1.05f;
            p.IndirectSolarBounceIntensity = 0.18f;
            p.ReflectionSpecularityBoost = 1.00f;
            p.PointLightRangeMultiplier = 1.65f;
            p.IndoorAmbientBoost = 0.08f;
            p.IndoorExposureBoost = 0.06f;

            p.ShadowDistance = 280f;
            p.CascadeSplit0 = 0.035f;
            p.CascadeSplit1 = 0.11f;
            p.CascadeSplit2 = 0.28f;
            p.UncapEnvironmentShadowCulling = true;
            p.EnableVSMContactShadows = true;
            p.ContactShadowSamples = 36;
            p.ContactShadowRange = 1.05f;
            p.ContactShadowZThickness = 0.10f;
            p.ContactShadowBlendStrength = 0.72f;

            p.EnableEnhancedAO = true;
            p.AOIntensity = 1.02f;
            p.AORadius = 0.32f;

            p.EnableACESFilmicGrading = true;
            p.PostExposureOffset = 0.15f;
            p.Contrast = 5.0f;
            p.Saturation = -5.0f;
            p.Temperature = 0.0f;
            p.Tint = 0.0f;
            p.Lift = new Vector4(1.00f, 1.00f, 1.00f, 0.002f);
            p.Gamma = new Vector4(1.00f, 1.00f, 1.00f, 0.02f);
            p.Gain = new Vector4(1.00f, 1.00f, 1.00f, 0.00f);

            p.EnablePhysicalBloom = true;
            p.BloomIntensity = 0.42f;
            p.BloomThreshold = 1.32f;
            p.BloomSoftKnee = 0.68f;
            p.BloomDiffusion = 8.2f;
            p.EnableDynamicAutofocusDoF = false;
            p.DoFAperture = 5.6f;
            p.DoFFocalLength = 38.0f;
            p.EnableMotionBlur = false;
            p.MotionBlurShutterAngle = 90.0f;
            p.VignetteIntensity = 0.06f;
            p.ChromaticAberrationIntensity = 0.0f;
            p.FilmGrainIntensity = 0.0f;

            p.EnableTemporalAA = false;
            p.TAAJitterSpread = 0.18f;
            p.TAAStationaryBlending = 0.95f;
            p.TAAMotionBlending = 0.88f;
            p.TAASharpness = 0.25f;

            p.EnableVolumetricSkyOverhaul = true;
            p.RayleighScatteringBoost = 1.05f;
            p.MieScatteringBoost = 1.12f;
            p.SunDirectionality = 0.76f;
            p.CloudSharpness = 1.18f;
            p.CloudScattering = 1.15f;
            p.GodRayIntensity = 0.45f;
            p.EnableScreenSpaceReflections = false;
            p.SSRMaxMarchDistance = 100.0f;
            p.PlanarWaterResolution = 512;

            p.MeshLODBias = 2.8f;
            p.CameraEnvironmentCullDistance = 950.0f;
            p.TerrainPixelError = 2.0f;
            p.GrassDensityMultiplier = 1.15f;
            p.GrassDrawDistance = 230.0f;
            p.EnablePBRMaterialOverhaul = true;
            p.TextureMipMapBias = -0.32f;
            p.NormalMapReliefBoost = 1.10f;
            p.PBRMicroSmoothnessBoost = 1.00f;
            p.SubsurfaceTranslucencyStrength = 4.2f;
            return p;
        }

        public static UE5RenderProfile CreateDarkFantasyGrim()
        {
            UE5RenderProfile p = CreateLumenNanite2026();
            p.Name = "UE5 Dark Fantasy (Cinematic & Grounded)";
            p.Subtitle = "Elden Ring / Witchfire Natural Dark Aesthetic";
            p.Description = "Moody desaturated earth tones, deep contact occlusion, and long-range 1.75x lava/torch/lantern light carry.";

            p.SkyBounceIntensity = 1.08f;
            p.EquatorBounceIntensity = 1.12f;
            p.GroundBounceIntensity = 1.00f;
            p.IndirectSolarBounceIntensity = 0.16f;
            p.PointLightRangeMultiplier = 1.75f;
            p.IndoorAmbientBoost = 0.04f;
            p.IndoorExposureBoost = 0.03f;

            p.AOIntensity = 1.12f;
            p.ContactShadowBlendStrength = 0.78f;

            p.PostExposureOffset = 0.12f;
            p.Contrast = 6.5f;
            p.Saturation = -10.0f;
            p.Temperature = -1.0f;
            p.Tint = 0.0f;
            p.Lift = new Vector4(0.99f, 1.00f, 1.01f, 0.000f);
            p.Gamma = new Vector4(1.00f, 1.00f, 1.00f, 0.01f);
            p.Gain = new Vector4(1.00f, 1.00f, 0.99f, 0.00f);

            p.VignetteIntensity = 0.10f;
            return p;
        }

        public static UE5RenderProfile CreatePhotorealisticNatural()
        {
            UE5RenderProfile p = CreateLumenNanite2026();
            p.Name = "UE5 Photorealistic Natural (Real-Life 4K Photo)";
            p.Subtitle = "ARRI / Sony Full-Frame D65 + Neural Light Reconstruction";
            p.Description = "Maximum real-life photographic realism: extended 1.75x lava/point light carry, natural indoor shadow response, D65 daylight, and crisp 4K micro-detail.";

            p.SkyBounceIntensity = 1.18f;
            p.EquatorBounceIntensity = 1.20f;
            p.GroundBounceIntensity = 1.08f;
            p.IndirectSolarBounceIntensity = 0.20f;
            p.ReflectionSpecularityBoost = 1.00f;
            p.PointLightRangeMultiplier = 1.75f;
            p.IndoorAmbientBoost = 0.10f;
            p.IndoorExposureBoost = 0.08f;

            p.AOIntensity = 0.95f;
            p.ContactShadowBlendStrength = 0.65f;

            p.PostExposureOffset = 0.16f;
            p.Contrast = 4.5f;
            p.Saturation = -6.0f;
            p.Temperature = 0.0f;
            p.Tint = 0.0f;
            p.Lift = new Vector4(1.0f, 1.0f, 1.0f, 0.003f);
            p.Gamma = new Vector4(1.0f, 1.0f, 1.0f, 0.02f);
            p.Gain = new Vector4(1.0f, 1.0f, 1.0f, 0.00f);

            p.BloomIntensity = 0.36f;
            p.BloomThreshold = 1.35f;
            p.VignetteIntensity = 0.04f;
            p.TextureMipMapBias = -0.30f;
            p.NormalMapReliefBoost = 1.08f;
            p.PBRMicroSmoothnessBoost = 1.00f;
            return p;
        }

        public static UE5RenderProfile CreateHighFPSGameplay()
        {
            UE5RenderProfile p = CreateLumenNanite2026();
            p.Name = "UE5 High-FPS Gameplay (140+ FPS Optimized)";
            p.Subtitle = "Photorealistic Visuals Tuned for High-Refresh Monitors";
            p.Description = "Retains 4K photographic color science, 1.65x unclamped light carry, and Lumen GI with lightweight shadow & LOD distances.";

            p.ShadowDistance = 200f;
            p.ContactShadowSamples = 28;
            p.PlanarWaterResolution = 512;
            p.MeshLODBias = 2.2f;
            p.CameraEnvironmentCullDistance = 600.0f;
            p.TerrainPixelError = 3.0f;
            p.GrassDensityMultiplier = 1.0f;
            p.GrassDrawDistance = 190.0f;
            return p;
        }

        public static UE5RenderProfile CreateVanillaOriginal()
        {
            UE5RenderProfile p = new UE5RenderProfile();
            p.Name = "Vanilla (Original Outward)";
            p.Subtitle = "Unmodified Unity 2020.3 Visuals";
            p.Description = "Restores 100% of original Outward Definitive Edition lighting, post-processing, shadow culling, and material settings.";

            p.EnableLumenGI = false;
            p.SkyBounceIntensity = 1.0f;
            p.EquatorBounceIntensity = 1.0f;
            p.GroundBounceIntensity = 1.0f;
            p.IndirectSolarBounceIntensity = 0.0f;
            p.ReflectionSpecularityBoost = 1.0f;
            p.PointLightRangeMultiplier = 1.0f;
            p.IndoorAmbientBoost = 0.0f;
            p.IndoorExposureBoost = 0.0f;

            p.ShadowDistance = 150f;
            p.CascadeSplit0 = 0.02f;
            p.CascadeSplit1 = 0.05f;
            p.CascadeSplit2 = 0.20f;
            p.UncapEnvironmentShadowCulling = false;
            p.EnableVSMContactShadows = true;
            p.ContactShadowSamples = 32;
            p.ContactShadowRange = 0.7f;
            p.ContactShadowZThickness = 0.1f;
            p.ContactShadowBlendStrength = 1.0f;

            p.EnableEnhancedAO = false;
            p.AOIntensity = 0.8f;
            p.AORadius = 0.3f;

            p.EnableACESFilmicGrading = false;
            p.PostExposureOffset = 0.0f;
            p.Contrast = 0.0f;
            p.Saturation = 0.0f;
            p.Temperature = 0.0f;
            p.Tint = 0.0f;
            p.Lift = new Vector4(1f, 1f, 1f, 0f);
            p.Gamma = new Vector4(1f, 1f, 1f, 0f);
            p.Gain = new Vector4(1f, 1f, 1f, 0f);

            p.EnablePhysicalBloom = false;
            p.BloomIntensity = 0.8f;
            p.BloomThreshold = 1.1f;
            p.BloomSoftKnee = 0.5f;
            p.BloomDiffusion = 7.0f;
            p.EnableDynamicAutofocusDoF = false;
            p.DoFAperture = 5.6f;
            p.DoFFocalLength = 50.0f;
            p.EnableMotionBlur = false;
            p.MotionBlurShutterAngle = 90.0f;
            p.VignetteIntensity = 0.0f;
            p.ChromaticAberrationIntensity = 0.0f;
            p.FilmGrainIntensity = 0.0f;

            p.EnableTemporalAA = false;
            p.TAAJitterSpread = 0.75f;
            p.TAAStationaryBlending = 0.95f;
            p.TAAMotionBlending = 0.85f;
            p.TAASharpness = 0.25f;

            p.EnableVolumetricSkyOverhaul = false;
            p.RayleighScatteringBoost = 1.0f;
            p.MieScatteringBoost = 1.0f;
            p.SunDirectionality = 0.7f;
            p.CloudSharpness = 1.0f;
            p.CloudScattering = 1.0f;
            p.GodRayIntensity = 0.4f;
            p.EnableScreenSpaceReflections = false;
            p.SSRMaxMarchDistance = 100.0f;
            p.PlanarWaterResolution = 256;

            p.MeshLODBias = 1.5f;
            p.CameraEnvironmentCullDistance = 200.0f;
            p.TerrainPixelError = 5.0f;
            p.GrassDensityMultiplier = 1.0f;
            p.GrassDrawDistance = 170.0f;
            p.EnablePBRMaterialOverhaul = false;
            p.TextureMipMapBias = 0.0f;
            p.NormalMapReliefBoost = 1.0f;
            p.PBRMicroSmoothnessBoost = 1.0f;
            p.SubsurfaceTranslucencyStrength = 4.0f;
            return p;
        }
    }
}
