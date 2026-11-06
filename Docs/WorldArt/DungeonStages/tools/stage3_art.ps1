param([string]$Out)
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $Out | Out-Null

function C([string]$h, [int]$a = 255) { [System.Drawing.Color]::FromArgb($a, [Convert]::ToInt32($h.Substring(1,2),16), [Convert]::ToInt32($h.Substring(3,2),16), [Convert]::ToInt32($h.Substring(5,2),16)) }
function NewBmp([int]$w, [int]$h) { New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb) }
function Paint($bmp, [string[]]$rows, [hashtable]$pal, [int]$ox) {
    for ($y = 0; $y -lt $rows.Length; $y++) { for ($x = 0; $x -lt $rows[$y].Length; $x++) {
        $ch = [string]$rows[$y][$x]
        if ($pal.ContainsKey($ch)) { $bmp.SetPixel($ox + $x, $y, $pal[$ch]) } } }
}

# 1) Edge warning arrow, 3 frames (48x16). Points right; rotate toward the incoming side.
$warnRows = @(
'................',
'........K.......',
'........KK......',
'........KRK.....',
'.KKKKKKKKHRK....',
'.KRRRRRRRHHRK...',
'.KRHHHHHHHHHRK..',
'.KRHWWHHHHHHHRK.',
'.KRHWWHHHHHHHRK.',
'.KRHHHHHHHHHRK..',
'.KRRRRRRRHHRK...',
'.KKKKKKKKHRK....',
'........KRK.....',
'........KK......',
'........K.......',
'................')
$warnFrames = @(
    @{ 'K' = (C '#141018'); 'R' = (C '#a32d2d'); 'H' = (C '#e04848'); 'W' = (C '#f7c1c1') },
    @{ 'K' = (C '#141018'); 'R' = (C '#e04848'); 'H' = (C '#ff7a6a'); 'W' = (C '#ffffff') },
    @{ 'K' = (C '#141018'); 'R' = (C '#791f1f'); 'H' = (C '#a32d2d'); 'W' = (C '#e04848') })
$warn = NewBmp 48 16
for ($f = 0; $f -lt 3; $f++) { Paint $warn $warnRows $warnFrames[$f] ($f * 16) }
$warn.Save("$Out\Stage3_SurroundWarn_Sheet_3f.png")

# 2) Encircle ring under the hero, 2 frames (96x24 = 2 x 48x24). Squashed ellipse for 2.5D, dashed.
$ring = NewBmp 96 24
$cx = 23.5; $cy = 11.5
function Inside([double]$x, [double]$y, [double]$a, [double]$b) { $dx = ($x - $cx) / $a; $dy = ($y - $cy) / $b; return ($dx * $dx + $dy * $dy) -le 1.0 }
function Edge([int]$x, [int]$y, [double]$a, [double]$b) {
    if (-not (Inside $x $y $a $b)) { return $false }
    return -not ((Inside ($x - 1) $y $a $b) -and (Inside ($x + 1) $y $a $b) -and (Inside $x ($y - 1) $a $b) -and (Inside $x ($y + 1) $a $b))
}
for ($f = 0; $f -lt 2; $f++) {
    $a = if ($f -eq 0) { 20.5 } else { 22.0 }
    $b = if ($f -eq 0) { 9.5 } else { 10.5 }
    $outer = if ($f -eq 0) { C '#e04848' 210 } else { C '#ff6a5a' 245 }
    $inner = if ($f -eq 0) { C '#f7c1c1' 150 } else { C '#fcebeb' 190 }
    for ($y = 0; $y -lt 24; $y++) { for ($x = 0; $x -lt 48; $x++) {
        $ang = [Math]::Atan2(($y - $cy) / $b, ($x - $cx) / $a) * 180.0 / [Math]::PI + 180.0
        if (([int](($ang + 11.25) / 22.5)) % 4 -eq 3) { continue }
        $px = $null
        if ((Edge $x $y $a $b) -or (Edge $x $y ($a - 1.0) ($b - 1.0))) { $px = $outer }
        elseif (Edge $x $y ($a - 2.0) ($b - 2.0)) { $px = $inner }
        if ($px -ne $null) { $ring.SetPixel($f * 48 + $x, $y, $px) } } }
}
$ring.Save("$Out\Stage3_EncircleRing_Sheet_2f.png")

# Preview x6 on the camp dirt colour, ring around a stand-in hero box, warning arrows at the edges
$S = 6
$pv = New-Object System.Drawing.Bitmap -ArgumentList (176 * $S), (64 * $S)
$g = [System.Drawing.Graphics]::FromImage($pv)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$g.Clear((C '#b8946a'))
for ($f = 0; $f -lt 3; $f++) {
    $g.DrawImage($warn, (New-Object System.Drawing.Rectangle ((4 + $f * 20) * $S), (8 * $S), (16 * $S), (16 * $S)), (New-Object System.Drawing.Rectangle ($f * 16), 0, 16, 16), [System.Drawing.GraphicsUnit]::Pixel) }
for ($f = 0; $f -lt 2; $f++) {
    $ox = 70 + $f * 50
    $g.DrawImage($ring, (New-Object System.Drawing.Rectangle ($ox * $S), (30 * $S), (48 * $S), (24 * $S)), (New-Object System.Drawing.Rectangle ($f * 48), 0, 48, 24), [System.Drawing.GraphicsUnit]::Pixel)
    $hero = New-Object System.Drawing.SolidBrush (C '#3c3489')
    $g.FillRectangle($hero, ($ox + 18) * $S, 14 * $S, 12 * $S, 28 * $S) }
$pv.Save("$Out\Stage3_Preview_x6.png")
$g.Dispose()
"ok"
