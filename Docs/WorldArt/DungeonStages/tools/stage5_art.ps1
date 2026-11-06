param([string]$Out)
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $Out | Out-Null

function C([string]$h, [int]$a = 255) { [System.Drawing.Color]::FromArgb($a, [Convert]::ToInt32($h.Substring(1,2),16), [Convert]::ToInt32($h.Substring(3,2),16), [Convert]::ToInt32($h.Substring(5,2),16)) }
function NewBmp([int]$w, [int]$h) { New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb) }
function Hash([int]$x, [int]$y, [int]$k) { $n = ($x * 374761393 + $y * 668265263 + $k * 1442695041) -band 0x7fffffff; $n = (($n -bxor ($n -shr 13)) * 1274126177) -band 0x7fffffff; return ($n % 1000) / 1000.0 }
function Paint($bmp, [string[]]$rows, [hashtable]$pal, [int]$ox, [int]$oy) {
    for ($y = 0; $y -lt $rows.Length; $y++) { for ($x = 0; $x -lt $rows[$y].Length; $x++) {
        $ch = [string]$rows[$y][$x]
        if ($pal.ContainsKey($ch)) { $bmp.SetPixel($ox + $x, $oy + $y, $pal[$ch]) } } }
}
function Ell([double]$x, [double]$y, [double]$cx, [double]$cy, [double]$a, [double]$b) { $dx = ($x - $cx) / $a; $dy = ($y - $cy) / $b; return [Math]::Sqrt($dx * $dx + $dy * $dy) }

$ink = C '#141018'; $bone = C '#e8e0c8'; $boneS = C '#b4a88a'; $boneD = C '#7a6e58'

# 1) Ritual bone ring decal 64x32 (2.5D ellipse): bone chips along the ring, 4 small skulls.
$ring = NewBmp 64 32
$cx = 31.5; $cy = 15.5; $A = 29.0; $B = 13.5
for ($y = 0; $y -lt 32; $y++) { for ($x = 0; $x -lt 64; $x++) {
    $r = Ell $x $y $cx $cy $A $B
    if ($r -gt 0.78 -and $r -lt 1.0) {
        $h = Hash $x $y 9
        $col = if ($h -lt 0.55) { $bone } elseif ($h -lt 0.85) { $boneS } else { $boneD }
        if ($r -gt 0.95 -or $r -lt 0.81) { $col = $boneD }
        if ((Hash ([int]($x / 3)) ([int]($y / 2)) 4) -lt 0.06) { continue }
        $ring.SetPixel($x, $y, $col) }
    elseif ($r -ge 1.0 -and $r -lt 1.06 -and (Hash $x $y 2) -lt 0.6) { $ring.SetPixel($x, $y, (C '#2a1f24' 120)) } } }
$skull = @(
'.KKK.',
'KBBBK',
'KDBDK',
'KBBBK',
'.KDK.')
foreach ($p in @(@(29, 1), @(29, 26), @(1, 13), @(58, 13))) { Paint $ring $skull @{ 'K' = $ink; 'B' = $bone; 'D' = $boneD } $p[0] $p[1] }
$ring.Save("$Out\Stage5_RitualRing.png")

# 2) Telegraph overlay, 4 frames (256x32): red fills inward, last frame is the strike flash.
$tele = NewBmp 256 32
$fill = @(0.0, 0.35, 0.7, 1.0)
for ($f = 0; $f -lt 4; $f++) {
    for ($y = 0; $y -lt 32; $y++) { for ($x = 0; $x -lt 64; $x++) {
        $r = Ell $x $y $cx $cy ($A - 2.0) ($B - 1.5)
        if ($r -gt 1.0) { continue }
        $px = $null
        if ($r -gt 0.9) { $px = if ($f -eq 3) { C '#ffffff' 235 } else { C '#e04848' 230 } }
        elseif ($f -eq 3 -or $r -gt (1.0 - 0.9 * $fill[$f])) {
            if ($f -eq 3) { $px = C '#ff7a6a' 170 }
            elseif ((($x + $y + $f) % 2) -eq 0) { $px = C '#e04848' 120 } }
        if ($px -ne $null) { $tele.SetPixel($f * 64 + $x, $y, $px) } } }
}
$tele.Save("$Out\Stage5_RitualTelegraph_Sheet_4f.png")

# 3) Boss bar phase notch, 2 frames (8x12): normal | just broken flash
$notch = NewBmp 8 12
$nRows = @(
'KLLK',
'KDDK',
'.KK.',
'.KK.',
'.KK.',
'.KK.',
'.KK.',
'.KK.',
'.KK.',
'.KK.',
'KDDK',
'KLLK')
Paint $notch $nRows @{ 'K' = $ink; 'L' = (C '#f2d84a'); 'D' = (C '#ba7517') } 0 0
Paint $notch $nRows @{ 'K' = (C '#ffffff'); 'L' = (C '#ffffff'); 'D' = (C '#ff7a6a') } 4 0
$notch.Save("$Out\Stage5_BossPhaseNotch_2f.png")

# 4) Chapter banner ribbon 96x32, 9-slice L/R 24 (text is Unity text, not baked)
$ban = NewBmp 96 32
$pD = C '#26215c'; $pM = C '#534ab7'; $pL = C '#7f77dd'; $gold = C '#f2d84a'; $goldD = C '#ba7517'
for ($y = 0; $y -lt 32; $y++) { for ($x = 0; $x -lt 96; $x++) {
    $col = $null
    $tailL = $x -lt 10; $tailR = $x -gt 85
    if ($tailL -or $tailR) {
        $tx = if ($tailL) { $x } else { 95 - $x }
        if ($y -ge 10 -and $y -le 29) {
            $notchV = [Math]::Abs($y - 19.5) -lt (4.5 - $tx * 0.45)
            if (-not $notchV) { $col = if ($y -eq 10 -or $y -eq 29 -or $tx -eq 9) { $ink } else { $pD } } } }
    elseif ($y -ge 4 -and $y -le 25) {
        $col = $pM
        if ($y -eq 4 -or $y -eq 25) { $col = $ink }
        elseif ($y -eq 5 -or $y -eq 24) { $col = $goldD }
        elseif ($y -eq 6 -or $y -eq 23) { $col = $gold }
        elseif ($y -le 9) { $col = $pL }
        if ($x -eq 10 -or $x -eq 85) { $col = $ink } }
    if ($col -ne $null) { $ban.SetPixel($x, $y, $col) } } }
$ban.Save("$Out\Stage5_ChapterBanner_9s.png")

# Preview x5: ring + telegraph frames on stronghold dirt, notches on a mock boss bar, banner
$S = 5
$pv = New-Object System.Drawing.Bitmap -ArgumentList (280 * $S), (112 * $S)
$g = [System.Drawing.Graphics]::FromImage($pv)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$g.Clear((C '#6e5a4a'))
for ($f = 0; $f -lt 4; $f++) {
    $ox = 4 + $f * 68
    $g.DrawImage($ring, (New-Object System.Drawing.Rectangle ($ox * $S), (4 * $S), (64 * $S), (32 * $S)), (New-Object System.Drawing.Rectangle 0, 0, 64, 32), [System.Drawing.GraphicsUnit]::Pixel)
    $g.DrawImage($tele, (New-Object System.Drawing.Rectangle ($ox * $S), (4 * $S), (64 * $S), (32 * $S)), (New-Object System.Drawing.Rectangle ($f * 64), 0, 64, 32), [System.Drawing.GraphicsUnit]::Pixel) }
$g.DrawImage($ring, (New-Object System.Drawing.Rectangle (210 * $S), (60 * $S), (64 * $S), (32 * $S)), (New-Object System.Drawing.Rectangle 0, 0, 64, 32), [System.Drawing.GraphicsUnit]::Pixel)
$bar = New-Object System.Drawing.SolidBrush (C '#1c1f26'); $red = New-Object System.Drawing.SolidBrush (C '#e04848')
$g.FillRectangle($bar, 8 * $S, 48 * $S, 128 * $S, 12 * $S)
$g.FillRectangle($red, 10 * $S, 50 * $S, 80 * $S, 8 * $S)
foreach ($nx in @(@(51, 0), @(93, 4))) {
    $g.DrawImage($notch, (New-Object System.Drawing.Rectangle ($nx[0] * $S), (48 * $S), (4 * $S), (12 * $S)), (New-Object System.Drawing.Rectangle $nx[1], 0, 4, 12), [System.Drawing.GraphicsUnit]::Pixel) }
$g.DrawImage($ban, (New-Object System.Drawing.Rectangle (8 * $S), (70 * $S), (24 * $S), (32 * $S)), (New-Object System.Drawing.Rectangle 0, 0, 24, 32), [System.Drawing.GraphicsUnit]::Pixel)
for ($tx = 32; $tx -lt 152; $tx += 48) {
    $g.DrawImage($ban, (New-Object System.Drawing.Rectangle ($tx * $S), (70 * $S), (48 * $S), (32 * $S)), (New-Object System.Drawing.Rectangle 24, 0, 48, 32), [System.Drawing.GraphicsUnit]::Pixel) }
$g.DrawImage($ban, (New-Object System.Drawing.Rectangle (176 * $S), (70 * $S), (24 * $S), (32 * $S)), (New-Object System.Drawing.Rectangle 72, 0, 24, 32), [System.Drawing.GraphicsUnit]::Pixel)
$pv.Save("$Out\Stage5_Preview_x5.png")
$g.Dispose()
"ok"
