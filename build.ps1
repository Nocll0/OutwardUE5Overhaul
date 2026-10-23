$ErrorActionPreference = "Stop"

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$gameDir = "C:\Program Files (x86)\Steam\steamapps\common\Outward\Outward_Defed"
$managedDir = "$gameDir\Outward Definitive Edition_Data\Managed"
$bepInExProfileDir = "$env:APPDATA\r2modmanPlus-local\OutwardDe\profiles\outward\BepInEx"
$bepInExCoreDir = "$bepInExProfileDir\core"

$projectDir = "c:\argon4\OutwardUE5Overhaul"
$outDir = "$projectDir\bin"
$distDir = "$projectDir\dist"
$tsStageDir = "$projectDir\thunderstore_package"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
New-Item -ItemType Directory -Force -Path $distDir | Out-Null

$outDll = "$outDir\OutwardUE5Overhaul.dll"

$refs = @(
    "$bepInExCoreDir\BepInEx.dll",
    "$bepInExCoreDir\0Harmony.dll",
    "$managedDir\Assembly-CSharp.dll",
    "$managedDir\Assembly-CSharp-firstpass.dll",
    "$managedDir\Unity.Postprocessing.Runtime.dll",
    "$managedDir\AwesomeTechnologies.VegetationStudioPro.Runtime.dll",
    "$managedDir\UnityEngine.dll",
    "$managedDir\UnityEngine.CoreModule.dll",
    "$managedDir\UnityEngine.PhysicsModule.dll",
    "$managedDir\UnityEngine.TerrainModule.dll",
    "$managedDir\UnityEngine.IMGUIModule.dll",
    "$managedDir\UnityEngine.InputLegacyModule.dll",
    "$managedDir\UnityEngine.TextRenderingModule.dll",
    "$managedDir\UnityEngine.UI.dll",
    "$managedDir\UnityEngine.ParticleSystemModule.dll"
)

$refArgs = $refs | ForEach-Object { "/r:`"$_`"" }
$sources = Get-ChildItem -Path "$projectDir\src" -Filter "*.cs" -Recurse | ForEach-Object { "`"$($_.FullName)`"" }

Write-Host "Compiling OutwardUE5Overhaul.dll..."
& $csc /noconfig /target:library /out:"$outDll" /optimize+ /nostdlib /r:"$managedDir\mscorlib.dll" /r:"$managedDir\System.dll" /r:"$managedDir\System.Core.dll" $refArgs $sources

if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE"
}
Write-Host "Compilation succeeded: $outDll"

# Create manifest.json (strictly UTF-8 without BOM and description <= 250 chars for Thunderstore validator)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$manifestJson = @"
{
  "name": "Outward_UE5_Overhaul",
  "version_number": "1.6.2",
  "website_url": "https://github.com/Nocll0/OutwardUE5Overhaul",
  "description": "Complete Unreal Engine 5 Graphics Overhaul for Outward Definitive Edition: Lumen GI & Solar/Torch Bounce, VSM Contact Shadows, Dual-Layer AO, ACES Filmic HDR, 2K Water & SSR, and Nanite LOD/4K PBR.",
  "dependencies": [
    "BepInEx-BepInExPack_Outward-5.4.19"
  ]
}
"@
[System.IO.File]::WriteAllText("$outDir\manifest.json", $manifestJson, $utf8NoBom)

# Generate 256x256 icon.png using System.Drawing
Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap(256, 256)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$rect = New-Object System.Drawing.Rectangle(0, 0, 256, 256)
$brushBg = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rect,
    [System.Drawing.Color]::FromArgb(255, 14, 20, 34),
    [System.Drawing.Color]::FromArgb(255, 28, 78, 168),
    45.0
)
$g.FillRectangle($brushBg, $rect)
$penBorder = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 80, 175, 255), 6.0)
$g.DrawRectangle($penBorder, 6, 6, 244, 244)
$fontMain = New-Object System.Drawing.Font("Segoe UI", 46, [System.Drawing.FontStyle]::Bold)
$fontSub = New-Object System.Drawing.Font("Segoe UI", 18, [System.Drawing.FontStyle]::Bold)
$sf = New-Object System.Drawing.StringFormat
$sf.Alignment = [System.Drawing.StringAlignment]::Center
$sf.LineAlignment = [System.Drawing.StringAlignment]::Center
$g.DrawString("UE5", $fontMain, [System.Drawing.Brushes]::White, [System.Drawing.RectangleF]::new(0, 35, 256, 110), $sf)
$g.DrawString("2026 OVERHAUL", $fontSub, [System.Drawing.Brushes]::LightSkyBlue, [System.Drawing.RectangleF]::new(0, 145, 256, 60), $sf)
$g.Dispose()
$bmp.Save("$outDir\icon.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# Deploy to r2modman profiles
$profilesRoot = "$env:APPDATA\r2modmanPlus-local\OutwardDe\profiles"
if (Test-Path $profilesRoot) {
    Get-ChildItem -Path $profilesRoot -Directory | ForEach-Object {
        $pDir = "$($_.FullName)\BepInEx\plugins\Argon-Outward_UE5_Overhaul"
        New-Item -ItemType Directory -Force -Path $pDir | Out-Null
        try {
            Copy-Item -Path "$outDir\OutwardUE5Overhaul.dll" -Destination "$pDir\OutwardUE5Overhaul.dll" -Force
            Copy-Item -Path "$outDir\manifest.json" -Destination "$pDir\manifest.json" -Force
            Copy-Item -Path "$outDir\icon.png" -Destination "$pDir\icon.png" -Force
            Copy-Item -Path "$projectDir\README.md" -Destination "$pDir\README.md" -Force
            Copy-Item -Path "$projectDir\CHANGELOG.md" -Destination "$pDir\CHANGELOG.md" -Force
            Write-Host "Deployed to r2modman profile: $pDir"
        } catch {
            Write-Host "Skipped locked DLL in active profile (game is running): $pDir"
        }
    }
}

# Also deploy directly into Outward_Defed\BepInEx so launching via Steam directly also loads BepInEx + the Overhaul!
$gameBepInEx = "$gameDir\BepInEx"
if (-not (Test-Path "$gameBepInEx\core\BepInEx.Preloader.dll") -and (Test-Path "$bepInExProfileDir\core\BepInEx.Preloader.dll")) {
    New-Item -ItemType Directory -Force -Path "$gameBepInEx\core" | Out-Null
    Copy-Item -Path "$bepInExProfileDir\core\*" -Destination "$gameBepInEx\core\" -Recurse -Force
    if (Test-Path "$bepInExProfileDir\patchers") {
        New-Item -ItemType Directory -Force -Path "$gameBepInEx\patchers" | Out-Null
        Copy-Item -Path "$bepInExProfileDir\patchers\*" -Destination "$gameBepInEx\patchers\" -Recurse -Force
    }
    if (Test-Path "$bepInExProfileDir\config") {
        New-Item -ItemType Directory -Force -Path "$gameBepInEx\config" | Out-Null
        Copy-Item -Path "$bepInExProfileDir\config\*" -Destination "$gameBepInEx\config\" -Recurse -Force
    }
    Write-Host "Synced BepInEx core/patchers/config into $gameBepInEx for direct Steam launches"
}
$gamePluginDir = "$gameBepInEx\plugins\Argon-Outward_UE5_Overhaul"
New-Item -ItemType Directory -Force -Path $gamePluginDir | Out-Null
try {
    Copy-Item -Path "$outDir\OutwardUE5Overhaul.dll" -Destination "$gamePluginDir\OutwardUE5Overhaul.dll" -Force
    Copy-Item -Path "$outDir\manifest.json" -Destination "$gamePluginDir\manifest.json" -Force
    Copy-Item -Path "$outDir\icon.png" -Destination "$gamePluginDir\icon.png" -Force
    Copy-Item -Path "$projectDir\README.md" -Destination "$gamePluginDir\README.md" -Force
    Copy-Item -Path "$projectDir\CHANGELOG.md" -Destination "$gamePluginDir\CHANGELOG.md" -Force
    Write-Host "Deployed directly to game folder: $gamePluginDir"
} catch {
    Write-Host "Skipped locked DLL in game folder (game is running): $gamePluginDir"
}

# Stage Thunderstore-compliant package directory
if (Test-Path $tsStageDir) { Remove-Item $tsStageDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path "$tsStageDir\plugins" | Out-Null
[System.IO.File]::WriteAllText("$tsStageDir\manifest.json", $manifestJson, $utf8NoBom)
[System.IO.File]::WriteAllText("$tsStageDir\README.md", [System.IO.File]::ReadAllText("$projectDir\README.md"), $utf8NoBom)
[System.IO.File]::WriteAllText("$tsStageDir\CHANGELOG.md", [System.IO.File]::ReadAllText("$projectDir\CHANGELOG.md"), $utf8NoBom)
Copy-Item -Path "$outDir\icon.png" -Destination "$tsStageDir\icon.png" -Force
Copy-Item -Path "$outDir\OutwardUE5Overhaul.dll" -Destination "$tsStageDir\plugins\OutwardUE5Overhaul.dll" -Force

# Remove old zip files and build the official Thunderstore zip archive (using forward slashes for POSIX/Thunderstore compatibility)
Get-ChildItem -Path $distDir -Filter "*.zip" | Remove-Item -Force
Get-ChildItem -Path $projectDir -Filter "*-Thunderstore.zip" | Remove-Item -Force
$zipPath = "$distDir\Nocll0-Outward_UE5_Overhaul-1.6.2.zip"
$rootZipPath = "$projectDir\Outward_UE5_Overhaul-1.6.2-Thunderstore.zip"
if (Test-Path $rootZipPath) { Remove-Item $rootZipPath -Force }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$fs = New-Object System.IO.FileStream($zipPath, [System.IO.FileMode]::Create)
$zip = New-Object System.IO.Compression.ZipArchive($fs, [System.IO.Compression.ZipArchiveMode]::Create, $false)
Get-ChildItem -Path $tsStageDir -File -Recurse | ForEach-Object {
    $relPath = $_.FullName.Substring($tsStageDir.Length + 1).Replace('\', '/')
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $relPath, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
}
$zip.Dispose()
$fs.Dispose()
Copy-Item -Path $zipPath -Destination $rootZipPath -Force

Write-Host "Created Thunderstore mod archive: $zipPath"
Write-Host "Created Thunderstore mod archive copy: $rootZipPath"
