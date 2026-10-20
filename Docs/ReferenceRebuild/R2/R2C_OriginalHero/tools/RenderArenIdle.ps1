# Builds Aren_Idle{Down,Up,Side}_v1.png (128x32 each) and review boards in ..\idle\
$D = Split-Path $PSScriptRoot -Parent
$O = Join-Path $D "idle"; New-Item -ItemType Directory -Force $O | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
[ArenIdleGen]::Run($O)

$f = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold)
$grassC = [System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87)
$grass = New-Object System.Drawing.SolidBrush $grassC
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(90, 0, 0, 0))
$red = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 255, 60, 60)), 2
$K = 8
$times = @("0.25 s", "0.15 s", "0.25 s", "0.15 s")
function Sheet($v) { [System.Drawing.Bitmap]::FromFile((Join-Path $O "Aren_Idle${v}_v1.png")) }

# Board 1: three views x8 with diff markers
$views = @(@("Down", "Down (approved v1)"), @("Up", "Up - back view (NEW)"), @("Side", "Side 3/4 facing right (NEW; left = flipX)"))
$b = New-Object System.Drawing.Bitmap 1084, (3 * 300 + 20); $g = [System.Drawing.Graphics]::FromImage($b)
$g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
for ($r = 0; $r -lt 3; $r++) {
    $s = Sheet $views[$r][0]; $y = 30 + $r * 300
    $g.DrawString("Aren Idle $($views[$r][1]) - x8, red = changed vs F0, loop 0.80 s", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
    for ($i = 0; $i -lt 4; $i++) {
        $x = 10 + $i * 266
        $g.FillRectangle($grass, $x, $y, 256, 256); $g.FillEllipse($shadow, $x + 80, $y + 224, 96, 24)
        $g.DrawImage($s, (New-Object System.Drawing.Rectangle $x, $y, 256, 256), (New-Object System.Drawing.Rectangle ($i * 32), 0, 32, 32), [System.Drawing.GraphicsUnit]::Pixel)
        $g.DrawString("F$i  $($times[$i])", $f, [System.Drawing.Brushes]::White, $x + 4, $y + 4)
        if ($i -gt 0) { for ($py = 0; $py -lt 32; $py++) { for ($px = 0; $px -lt 32; $px++) { if ($s.GetPixel($i * 32 + $px, $py).ToArgb() -ne $s.GetPixel($px, $py).ToArgb()) { $g.DrawRectangle($red, $x + $px * $K, $y + $py * $K, $K, $K) } } } }
    }
}
$b.Save((Join-Path $O "Aren_Idle_3views_v1_review.png"))

# Board 2: turnaround at x4 (Down, Side-right, Up, Side-left flipped) + in-game scale
$b = New-Object System.Drawing.Bitmap 1084, 560; $g = [System.Drawing.Graphics]::FromImage($b)
$g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$g.DrawString("Turnaround F0 x4: Down | Side right | Up | Side left (flipX)", $f, [System.Drawing.Brushes]::Yellow, 10, 6)
$g.FillRectangle($grass, 10, 30, 1064, 150)
$turn = @(@("Down", $false), @("Side", $false), @("Up", $false), @("Side", $true))
for ($i = 0; $i -lt 4; $i++) {
    $s = Sheet $turn[$i][0]; $x = 60 + $i * 250
    $g.FillEllipse($shadow, $x + 40, 30 + 112, 48, 12)
    if ($turn[$i][1]) { $g.DrawImage($s, (New-Object System.Drawing.Rectangle ($x + 128), 30, -128, 128), (New-Object System.Drawing.Rectangle 0, 0, 32, 32), [System.Drawing.GraphicsUnit]::Pixel) }
    else { $g.DrawImage($s, (New-Object System.Drawing.Rectangle $x, 30, 128, 128), (New-Object System.Drawing.Rectangle 0, 0, 32, 32), [System.Drawing.GraphicsUnit]::Pixel) }
}
$y = 220; $g.DrawString("In-game scale: Portrait 144x256 | Landscape 480x270 (Down, Side, Up, Side-left)", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
foreach ($v in @(@(10, 144, 256), @(250, 480, 270))) {
    $vb = New-Object System.Drawing.Bitmap $v[1], $v[2]; $vg = [System.Drawing.Graphics]::FromImage($vb); $vg.Clear($grassC); $vg.InterpolationMode = 'NearestNeighbor'
    $cy = [int]($v[2] / 2) - 16
    for ($i = 0; $i -lt 4; $i++) {
        $s = Sheet $turn[$i][0]; $cx = [int]($v[1] / 2) - 64 + $i * 32
        $vg.FillEllipse($shadow, $cx + 10, $cy + 28, 12, 3)
        if ($turn[$i][1]) { $vg.DrawImage($s, (New-Object System.Drawing.Rectangle ($cx + 32), $cy, -32, 32), (New-Object System.Drawing.Rectangle 0, 0, 32, 32), [System.Drawing.GraphicsUnit]::Pixel) }
        else { $vg.DrawImage($s, (New-Object System.Drawing.Rectangle $cx, $cy, 32, 32), (New-Object System.Drawing.Rectangle 0, 0, 32, 32), [System.Drawing.GraphicsUnit]::Pixel) }
    }
    $g.DrawImage($vb, (New-Object System.Drawing.Rectangle $v[0], $y, ([int]($v[1] * 1.2)), ([int]($v[2] * 1.2))), (New-Object System.Drawing.Rectangle 0, 0, $v[1], $v[2]), [System.Drawing.GraphicsUnit]::Pixel)
}
$b.Save((Join-Path $O "Aren_Idle_turnaround_v1.png"))
