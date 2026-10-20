# Builds Aren_Walk{Down,Up,Side}_v1.png (128x32 each) and review boards in ..\walk\
$D = Split-Path $PSScriptRoot -Parent
$O = Join-Path $D "walk"; New-Item -ItemType Directory -Force $O | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
[ArenWalkGen]::Run($O)

$f = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold)
$grassC = [System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87)
$grass = New-Object System.Drawing.SolidBrush $grassC
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(90, 0, 0, 0))
$guide = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(160, 255, 255, 0)), 1
$K = 8
$labels = @("F0 contact L", "F1 pass", "F2 contact R", "F3 pass")
function Sheet($v) { [System.Drawing.Bitmap]::FromFile((Join-Path $O "Aren_Walk${v}_v1.png")) }

# Board 1: three views x8; yellow line = sole row 29
$views = @(@("Down", "Down"), @("Up", "Up (back)"), @("Side", "Side 3/4 right (left = flipX)"))
$b = New-Object System.Drawing.Bitmap 1084, 920; $g = [System.Drawing.Graphics]::FromImage($b)
$g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
for ($r = 0; $r -lt 3; $r++) {
    $s = Sheet $views[$r][0]; $y = 30 + $r * 300
    $g.DrawString("Aren Walk $($views[$r][1]) - x8, 4 x 0.125 s (loop 0.5 s); yellow = sole row 29", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
    for ($i = 0; $i -lt 4; $i++) {
        $x = 10 + $i * 266
        $g.FillRectangle($grass, $x, $y, 256, 256); $g.FillEllipse($shadow, $x + 80, $y + 224, 96, 24)
        $g.DrawImage($s, (New-Object System.Drawing.Rectangle $x, $y, 256, 256), (New-Object System.Drawing.Rectangle ($i * 32), 0, 32, 32), [System.Drawing.GraphicsUnit]::Pixel)
        $g.DrawLine($guide, $x, $y + 30 * $K, $x + 255, $y + 30 * $K)
        $g.DrawString($labels[$i], $f, [System.Drawing.Brushes]::White, $x + 4, $y + 4)
    }
}
$b.Save((Join-Path $O "Aren_Walk_3views_v1_review.png"))

# Board 2: walking path at x4 (8 steps each view) + in-game scale
$b = New-Object System.Drawing.Bitmap 1084, 600; $g = [System.Drawing.Graphics]::FromImage($b)
$g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$g.DrawString("Walking path x4 (F0..F3 twice, advancing): Side right | Side left (flipX)", $f, [System.Drawing.Brushes]::Yellow, 10, 6)
$g.FillRectangle($grass, 10, 30, 1064, 320)
$s = Sheet "Side"
for ($n = 0; $n -lt 8; $n++) {
    $i = $n % 4; $x = 20 + $n * 120
    $g.FillEllipse($shadow, $x + 40, 30 + 112, 48, 12)
    $g.DrawImage($s, (New-Object System.Drawing.Rectangle $x, 30, 128, 128), (New-Object System.Drawing.Rectangle ($i * 32), 0, 32, 32), [System.Drawing.GraphicsUnit]::Pixel)
    $x2 = 1000 - $n * 120
    $g.FillEllipse($shadow, $x2 + 40, 190 + 112, 48, 12)
    $g.DrawImage($s, (New-Object System.Drawing.Rectangle ($x2 + 128), 190, -128, 128), (New-Object System.Drawing.Rectangle ($i * 32), 0, 32, 32), [System.Drawing.GraphicsUnit]::Pixel)
}
$y = 390; $g.DrawString("In-game scale x1: Down / Up / Side frames (Portrait 144x256 crop)", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
$vb = New-Object System.Drawing.Bitmap 144, 100; $vg = [System.Drawing.Graphics]::FromImage($vb); $vg.Clear($grassC); $vg.InterpolationMode = 'NearestNeighbor'
$vi = 0
foreach ($v in @("Down", "Up", "Side")) { $s = Sheet $v; for ($i = 0; $i -lt 4; $i++) { $vg.DrawImage($s, (New-Object System.Drawing.Rectangle (4 + $i * 34), (2 + $vi * 32), 32, 32), (New-Object System.Drawing.Rectangle ($i * 32), 0, 32, 32), [System.Drawing.GraphicsUnit]::Pixel) }; $vi++ }
$g.DrawImage($vb, (New-Object System.Drawing.Rectangle 10, $y, 288, 200), (New-Object System.Drawing.Rectangle 0, 0, 144, 100), [System.Drawing.GraphicsUnit]::Pixel)
$b.Save((Join-Path $O "Aren_Walk_path_v1.png"))
