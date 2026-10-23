# Outward UE5 Graphics Overhaul (2026 Photorealistic Edition)

A complete engine-level graphics overhaul for **Outward Definitive Edition** that transforms lighting, global illumination, shadows, ambient occlusion, color grading, reflections, water, terrain tessellation, foliage, and PBR materials to deliver a **2026 Unreal Engine 5 / Neural-Relit Photorealistic** visual pipeline.

---

## Visual Comparisons (Before vs. After)

> **Interactive Draggable Slider Showcase**: [**Click here to open the live draggable Before/After slider page in your browser**](https://htmlpreview.github.io/?https://github.com/Nocll0/OutwardUE5Overhaul/blob/main/docs/index.html)

Each comparison below includes an **automatic A/B toggle animation** (labeled **`BEFORE: VANILLA OUTWARD`** vs. **`AFTER: UE5 2026 OVERHAUL`** in the top-left corner) followed by **side-by-side full-resolution (`2560x1440`) static screenshots** you can click to enlarge.

---

### 1. Stormy Sunset, Volumetric Rain Scatter & Stone PBR Relief
*Per-pixel HDR sunset scattering through rain, enhanced stone masonry normal relief (`+35%`), and warm emissive particle bounce.*

![Comparison 1 Animated A/B](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/compare1.gif)

| **BEFORE — Vanilla Outward** *(Click for 1440p)* | **AFTER — UE5 2026 Overhaul** *(Click for 1440p)* |
| :---: | :---: |
| [![Before 1](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/before1.jpg)](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/before1.jpg) | [![After 1](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/after1.jpg)](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/after1.jpg) |

---

### 2. Night Sky, Lighthouse Beacon & Physical HDR Bloom
*Natural ACES night sky contrast, volumetric cyan beacon glow on the lighthouse structure, and wide-diffusion physical HDR bloom.*

![Comparison 2 Animated A/B](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/compare2.gif)

| **BEFORE — Vanilla Outward** *(Click for 1440p)* | **AFTER — UE5 2026 Overhaul** *(Click for 1440p)* |
| :---: | :---: |
| [![Before 2](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/before2.jpg)](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/before2.jpg) | [![After 2](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/after2.jpg)](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/after2.jpg) |

---

### 3. Berg Night Courtyard — Warm Lantern Falloff & Cobblestone PBR Relief
*Extended `2.0x` point-light range and warm radiosity bounce replace vanilla's flat cold wash while bringing out deep cobblestone and woodwork micro-relief.*

![Comparison 3 Animated A/B](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/compare3.gif)

| **BEFORE — Vanilla Outward** *(Click for 1440p)* | **AFTER — UE5 2026 Overhaul** *(Click for 1440p)* |
| :---: | :---: |
| [![Before 3](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/before3.jpg)](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/before3.jpg) | [![After 3](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/after3.jpg)](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/after3.jpg) |

---

### 4. City Alleyway — Extended Light Activation & Dual-Layer AO
*Distant streetlamps remain active across the entire street (`45m+` activation) while MSVO + SSAO ground stonework and balustrades.*

![Comparison 4 Animated A/B](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/compare4.gif)

| **BEFORE — Vanilla Outward** *(Click for 1440p)* | **AFTER — UE5 2026 Overhaul** *(Click for 1440p)* |
| :---: | :---: |
| [![Before 4](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/before4.jpg)](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/before4.jpg) | [![After 4](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/after4.jpg)](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/after4.jpg) |

---

### 5. Berg Player House Interior — Firepit & Candelabra Multi-Bounce GI
*Isolated indoor cubemap reflections and warm fire/candle radiosity bounce across stone masonry, wooden floorboards, and brass fixtures.*

![Comparison 7 Animated A/B](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/compare7.gif)

| **BEFORE — Vanilla Outward** *(Click for 1440p)* | **AFTER — UE5 2026 Overhaul** *(Click for 1440p)* |
| :---: | :---: |
| [![Before 7](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/before7.jpg)](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/before7.jpg) | [![After 7](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/after7.jpg)](https://raw.githubusercontent.com/Nocll0/OutwardUE5Overhaul/main/media/after7.jpg) |

---

## Requirements & Dependencies

- **Outward Definitive Edition**
- **[BepInExPack Outward](https://thunderstore.io/c/outward/p/BepInEx/BepInExPack_Outward/) (`BepInEx-BepInExPack_Outward-5.4.19`)** — *Automatically installed when using **r2modman** or **Thunderstore Mod Manager**.*
- *(Optional)* **[Outward Config Manager](https://thunderstore.io/c/outward/p/Mefino/Outward_Config_Manager/)** — Fully supported (`F5` in-game), in addition to the built-in **UE5 Graphics Studio (`F10`)**.

---

## In-Game Hotkeys

- **`F10`** — Open/Close the **Unreal Engine 5 Graphics Studio** live control window (real-time preset switching, FPS/frame-time telemetry, and per-subsystem sliders).
- **`F11`** — **Instant A/B Toggle** between the active **UE5 Overhaul** preset and **100% Vanilla Outward** visuals in real time.
- **`F5`** — Standard **Outward Config Manager** menu integration.

---

## Included Visual Presets (Switchable Live via `F10`)

1. **UE5 Photorealistic 2026 (Default)** — Neutral AgX/ACES natural daylight & true-to-life indoor candle/torch bounce lighting without artificial oversaturation or over-sharpening.
2. **UE5 Cinema (Quality)** — Richer atmospheric warmth and deeper cinematic contrast.
3. **UE5 Archviz Ultra (600m)** — Maximum 600m shadow cascades, 4x LOD bias, and ultra-crisp 1.0px terrain tessellation for high-end GPUs and 4K screenshots.
4. **UE5 Balanced (High FPS)** — Retains the full Lumen GI, ACES HDR, and PBR material overhaul with lighter screen-space raymarching for high refresh rates.

---

## Core Visual Pillars

### 1. Lumen Global Illumination & Multi-Bounce Radiosity
- **Hemispherical Trilight Sky-Equator-Ground GI**: Replaces vanilla Outward's flat uniform ambient light with dynamic physically-based `AmbientMode.Trilight` driven by Rayleigh sky zenith scatter, horizon solar scatter, and regional/seasonal ground albedo bounce.
- **Secondary Solar/Lunar & Indoor Torch Radiosity Bounce**: Spawns a dynamic directional bounce light that adapts outdoors to sun/moon terrain bounce and indoors to nearby warm point/spot lights (lanterns, candles, sconces, lava, and magical crystals).
- **Extended Local Light Falloff & Distance Activation**: Extends indoor and environmental point/spot light ranges (`2.0x`) and `LightQuality` activation distances (`2.5x`) while preserving large environmental lava/volcano lights and soft shadow maps.
- **Indoor Reflection & Cubemap Isolation**: Automatically attenuates outdoor sky cubemap bleed inside underground city interiors so wood, stone, and pottery react naturally to local candles and fires.

### 2. Virtual Shadow Maps (VSM) & Dual-Layer Ambient Occlusion
- **Uncapped 4-Cascade Shadows**: Pushes directional shadow distance up to `380m–600m` (`ShadowResolution.VeryHigh`, `ShadowProjection.StableFit`) and removes vanilla Outward's 50% shadow distance cutoff on environment geometry.
- **36-Sample Contact Shadows + Day/Night Bug Fix**: Upgrades screen-space contact shadows (`SEScreenSpaceShadows`) for micro-detail grounding on grass, rocks, masonry, and armor crevices, and fixes a vanilla bug where contact shadows stopped updating after day/night transitions.
- **Dual-Layer Ambient Occlusion**: Combines DX11 Compute Shader **Multi-Scale Volumetric Obscurance (MSVO)** for wide room/crevice shading with bilateral **`SSAOPro`** for tight contact occlusion.

### 3. ACES Filmic HDR Color Pipeline & Physical Camera
- **HDR ACES Filmic Tonemapper**: Enables `GradingMode.HighDefinitionRange` with `Tonemapper.ACES` and natural photorealistic 3-way `Lift`/`Gamma`/`Gain` color balance.
- **Fixed Physical Camera Exposure**: Eliminates vanilla auto-exposure brightness pumping/oscillation with stable day/night and indoor/outdoor exposure calibration.
- **Dual-Stage Physical HDR Bloom**: Smooth wide-diffusion HDR bloom (`diffusion = 8.4`) paired with subtle lens scatter.
- **Safe Subpixel SMAA / Optional TSR**: Defaults to flicker-free High SMAA to keep refractive glass/lantern shaders stable on AMD RDNA3 and NVIDIA GPUs, with optional Temporal AA and Dynamic Autofocus Depth of Field toggleable in `F10`.

### 4. Volumetric Atmosphere, God Rays, 2K Planar Water & SSR
- **Per-Pixel HDR Sky & Clouds**: Upgrades `TOD_Sky` to `PerPixel` HDR quality with natural Rayleigh/Mie scattering, cloud shadows (`TOD_Shadows`), and depth-aware volumetric god rays (`TOD_Rays`).
- **2048x2048 Planar Water & Screen-Space Reflections**: Upgrades `WaterRender` planar reflection and refraction render textures from vanilla `256x256` to `2048x2048` and enables smooth screen-space reflections on wet and metallic surfaces.

### 5. Nanite-Style LODs, Ultra Foliage & 4K-Grade PBR Material Overhaul
- **Nanite-Style LOD & Terrain Tessellation**: Raises `QualitySettings.lodBias` up to `3.6x–4.0x`, uncaps camera environment layer culling from `200m` to `1600m`, and sets terrain `heightmapPixelError = 1.0f` for maximum geometric fidelity.
- **Vegetation Studio Pro & uNature Overhaul**: Expands grass, plant, and tree draw distances (`320m+`) and enables full foliage shadow casting.
- **16x Anisotropic Filtering & Normal Map Relief**: Applies `16x` anisotropic filtering and `-0.50` negative mipmap bias to 3D world textures, boosts normal map 3D relief (`_BumpScale`, `_DetailNormalMapScale`) by `+35%`, and enhances `UBER` deferred subsurface translucency.

---

## Installation

### Recommended: Using r2modman / Thunderstore Mod Manager
1. Open **r2modman** (or Thunderstore Mod Manager / Gale) and select **Outward Definitive Edition**.
2. Find **Outward_UE5_Overhaul** in the **Online** tab and click **Download** (this automatically downloads **BepInExPack_Outward** if not already installed).
3. Click **Start Modded** and press **`F10`** in-game to open the **UE5 Graphics Studio** or **`F11`** to toggle A/B comparison.

### Manual Installation
1. Ensure **[BepInExPack_Outward](https://thunderstore.io/c/outward/p/BepInEx/BepInExPack_Outward/)** is installed in your `Outward\Outward_Defed` folder.
2. Extract the `plugins` folder from this archive into `Outward\Outward_Defed\BepInEx\` (so that `OutwardUE5Overhaul.dll` is inside `BepInEx\plugins\`).
