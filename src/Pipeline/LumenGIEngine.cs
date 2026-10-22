using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using OutwardUE5Overhaul.Presets;

namespace OutwardUE5Overhaul.Pipeline
{
    public class LumenGIEngine
    {
        private struct CachedLightQualityProps
        {
            public Vector2 EnabledDistances;
        }

        private readonly Dictionary<int, float> _cachedLightRanges = new Dictionary<int, float>();
        private readonly Dictionary<int, CachedLightQualityProps> _cachedLightQualities = new Dictionary<int, CachedLightQualityProps>();

        private readonly FieldInfo _fiLQLight;
        private readonly FieldInfo _fiLQEnabledDistSqr;

        private bool _hasTrueVanillaAmbient = false;
        private Color _trueVanillaAmbientLight = new Color(0.25f, 0.25f, 0.28f, 1f);
        private Color _lastAppliedSkyColor = new Color(-1f, -1f, -1f, -1f);

        public LumenGIEngine()
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            _fiLQLight = typeof(LightQuality).GetField("m_light", flags);
            _fiLQEnabledDistSqr = typeof(LightQuality).GetField("m_enabledDistancesSqr", flags);
        }

        public static bool IsCameraInInterior(Camera cam)
        {
            if (cam == null) cam = GetActiveGameplayCamera();
            if (cam == null) return false;

            Vector3 pos = cam.transform.position;
            if (pos.y < -150f) return true;

            try
            {
                if (InteriorArea.GetInterior(pos) != null)
                    return true;
            }
            catch
            {
            }

            return false;
        }

        public void RecordVanillaAmbientIfChanged()
        {
            Color current = RenderSettings.ambientLight;
            if (!_hasTrueVanillaAmbient || !ColorsApproxEqual(current, _lastAppliedSkyColor))
            {
                if (current.maxColorComponent > 0.005f || !_hasTrueVanillaAmbient)
                {
                    _trueVanillaAmbientLight = current;
                    _hasTrueVanillaAmbient = true;
                }
            }
        }

        public void ApplyAtmosphereGI(UE5RenderProfile profile)
        {
            if (profile == null) return;

            RecordVanillaAmbientIfChanged();

            if (!profile.EnableLumenGI)
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                if (_hasTrueVanillaAmbient)
                {
                    RenderSettings.ambientLight = _trueVanillaAmbientLight;
                }
                RenderSettings.reflectionIntensity = 1.0f;
                return;
            }

            Camera mainCam = GetActiveGameplayCamera();
            bool isInterior = IsCameraInInterior(mainCam);

            Color baseAmbient = _trueVanillaAmbientLight;
            Color fogCol = RenderSettings.fogColor;

            float snowFactor = 0f;
            float autumnFactor = 0f;

            if (EnvironmentConditions.Instance != null)
            {
                snowFactor = Mathf.Clamp01(EnvironmentConditions.Instance.SnowQuantity);
                autumnFactor = Mathf.Clamp01(EnvironmentConditions.Instance.AutumnLerp);
            }

            if (isInterior)
            {
                // Inside interiors:
                // 1. Do NOT flood the room with artificial flat ambient light so wood & walls respond dynamically
                //    to actual candles, sconces, fireplaces, and lanterns!
                // 2. Lower RenderSettings.reflectionIntensity to 0.30f indoors so glossy vases and wood in unlit
                //    corners do NOT reflect the bright outdoor daytime sky cubemap and glow in the dark!
                float indoorMult = 1.0f + Mathf.Clamp(profile.IndoorAmbientBoost, 0.0f, 0.50f);
                Color indoorSky = ClampHDRColor(baseAmbient * indoorMult, 0.85f);
                Color indoorEq = ClampHDRColor(baseAmbient * (indoorMult * 0.96f), 0.85f);
                Color indoorGnd = ClampHDRColor(baseAmbient * (indoorMult * 0.88f), 0.75f);

                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = indoorSky;
                RenderSettings.ambientEquatorColor = indoorEq;
                RenderSettings.ambientGroundColor = indoorGnd;
                RenderSettings.reflectionIntensity = 0.30f;
                _lastAppliedSkyColor = indoorSky;
                return;
            }

            // Outdoor Hemispherical Trilight GI
            Color skyBounce = baseAmbient * profile.SkyBounceIntensity;

            // Horizon Equator Bounce (lights vertical cliffs, buildings, trees, characters)
            Color equatorBase = Color.Lerp(baseAmbient, fogCol.maxColorComponent > 0.05f ? fogCol : baseAmbient, 0.15f);
            Color equatorBounce = equatorBase * profile.EquatorBounceIntensity;

            // Ground Albedo Bounce (lights overhangs, undersides of rocks, doorways)
            Color earthTint = Color.Lerp(
                new Color(0.96f, 0.92f, 0.86f, 1f),
                new Color(0.98f, 0.90f, 0.82f, 1f),
                autumnFactor);
            Color snowTint = new Color(0.98f, 0.99f, 1.02f, 1f);
            Color surfaceTint = Color.Lerp(earthTint, snowTint, snowFactor);

            Color groundBounce = (baseAmbient * surfaceTint) * profile.GroundBounceIntensity;

            Color finalSky = ClampHDRColor(skyBounce, 1.35f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = finalSky;
            RenderSettings.ambientEquatorColor = ClampHDRColor(equatorBounce, 1.30f);
            RenderSettings.ambientGroundColor = ClampHDRColor(groundBounce, 1.15f);
            RenderSettings.reflectionIntensity = Mathf.Clamp(profile.ReflectionSpecularityBoost, 0.5f, 1.25f);
            _lastAppliedSkyColor = finalSky;
        }

        /// <summary>
        /// Extends point & spot light range ("Light Carry") and LightQuality cull distances across the scene
        /// so all candles, sconces, torches, and volcano lava lights stay active from afar and carry naturally.
        /// </summary>
        public void ApplySceneLightsOverhaul(UE5RenderProfile profile)
        {
            if (profile == null) return;

            // Ensure Unity never drops overlapping point/spot lights (candles, sconces, lava, torches) to vertex lighting
            QualitySettings.pixelLightCount = profile.EnableLumenGI ? 32 : 4;

            LightQuality[] lqList = UnityEngine.Object.FindObjectsOfType<LightQuality>();
            if (lqList != null)
            {
                for (int i = 0; i < lqList.Length; i++)
                {
                    LightQuality lq = lqList[i];
                    if (lq != null)
                    {
                        ProcessLightQualityInstance(lq, profile);
                    }
                }
            }

            Light[] allLights = UnityEngine.Object.FindObjectsOfType<Light>();
            if (allLights != null)
            {
                for (int i = 0; i < allLights.Length; i++)
                {
                    Light l = allLights[i];
                    if (l == null) continue;
                    if (l.type == LightType.Directional) continue;

                    UpgradePointOrSpotLightRangeOnly(l, profile);
                }
            }
        }

        public void ProcessLightQualityInstance(LightQuality lq, UE5RenderProfile profile)
        {
            if (lq == null || profile == null) return;

            int lqId = lq.GetInstanceID();
            CachedLightQualityProps origLQ;
            if (!_cachedLightQualities.TryGetValue(lqId, out origLQ))
            {
                origLQ = new CachedLightQualityProps
                {
                    EnabledDistances = lq.EnabledDistances
                };
                _cachedLightQualities[lqId] = origLQ;
            }

            // CRITICAL FIX:
            // In Outward's LightQuality, EnabledDistances.y is -1f when vertical culling is disabled!
            // Previously, squaring targetDist.y turned (-1.65)^2 into +2.72f, which activated a 1.65m vertical culling
            // cutoff that turned all candles and sconces OFF until the player stood right next to them!
            // Never square a negative EnabledDistances component, and extend active light culling to 2.5x distance (min 45m)!
            float distMult = profile.EnableLumenGI ? Mathf.Max(2.2f, profile.PointLightRangeMultiplier * 1.5f) : 1.0f;

            float origX = origLQ.EnabledDistances.x;
            float origY = origLQ.EnabledDistances.y;

            float targetX = (origX > 0f)
                ? (profile.EnableLumenGI ? Mathf.Max(45f, origX * distMult) : origX)
                : origX;
            float targetY = (origY > 0f)
                ? (profile.EnableLumenGI ? Mathf.Max(30f, origY * distMult) : origY)
                : origY;

            float sqrX = (targetX > 0f) ? (targetX * targetX) : -1f;
            float sqrY = (targetY > 0f) ? (targetY * targetY) : -1f;

            if (_fiLQEnabledDistSqr != null)
            {
                _fiLQEnabledDistSqr.SetValue(lq, new Vector2(sqrX, sqrY));
            }

            Light light = (_fiLQLight != null) ? (_fiLQLight.GetValue(lq) as Light) : lq.GetComponent<Light>();
            if (light != null && light.type != LightType.Directional)
            {
                UpgradePointOrSpotLightRangeOnly(light, profile);
            }
        }

        private void UpgradePointOrSpotLightRangeOnly(Light light, UE5RenderProfile profile)
        {
            if (light == null || light.type == LightType.Directional) return;

            int lightId = light.GetInstanceID();
            float origRange;
            if (!_cachedLightRanges.TryGetValue(lightId, out origRange))
            {
                origRange = light.range;
                _cachedLightRanges[lightId] = origRange;
            }

            if (!profile.EnableLumenGI)
            {
                light.range = origRange;
                return;
            }

            float rangeMult = Mathf.Clamp(profile.PointLightRangeMultiplier, 1.0f, 2.5f);
            light.range = Mathf.Max(origRange, Mathf.Clamp(origRange * rangeMult, 1.0f, 2500.0f));
        }

        public static Camera GetActiveGameplayCamera()
        {
            if (CharacterCamera.CharCamList != null && CharacterCamera.CharCamList.Count > 0)
            {
                CharacterCamera cc = CharacterCamera.CharCamList[0];
                if (cc != null && cc.CameraScript != null)
                    return cc.CameraScript;
            }
            return Camera.main;
        }

        private static bool ColorsApproxEqual(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.002f &&
                   Mathf.Abs(a.g - b.g) < 0.002f &&
                   Mathf.Abs(a.b - b.b) < 0.002f;
        }

        private static Color ClampHDRColor(Color c, float maxChannel)
        {
            c.r = Mathf.Clamp(c.r, 0f, maxChannel);
            c.g = Mathf.Clamp(c.g, 0f, maxChannel);
            c.b = Mathf.Clamp(c.b, 0f, maxChannel);
            c.a = 1f;
            return c;
        }
    }
}
