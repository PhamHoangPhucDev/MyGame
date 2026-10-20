# Aren designed kit - candidate VFX board -> ..\aren_kit\AREN_KIT_VFX_CANDIDATE.png
$D = Split-Path $PSScriptRoot -Parent
$O = Join-Path $D "aren_kit"; New-Item -ItemType Directory -Force $O | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\FxGen.cs", "$D\tools\VfxConceptGen.cs", "$D\tools\ArenKitVfxGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$K = 4; $tw = 96 * $K; $th = 64 * $K
$f = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold)
$b = New-Object System.Drawing.Bitmap (3 * ($tw + 10) + 10), (2 * ($th + 34) + 40 + 120)
$g = [System.Drawing.Graphics]::FromImage($b); $g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$g.DrawString("Aren - bo skill thiet ke: CANDIDATE key frame (x4). Palette = vet chem Aren da duyet (trang / xanh bang / vang)", $f, [System.Drawing.Brushes]::Yellow, 10, 6)
$tiles = @()
for ($i = 0; $i -lt 6; $i++) {
    $x = 10 + ($i % 3) * ($tw + 10); $y = 50 + [math]::Floor($i / 3) * ($th + 34)
    $t = [ArenKitVfxGen]::Tile($i); $tiles += $t
    $g.DrawImage($t, (New-Object System.Drawing.Rectangle $x, $y, $tw, $th))
    $g.DrawString([ArenKitVfxGen]::Labels[$i], $f, [System.Drawing.Brushes]::White, $x, $y - 20)
}
# 1x row (real size)
$y = 50 + 2 * ($th + 34) + 10
$g.DrawString("Kich thuoc that (x1) va x2:", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 4)
for ($i = 0; $i -lt 6; $i++) { $g.DrawImage($tiles[$i], (New-Object System.Drawing.Rectangle (10 + $i * 100), ($y + 18), 96, 64)) }
for ($i = 0; $i -lt 3; $i++) { $g.DrawImage($tiles[$i], (New-Object System.Drawing.Rectangle (620 + $i * 196), ($y - 10), 192, 128)) }
$b.Save((Join-Path $O "AREN_KIT_VFX_CANDIDATE.png")); "ok"
