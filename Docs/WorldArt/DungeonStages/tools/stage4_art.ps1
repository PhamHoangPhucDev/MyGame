param([string]$Out)
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $Out | Out-Null

function C([string]$h, [int]$a = 255) { [System.Drawing.Color]::FromArgb($a, [Convert]::ToInt32($h.Substring(1,2),16), [Convert]::ToInt32($h.Substring(3,2),16), [Convert]::ToInt32($h.Substring(5,2),16)) }
function NewBmp([int]$w, [int]$h) { New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb) }
function Hash([int]$x, [int]$y, [int]$s) { $n = ($x * 374761393 + $y * 668265263 + $s * 1442695041) -band 0x7fffffff; $n = (($n -bxor ($n -shr 13)) * 1274126177) -band 0x7fffffff; return ($n % 1000) / 1000.0 }
function Paint($bmp, [string[]]$rows, [hashtable]$pal, [int]$ox, [int]$oy) {
    for ($y = 0; $y -lt $rows.Length; $y++) { for ($x = 0; $x -lt $rows[$y].Length; $x++) {
        $ch = [string]$rows[$y][$x]
        if ($pal.ContainsKey($ch)) { $bmp.SetPixel($ox + $x, $oy + $y, $pal[$ch]) } } }
}

$ink = C '#141018'; $wl = C '#b07840'; $wm = C '#8a5a30'; $wd = C '#5a3a20'; $nail = C '#3a3a3a'
$rl = C '#d4b483'; $rd = C '#8a6a3a'

# 1) Bridge tiles: 64x32 sheet = Start | Mid | End | Broken, each 16x32. Bridge runs left-right over a vertical ravine.
$bridge = NewBmp 64 32
function Rope($bmp, [int]$ox, [int]$y, [int]$x0, [int]$x1) {
    for ($x = $x0; $x -le $x1; $x++) {
        $bmp.SetPixel($ox + $x, $y, $(if (($x % 2) -eq 0) { $rl } else { $rd }))
        $bmp.SetPixel($ox + $x, $y + 1, $(if (($x % 2) -eq 0) { $rd } else { $rl })) }
}
function Planks($bmp, [int]$ox, [int]$x0, [int]$x1, [int]$seed, [int[]]$skip) {
    for ($x = $x0; $x -le $x1; $x++) {
        $i = [int](($x - $x0) / 4); $c = ($x - $x0) % 4
        if ($skip -contains $i) { continue }
        if ($c -eq 3) { continue }
        $top = 5 + [int]((Hash $i $seed 3) * 2); $bot = 26 - [int]((Hash $i $seed 5) * 2)
        for ($y = $top; $y -le $bot; $y++) {
            $col = if ($c -eq 0) { $wl } else { $wm }
            if ($y -eq $top -or $y -eq $bot) { $col = $wd }
            elseif ((Hash $x $y $seed) -lt 0.08) { $col = $wd }
            if ($c -eq 1 -and ($y -eq $top + 2 -or $y -eq $bot - 2)) { $col = $nail }
            $bmp.SetPixel($ox + $x, $y, $col) } }
}
function Post($bmp, [int]$ox, [int]$px) {
    for ($y = 1; $y -le 30; $y++) { for ($x = $px; $x -le $px + 2; $x++) {
        $col = if ($x -eq $px) { $ink } elseif ($x -eq $px + 2) { $ink } else { $wd }
        if ($y -eq 1 -or $y -eq 30) { $col = $ink }
        if ($x -eq $px + 1 -and ($y -eq 2 -or $y -eq 16)) { $col = $wl }
        $bmp.SetPixel($ox + $x, $y, $col) } }
}
# Start: post at x=1..3, planks from x=4
Planks $bridge 0 4 15 11 @(); Rope $bridge 0 3 3 15; Rope $bridge 0 27 3 15; Post $bridge 0 1
# Mid: seamless planks 0..15
Planks $bridge 16 0 15 22 @(); Rope $bridge 16 3 0 15; Rope $bridge 16 27 0 15
# End: mirror of start
Planks $bridge 32 0 11 33 @(); Rope $bridge 32 3 0 12; Rope $bridge 32 27 0 12; Post $bridge 32 12
# Broken: plank 1 missing, plank 2 cracked short, bottom rope snapped and dangling
Planks $bridge 48 0 15 44 @(1); Rope $bridge 48 3 0 15
for ($x = 0; $x -le 4; $x++) { $bridge.SetPixel(48 + $x, 27, $rl); $bridge.SetPixel(48 + $x, 28, $rd) }
for ($y = 27; $y -le 31; $y++) { $bridge.SetPixel(48 + 5, $y, $rd) }
for ($x = 11; $x -le 15; $x++) { $bridge.SetPixel(48 + $x, 27, $rl); $bridge.SetPixel(48 + $x, 28, $rd) }
for ($y = 18; $y -le 26; $y++) { for ($x = 8; $x -le 10; $x++) { $bridge.SetPixel(48 + $x, $y, [System.Drawing.Color]::Transparent) } }
$bridge.SetPixel(48 + 8, 17, $wd); $bridge.SetPixel(48 + 9, 18, $wd); $bridge.SetPixel(48 + 10, 17, $wd)
$bridge.Save("$Out\Stage4_Bridge_Tiles_4x16x32.png")

# 2) Bridge progress bar: frame 80x14 (icon + 3 holes) and pips sheet 54x8 (filled | active | empty, 18x8 each)
$bar = NewBmp 80 14
$panel = C '#1c1f26'; $edge = C '#4a5260'; $hole = C '#0e1014'
for ($y = 0; $y -lt 14; $y++) { for ($x = 0; $x -lt 80; $x++) {
    $col = $panel
    if ($x -eq 0 -or $x -eq 79 -or $y -eq 0 -or $y -eq 13) { $col = $ink }
    elseif ($x -eq 1 -or $x -eq 78 -or $y -eq 1 -or $y -eq 12) { $col = $edge }
    if (($x -eq 0 -or $x -eq 79) -and ($y -eq 0 -or $y -eq 13)) { $col = [System.Drawing.Color]::Transparent }
    $bar.SetPixel($x, $y, $col) } }
foreach ($slot in 0, 1, 2) { $hx = 18 + $slot * 20
    for ($y = 3; $y -le 10; $y++) { for ($x = $hx - 1; $x -le $hx + 18; $x++) { $bar.SetPixel($x, $y, $hole) } } }
$iconRows = @(
'..........',
'.R......R.',
'.RRRRRRRR.',
'.LWLWLWLW.',
'.LWLWLWLW.',
'.RRRRRRRR.',
'.R......R.',
'..........')
Paint $bar $iconRows @{ 'R' = $rl; 'L' = $wl; 'W' = $wd } 4 3
$bar.Save("$Out\Stage4_BridgeBar_Frame.png")

$pips = NewBmp 54 8
$pipPal = @(
    @{ 'o' = (C '#ba7517'); 'f' = (C '#f2d84a'); 'h' = (C '#fff6c0') },
    @{ 'o' = (C '#e0952a'); 'f' = (C '#fff6c0'); 'h' = (C '#ffffff') },
    @{ 'o' = (C '#2c2f38'); 'f' = (C '#3a3f4a'); 'h' = (C '#4a5260') })
$pipRows = @(
'oooooooooooooooooo',
'ohhhhhhhhhhhhhhhho',
'offfffffffffffffo.',
'offfffffffffffffo.',
'offfffffffffffffo.',
'offfffffffffffffo.',
'offfffffffffffffo.',
'oooooooooooooooooo')
$pipRows = $pipRows | ForEach-Object { $_.Replace('o.', 'oo') }
for ($k = 0; $k -lt 3; $k++) { Paint $pips $pipRows $pipPal[$k] ($k * 18) 0 }
$pips.Save("$Out\Stage4_BridgeBar_Pips_3x18x8.png")

# 3) Edge danger vignette: 48x48, 9-slice border 16, center transparent, stepped + dithered bands
$vig = NewBmp 48 48
$red = '#e04848'
for ($y = 0; $y -lt 48; $y++) { for ($x = 0; $x -lt 48; $x++) {
    $d = [Math]::Min([Math]::Min($x, $y), [Math]::Min(47 - $x, 47 - $y))
    $a = 0
    if ($d -le 1) { $a = 170 } elseif ($d -le 4) { $a = 110 } elseif ($d -le 8) { $a = 60 } elseif ($d -le 12) { if ((($x + $y) % 2) -eq 0) { $a = 40 } }
    if ($d -in 2, 5, 9) { if ((($x + $y) % 2) -eq 1) { $a = [int]($a * 1.4) } }
    if ($a -gt 0) { $vig.SetPixel($x, $y, (C $red ([Math]::Min(255, $a)))) } } }
$vig.Save("$Out\Stage4_EdgeDanger_9s.png")

# Preview x5: ravine with the bridge, the bar on top, the vignette on the right
$S = 5
$pv = New-Object System.Drawing.Bitmap -ArgumentList (200 * $S), (80 * $S)
$g = [System.Drawing.Graphics]::FromImage($pv)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$g.Clear((C '#5a7a3a'))
$g.FillRectangle((New-Object System.Drawing.SolidBrush (C '#2c2c2a')), 20 * $S, 0, 72 * $S, 80 * $S)
$g.FillRectangle((New-Object System.Drawing.SolidBrush (C '#141418')), 30 * $S, 0, 52 * $S, 80 * $S)
$order = @(0, 1, 1, 3, 1, 2)
for ($i = 0; $i -lt $order.Length; $i++) {
    $g.DrawImage($bridge, (New-Object System.Drawing.Rectangle ((8 + $i * 16) * $S), (34 * $S), (16 * $S), (32 * $S)), (New-Object System.Drawing.Rectangle ($order[$i] * 16), 0, 16, 32), [System.Drawing.GraphicsUnit]::Pixel) }
$g.DrawImage($bar, (New-Object System.Drawing.Rectangle (16 * $S), (8 * $S), (80 * $S), (14 * $S)), (New-Object System.Drawing.Rectangle 0, 0, 80, 14), [System.Drawing.GraphicsUnit]::Pixel)
$state = @(0, 1, 2)
for ($slot = 0; $slot -lt 3; $slot++) {
    $g.DrawImage($pips, (New-Object System.Drawing.Rectangle ((16 + 18 + $slot * 20) * $S), ((8 + 3) * $S), (18 * $S), (8 * $S)), (New-Object System.Drawing.Rectangle ($state[$slot] * 18), 0, 18, 8), [System.Drawing.GraphicsUnit]::Pixel) }
$g.FillRectangle((New-Object System.Drawing.SolidBrush (C '#3a5a2a')), 130 * $S, 16 * $S, 48 * $S, 48 * $S)
$g.DrawImage($vig, (New-Object System.Drawing.Rectangle (130 * $S), (16 * $S), (48 * $S), (48 * $S)), (New-Object System.Drawing.Rectangle 0, 0, 48, 48), [System.Drawing.GraphicsUnit]::Pixel)
$pv.Save("$Out\Stage4_Preview_x5.png")
$g.Dispose()
"ok"
