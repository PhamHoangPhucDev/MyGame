# Per-class VFX concept board -> ..\classkit\CLASS_VFX_CONCEPTS.png
$D = Split-Path $PSScriptRoot -Parent
$O = Join-Path $D "classkit"; New-Item -ItemType Directory -Force $O | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\VfxConceptGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$K = 3; $tw = 96 * $K; $th = 64 * $K
$classes = @(@("Aren", "Aren - Hiep si (trang, xanh bang, vang)"), @("Brom", "Brom - Nguoi lun Tank (cam, nau dat, tia lua)"), @("Lyra", "Lyra - Elf cung thu (xanh la, la cay, vang)"), @("Selene", "Selene - Phu thuy (tim hong, tim, sao)"), @("Vex", "Vex - Sat thu (do tham, khoi den, bac)"))
$b = New-Object System.Drawing.Bitmap (5 * ($tw + 10) + 10), (5 * ($th + 52) + 10)
$g = [System.Drawing.Graphics]::FromImage($b); $g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$f = New-Object System.Drawing.Font "Consolas", 12, ([System.Drawing.FontStyle]::Bold)
$fs = New-Object System.Drawing.Font "Consolas", 10
for ($r = 0; $r -lt 5; $r++) {
    $cls = $classes[$r][0]; $y = 10 + $r * ($th + 52)
    $g.DrawString($classes[$r][1], $f, [System.Drawing.Brushes]::Yellow, 10, $y)
    $labels = [VfxConceptGen]::Labels[$cls]
    for ($i = 0; $i -lt 5; $i++) {
        $x = 10 + $i * ($tw + 10)
        $t = [VfxConceptGen]::Tile($cls, $i)
        $g.DrawImage($t, (New-Object System.Drawing.Rectangle $x, ($y + 22), $tw, $th))
        $g.DrawString($labels[$i], $fs, [System.Drawing.Brushes]::White, ($x + 4), ($y + 26))
    }
}
$b.Save((Join-Path $O "CLASS_VFX_CONCEPTS.png"))
"done"
