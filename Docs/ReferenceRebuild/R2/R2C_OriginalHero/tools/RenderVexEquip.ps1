# Vex gear preview + Unity export (..\unity_export_vex\)
$D = Split-Path $PSScriptRoot -Parent
$O = Join-Path $D "vex"; $X = Join-Path $D "unity_export_vex"
New-Item -ItemType Directory -Force $O, $X | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\FxGen.cs", "$D\tools\VfxConceptGen.cs", "$D\tools\VexGen.cs", "$D\tools\VexEquip.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
[VexEquip]::Run($X)

$f = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold)
$fs = New-Object System.Drawing.Font "Consolas", 9
$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$K = 4; $sz = 48 * $K
function Tile($g, $bmp, $x, $y, $label) { $g.FillRectangle($grass, $x, $y, $sz, $sz); $g.FillEllipse($shadow, $x + 18 * $K, $y + 36 * $K, 12 * $K, 3 * $K); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'; $g.DrawImage($bmp, (New-Object System.Drawing.Rectangle $x, $y, $sz, $sz)); $g.DrawString($label, $fs, [System.Drawing.Brushes]::White, $x + 3, $y + 3) }

# Board A: attack combo per view (windup, 4 strikes, recover) + idle/walk sample
$b = New-Object System.Drawing.Bitmap (7 * ($sz + 8) + 10), (3 * ($sz + 30) + 20); $g = [System.Drawing.Graphics]::FromImage($b); $g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32))
$views = @("Down", "Up", "Side")
for ($r = 0; $r -lt 3; $r++) {
    $v = $views[$r]; $y = 26 + $r * ($sz + 30)
    $g.DrawString("Vex $v (Side = facing right) - Idle | windup | combo 1 stab L | 2 stab R | 3 cross | 4 X finisher | recover", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 22)
    Tile $g ([VexEquip]::Preview("Idle", $v, 0, 0)) 10 $y "Idle"
    Tile $g ([VexEquip]::Preview("Attack", $v, 0, 0)) (10 + 1 * ($sz + 8)) $y "F0 windup"
    for ($s = 0; $s -lt 4; $s++) { Tile $g ([VexEquip]::Preview("Attack", $v, 1, $s)) (10 + (2 + $s) * ($sz + 8)) $y "F1 hit $($s + 1)" }
    Tile $g ([VexEquip]::Preview("Attack", $v, 2, 3)) (10 + 6 * ($sz + 8)) $y "F2 recover"
}
$b.Save((Join-Path $O "Vex_combo_v1_review.png"))

# Board B: skill VFX sheets
$b = New-Object System.Drawing.Bitmap 1200, 330; $g = [System.Drawing.Graphics]::FromImage($b); $g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$items = @(@("Vex_Smoke.png", "Bom Khoi / Buoc Bong: khoi 4 frame", 128, 32, 3), @("Vex_Knife.png", "Phi Dao: dao bay 8 huong + vet do", 256, 32, 3), @("Vex_ExecuteX.png", "Doat Menh: chu X lon 2 frame", 96, 48, 3))
$y = 30
foreach ($it in $items) { $s = [System.Drawing.Bitmap]::FromFile((Join-Path $X "Gear\$($it[0])")); $g.DrawString($it[1], $f, [System.Drawing.Brushes]::Yellow, 10, $y - 22); $g.FillRectangle($grass, 10, $y, $it[2] * $it[4], $it[3] * $it[4]); $g.DrawImage($s, (New-Object System.Drawing.Rectangle 10, $y, ($it[2] * $it[4]), ($it[3] * $it[4]))); $y += $it[3] * $it[4] + 32 }
$b.Save((Join-Path $O "Vex_skillvfx_v1_review.png")); "done"
