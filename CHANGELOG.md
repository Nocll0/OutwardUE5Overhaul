# Changelog

## 1.6.2
- Added labeled Before/After animated A/B comparison GIFs, 1440p side-by-side comparisons, and interactive draggable slider showcase to documentation.

## 1.6.1
- Updated official GitHub repository link (`https://github.com/Nocll0/OutwardUE5Overhaul`) in package manifest.

## 1.6.0
- Fixed a `LightQuality` vertical distance culling bug (`EnabledDistances.y = -1`) where candles, sconces, and indoor point lights remained off until the player stood right next to them.
- Hooked both `LightQuality.Start` and `LightQuality.InitLightSettings` and extended positive light activation distances to `45m+` (`2.50x`).
- Isolated indoor reflection intensity (`0.30`) inside underground city interiors so glossy pottery and wood surfaces no longer reflect the bright outdoor daytime sky cubemap in dark rooms.
- Refined indoor ambient floor and ACES shadow lift so interiors are dynamically lit by actual light sources rather than flat ambient fill.

## 1.5.0
- Removed the 120m range cap on environmental point/spot lights so large volcano/lava lights (`250m–500m`) retain their full reach and gain extended bounce range.
- Added proximity-weighted local light sampling to Lumen GI so nearby lava, torches, candles, and crystals drive warm directional and hemispherical bounce lighting.

## 1.4.0
- Fixed an issue where player lanterns (`ItemLanternVisual`) remained lit when turned off by never overriding `Light.intensity` on dynamically controlled lights.
- Re-calibrated default **UE5 Photorealistic 2026** color grading, saturation, contrast, and mipmap bias for natural photorealism.

## 1.3.0
- Fixed refractive glass flickering under Temporal AA by defaulting to High Subpixel SMAA and safely clearing TAA history buffers on Alt-Tab/focus changes.
- Removed runtime `ReflectionProbe` and `RenderTexture` allocations that caused DX11 driver resets on Alt-Tab.

## 1.2.0
- Added automatic indoor/dungeon/underground city interior detection (`IsCameraInInterior`) and warm torchlight bounce fill.

## 1.1.0
- Fixed interior fog over-density (`GlobalFog`), eliminated auto-exposure brightness oscillation, and ensured `F11` A/B toggle restores 100% vanilla state.

## 1.0.0
- Initial release of the Outward UE5 Graphics Overhaul (Lumen GI, VSM Contact Shadows, Dual-Layer AO, ACES HDR, 2K Water & SSR, Nanite LOD/Terrain & 4K PBR Material Overhaul).
