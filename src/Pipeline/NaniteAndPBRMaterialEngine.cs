using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using uNature.Core.FoliageClasses;
using OutwardUE5Overhaul.Presets;

namespace OutwardUE5Overhaul.Pipeline
{
    public class NaniteAndPBRMaterialEngine
    {
        private struct CachedMaterialProps
        {
            public bool HasBumpScale;
            public float BumpScale;
            public bool HasDetailBumpScale;
            public float DetailBumpScale;
            public bool HasGlossiness;
            public float Glossiness;
            public bool HasGlossMapScale;
            public float GlossMapScale;
            public bool HasSmoothness;
            public float Smoothness;
            public bool HasOcclusionStrength;
            public float OcclusionStrength;
        }

        private struct CachedTextureProps
        {
            public float MipMapBias;
            public int AnisoLevel;
        }

        private readonly Dictionary<int, CachedMaterialProps> _cachedMaterials = new Dictionary<int, CachedMaterialProps>();
        private readonly Dictionary<int, CachedTextureProps> _cachedTextures = new Dictionary<int, CachedTextureProps>();

        private readonly FieldInfo _fiTMQMLodDistances;
        private readonly FieldInfo _fiTMQMBaseLodDistances;

        private static readonly string[] TexturePropNames = new string[]
        {
            "_MainTex", "_BumpMap", "_DetailAlbedoMap", "_DetailNormalMap",
            "_MetallicGlossMap", "_SpecGlossMap", "_OcclusionMap", "_ParallaxMap",
            "_Splat0", "_Splat1", "_Splat2", "_Splat3",
            "_Normal0", "_Normal1", "_Normal2", "_Normal3"
        };

        public NaniteAndPBRMaterialEngine()
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            _fiTMQMLodDistances = typeof(TerrainMeshQualityManager).GetField("m_sqrLodDistances", flags);
            _fiTMQMBaseLodDistances = typeof(TerrainMeshQualityManager).GetField("BASE_LOD_DISTANCES", flags);
        }

        public void ApplyNaniteWorldAndFoliage(UE5RenderProfile profile)
        {
            if (profile == null) return;

            QualitySettings.masterTextureLimit = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.lodBias = Mathf.Clamp(profile.MeshLODBias, 0.8f, 4.5f);
            QualitySettings.maximumLODLevel = 0;
            QualitySettings.skinWeights = SkinWeights.FourBones;
            QualitySettings.softParticles = true;
            Shader.globalMaximumLOD = 1000;

            Terrain[] terrains = UnityEngine.Object.FindObjectsOfType<Terrain>();
            if (terrains != null)
            {
                for (int i = 0; i < terrains.Length; i++)
                {
                    Terrain t = terrains[i];
                    if (t == null) continue;
                    t.heightmapPixelError = Mathf.Clamp(profile.TerrainPixelError, 1.0f, 20.0f);
                    t.basemapDistance = profile.EnablePBRMaterialOverhaul ? 2500f : 1500f;
                    t.detailObjectDistance = Mathf.Clamp(profile.GrassDrawDistance, 80f, 300f);
                    t.detailObjectDensity = Mathf.Clamp01(profile.GrassDensityMultiplier);
                }
            }

            if (TerrainMeshQualityManager.Instance != null)
            {
                TerrainMeshQualityManager tmqm = TerrainMeshQualityManager.Instance;
                tmqm.Lod0Shadows = ShadowCastingMode.On;
                tmqm.Lod1Shadows = ShadowCastingMode.On;

                if (_fiTMQMLodDistances != null && _fiTMQMBaseLodDistances != null)
                {
                    float[] baseDists = _fiTMQMBaseLodDistances.GetValue(tmqm) as float[];
                    if (baseDists != null && baseDists.Length > 0)
                    {
                        float mult = profile.EnablePBRMaterialOverhaul ? 1.65f : 1.0f;
                        float[] newDists = new float[baseDists.Length];
                        for (int i = 0; i < baseDists.Length; i++)
                        {
                            newDists[i] = baseDists[i] * mult;
                        }
                        _fiTMQMLodDistances.SetValue(tmqm, newDists);
                    }
                }
            }

            if (FoliageCore_MainManager.instance != null)
            {
                FoliageCore_MainManager fm = FoliageCore_MainManager.instance;
                fm.density = Mathf.Clamp01(profile.GrassDensityMultiplier);
                fm.globalFadeDistance = Mathf.RoundToInt(Mathf.Clamp(profile.GrassDrawDistance, 100f, 280f));
            }
        }

        public void ApplyPBRMaterialsAndTextures(UE5RenderProfile profile)
        {
            if (profile == null) return;

            UBER_DeferredTranslucencyParams[] translucencyList = UnityEngine.Object.FindObjectsOfType<UBER_DeferredTranslucencyParams>();
            if (translucencyList != null)
            {
                for (int i = 0; i < translucencyList.Length; i++)
                {
                    UBER_DeferredTranslucencyParams ut = translucencyList[i];
                    if (ut == null) continue;
                    ut.Strength = Mathf.Clamp(profile.SubsurfaceTranslucencyStrength, 1.0f, 8.0f);
                }
            }

            Renderer[] renderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
            if (renderers == null) return;

            HashSet<int> visitedMats = new HashSet<int>();
            HashSet<int> visitedTexs = new HashSet<int>();

            int uiLayer = 5;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || r.gameObject == null) continue;
                if (r.gameObject.layer == uiLayer) continue;
                if (r is ParticleSystemRenderer || r is SpriteRenderer) continue;

                Material[] mats = r.sharedMaterials;
                if (mats == null) continue;

                for (int m = 0; m < mats.Length; m++)
                {
                    Material mat = mats[m];
                    if (mat == null || mat.shader == null) continue;

                    int matId = mat.GetInstanceID();
                    if (visitedMats.Contains(matId)) continue;
                    visitedMats.Add(matId);

                    string shaderName = mat.shader.name ?? string.Empty;
                    if (shaderName.StartsWith("UI/") || shaderName.StartsWith("Sprites/") || shaderName.Contains("TextMesh") || shaderName.Contains("Particles"))
                        continue;

                    UpgradeMaterialPBR(mat, matId, profile, visitedTexs);
                }
            }

            Terrain[] terrains = UnityEngine.Object.FindObjectsOfType<Terrain>();
            if (terrains != null)
            {
                for (int i = 0; i < terrains.Length; i++)
                {
                    Terrain t = terrains[i];
                    if (t == null) continue;
                    if (t.materialTemplate != null)
                    {
                        int matId = t.materialTemplate.GetInstanceID();
                        if (!visitedMats.Contains(matId))
                        {
                            visitedMats.Add(matId);
                            UpgradeMaterialPBR(t.materialTemplate, matId, profile, visitedTexs);
                        }
                    }
                    if (t.terrainData != null && t.terrainData.terrainLayers != null)
                    {
                        TerrainLayer[] layers = t.terrainData.terrainLayers;
                        for (int l = 0; l < layers.Length; l++)
                        {
                            TerrainLayer tl = layers[l];
                            if (tl == null) continue;
                            if (tl.diffuseTexture != null) UpgradeTextureSharpness(tl.diffuseTexture, profile, visitedTexs);
                            if (tl.normalMapTexture != null) UpgradeTextureSharpness(tl.normalMapTexture, profile, visitedTexs);
                        }
                    }
                }
            }
        }

        private void UpgradeMaterialPBR(Material mat, int matId, UE5RenderProfile profile, HashSet<int> visitedTexs)
        {
            CachedMaterialProps orig;
            if (!_cachedMaterials.TryGetValue(matId, out orig))
            {
                orig = new CachedMaterialProps
                {
                    HasBumpScale = mat.HasProperty("_BumpScale"),
                    BumpScale = mat.HasProperty("_BumpScale") ? mat.GetFloat("_BumpScale") : 1.0f,
                    HasDetailBumpScale = mat.HasProperty("_DetailNormalMapScale"),
                    DetailBumpScale = mat.HasProperty("_DetailNormalMapScale") ? mat.GetFloat("_DetailNormalMapScale") : 1.0f,
                    HasGlossiness = mat.HasProperty("_Glossiness"),
                    Glossiness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f,
                    HasGlossMapScale = mat.HasProperty("_GlossMapScale"),
                    GlossMapScale = mat.HasProperty("_GlossMapScale") ? mat.GetFloat("_GlossMapScale") : 1.0f,
                    HasSmoothness = mat.HasProperty("_Smoothness"),
                    Smoothness = mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0.5f,
                    HasOcclusionStrength = mat.HasProperty("_OcclusionStrength"),
                    OcclusionStrength = mat.HasProperty("_OcclusionStrength") ? mat.GetFloat("_OcclusionStrength") : 1.0f
                };
                _cachedMaterials[matId] = orig;
            }

            // Always preserve original _OcclusionStrength so materials with unbaked/disabled occlusion maps
            // (such as interior wooden ceiling beams and stonework) never lose ambient light!
            if (orig.HasOcclusionStrength)
            {
                mat.SetFloat("_OcclusionStrength", orig.OcclusionStrength);
            }

            if (!profile.EnablePBRMaterialOverhaul)
            {
                if (orig.HasBumpScale) mat.SetFloat("_BumpScale", orig.BumpScale);
                if (orig.HasDetailBumpScale) mat.SetFloat("_DetailNormalMapScale", orig.DetailBumpScale);
                if (orig.HasGlossiness) mat.SetFloat("_Glossiness", orig.Glossiness);
                if (orig.HasGlossMapScale) mat.SetFloat("_GlossMapScale", orig.GlossMapScale);
                if (orig.HasSmoothness) mat.SetFloat("_Smoothness", orig.Smoothness);
            }
            else
            {
                float bumpMult = Mathf.Clamp(profile.NormalMapReliefBoost, 0.5f, 2.0f);
                float smoothMult = Mathf.Clamp(profile.PBRMicroSmoothnessBoost, 0.5f, 1.5f);

                if (orig.HasBumpScale && orig.BumpScale > 0.01f)
                {
                    mat.SetFloat("_BumpScale", Mathf.Clamp(orig.BumpScale * bumpMult, 0.1f, 2.0f));
                }
                if (orig.HasDetailBumpScale && orig.DetailBumpScale > 0.01f)
                {
                    mat.SetFloat("_DetailNormalMapScale", Mathf.Clamp(orig.DetailBumpScale * bumpMult, 0.1f, 2.0f));
                }
                if (orig.HasGlossiness && orig.Glossiness > 0.02f && orig.Glossiness < 0.85f)
                {
                    mat.SetFloat("_Glossiness", Mathf.Clamp(orig.Glossiness * smoothMult, 0.05f, 0.88f));
                }
                if (orig.HasGlossMapScale && orig.GlossMapScale > 0.02f && orig.GlossMapScale < 0.85f)
                {
                    mat.SetFloat("_GlossMapScale", Mathf.Clamp(orig.GlossMapScale * smoothMult, 0.05f, 0.88f));
                }
                if (orig.HasSmoothness && orig.Smoothness > 0.02f && orig.Smoothness < 0.85f)
                {
                    mat.SetFloat("_Smoothness", Mathf.Clamp(orig.Smoothness * smoothMult, 0.05f, 0.88f));
                }
            }

            for (int t = 0; t < TexturePropNames.Length; t++)
            {
                string prop = TexturePropNames[t];
                if (mat.HasProperty(prop))
                {
                    Texture tex = mat.GetTexture(prop);
                    if (tex != null)
                    {
                        UpgradeTextureSharpness(tex, profile, visitedTexs);
                    }
                }
            }
        }

        private void UpgradeTextureSharpness(Texture tex, UE5RenderProfile profile, HashSet<int> visitedTexs)
        {
            int texId = tex.GetInstanceID();
            if (visitedTexs.Contains(texId)) return;
            visitedTexs.Add(texId);

            CachedTextureProps orig;
            if (!_cachedTextures.TryGetValue(texId, out orig))
            {
                orig = new CachedTextureProps
                {
                    MipMapBias = tex.mipMapBias,
                    AnisoLevel = tex.anisoLevel
                };
                _cachedTextures[texId] = orig;
            }

            if (!profile.EnablePBRMaterialOverhaul)
            {
                tex.mipMapBias = orig.MipMapBias;
                tex.anisoLevel = orig.AnisoLevel;
            }
            else
            {
                tex.anisoLevel = 16;
                tex.mipMapBias = Mathf.Clamp(profile.TextureMipMapBias, -1.2f, 0.5f);
            }
        }
    }
}
