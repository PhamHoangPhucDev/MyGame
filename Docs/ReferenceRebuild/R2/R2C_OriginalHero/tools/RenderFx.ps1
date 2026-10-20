# Aren slash VFX, Hurt, Death. Output in ..\fx\
$D = Split-Path $PSScriptRoot -Parent
$O = Join-Path $D "fx"; New-Item -ItemType Directory -Force $O | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\FxGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
[FxGen]::Run($O)

$f = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold)
$fs = New-Object System.Drawing.Font "Consolas", 9
$grassC = [System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87)
$grass = New-Object System.Drawing.SolidBrush $grassC
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(90, 0, 0, 0))
$bgc = [System.Drawing.Color]::FromArgb(255, 28, 28, 32)
function NewBoard($w, $h) { $b = New-Object System.Drawing.Bitmap $w, $h; $g = [System.Drawing.Graphics]::FromImage($b); $g.Clear($bgc); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'; return @($b, $g) }
function Tile($g, $bmp, $x, $y, $k, $label, $wideShadow) {
    $sz = 48 * $k
    $g.FillRectangle($grass, $x, $y, $sz, $sz)
    if ($wideShadow) { $g.FillEllipse($shadow, $x + 12 * $k, $y + 36 * $k, 24 * $k, 3 * $k) } else { $g.FillEllipse($shadow, $x + 18 * $k, $y + 36 * $k, 12 * $k, 3 * $k) }
    $g.DrawImage($bmp, (New-Object System.Drawing.Rectangle $x, $y, $sz, $sz), (New-Object System.Drawing.Rectangle 0, 0, 48, 48), [System.Drawing.GraphicsUnit]::Pixel)
    if ($label) { $g.DrawString($label, $fs, [System.Drawing.Brushes]::White, $x + 3, $y + 3) }
}

# A) attack with slash VFX
$K = 5; $bb = NewBoard 780 840; $b = $bb[0]; $g = $bb[1]
$fl = @("F0 windup 0.08s", "F1 strike + VFX0 0.10s", "F2 recover + VFX1 0.08s")
$views = @(@("Down", "Down"), @("Up", "Up (vet chem sau than)"), @("Side", "Side 3/4 right"))
for ($r = 0; $r -lt 3; $r++) {
    $y = 30 + $r * 270
    $g.DrawString("Aren Attack $($views[$r][1]) - vet chem di qua mui kiem o F1", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
    for ($i = 0; $i -lt 3; $i++) { Tile $g ([FxGen]::ArenAttackWithFx($views[$r][0], $i)) (10 + $i * 255) $y $K $fl[$i] $false }
}
$b.Save((Join-Path $O "Aren_Attack_VFX_v1_review.png"))

# B) hurt (3 views x 2) + death (3)
$K = 4; $bb = NewBoard 1220 520; $b = $bb[0]; $g = $bb[1]
$g.DrawString("Hurt: H0 flash trang 0.06s | H1 nhuom do + giat 1px 0.12s", $f, [System.Drawing.Brushes]::Yellow, 10, 6)
$x = 10
foreach ($v in @("Down", "Up", "Side")) { for ($i = 0; $i -lt 2; $i++) { Tile $g ([FxGen]::ArenHurt($v, $i)) $x 30 $K "$v H$i" $false; $x += 200 } }
$g.DrawString("Death: D0 quy goi 0.15s | D1 nga nam 0.15s | D2 nam + roi kiem, khien (giu)", $f, [System.Drawing.Brushes]::Yellow, 10, 256)
$dl = @("D0 quy", "D1 nga", "D2 nam + roi do")
for ($i = 0; $i -lt 3; $i++) { Tile $g ([FxGen]::ArenDeath($i)) (10 + $i * 200) 280 $K $dl[$i] ($i -gt 0) }
$b.Save((Join-Path $O "Aren_Hurt_Death_v1_review.png"))
"done"
