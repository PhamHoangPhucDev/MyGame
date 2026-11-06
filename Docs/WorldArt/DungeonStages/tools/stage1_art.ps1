param([string]$Out)
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $Out | Out-Null

function C([string]$h) { [System.Drawing.Color]::FromArgb(255, [Convert]::ToInt32($h.Substring(1,2),16), [Convert]::ToInt32($h.Substring(3,2),16), [Convert]::ToInt32($h.Substring(5,2),16)) }
function Hash([int]$x, [int]$y, [int]$s) { $n = ($x * 374761393 + $y * 668265263 + $s * 1442695041) -band 0x7fffffff; $n = (($n -bxor ($n -shr 13)) * 1274126177) -band 0x7fffffff; return ($n % 1000) / 1000.0 }

function Ascii([string[]]$rows, [hashtable]$pal) {
    $b = New-Object System.Drawing.Bitmap $rows[0].Length, $rows.Length, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt $rows.Length; $y++) { for ($x = 0; $x -lt $rows[$y].Length; $x++) {
        $ch = [string]$rows[$y][$x]
        if ($pal.ContainsKey($ch)) { $b.SetPixel($x, $y, $pal[$ch]) } else { $b.SetPixel($x, $y, [System.Drawing.Color]::Transparent) } } }
    return $b
}

# Red dirt palette (shifted from in-game Dirt #a88e60)
$base = C '#a8704a'; $dark = C '#8a5636'; $rim = C '#6e4430'; $lite = C '#c08a5a'; $peb = C '#5a3a28'; $hi = C '#d4a070'

# 1) Path decal 48x48, 9-slice border 16
$path = New-Object System.Drawing.Bitmap 48, 48, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
for ($y = 0; $y -lt 48; $y++) { for ($x = 0; $x -lt 48; $x++) {
    $d = [Math]::Min([Math]::Min($x, $y), [Math]::Min(47 - $x, 47 - $y))
    $r = Hash $x $y 1
    $col = [System.Drawing.Color]::Transparent
    if ($d -ge 4) {
        $col = $base
        if ($r -lt 0.10) { $col = $dark } elseif ($r -lt 0.16) { $col = $lite }
        $q = Hash $x $y 7
        if ($q -lt 0.018) { $col = $peb } elseif ((Hash ($x - 1) ($y + 1) 7) -lt 0.018) { $col = $hi }
    } elseif ($d -eq 3) { if ($r -lt 0.85) { $col = $dark } else { $col = $base } }
    elseif ($d -eq 2) { if ($r -lt 0.70) { $col = $rim } }
    elseif ($d -eq 1) { if ($r -lt 0.25) { $col = $rim } }
    $path.SetPixel($x, $y, $col) } }
$path.Save("$Out\Stage1_RedDirtPath_9s.png")

# 2) Guide arrow, frame 1 (points right, rotate in engine)
$arrowRows = @(
'................',
'................',
'..O.....O.......',
'.OYO...OYO......',
'..OYO...OYO.....',
'...OYO...OYO....',
'....OYO...OYO...',
'.....OYW...OYW..',
'....OYO...OYO...',
'...OYO...OYO....',
'..OYO...OYO.....',
'.OYO...OYO......',
'..O.....O.......',
'................',
'................',
'................')
$arrow = Ascii $arrowRows @{ 'O' = (C '#ba7517'); 'Y' = (C '#f2d84a'); 'W' = (C '#fff6c0') }

# 4-frame sheet (64x16): light runs back chevron -> front chevron, then dims. Columns < 8 = back chevron.
$dimP = @{ 'O' = (C '#8a5a10'); 'Y' = (C '#c99a2e'); 'W' = (C '#e6c860') }
$midP = @{ 'O' = (C '#ba7517'); 'Y' = (C '#f2d84a'); 'W' = (C '#fff6c0') }
$hotP = @{ 'O' = (C '#e0952a'); 'Y' = (C '#fff6c0'); 'W' = (C '#ffffff') }
$frames = @(@($midP, $midP), @($hotP, $midP), @($midP, $hotP), @($dimP, $dimP))
$sheet = New-Object System.Drawing.Bitmap 64, 16, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
for ($f = 0; $f -lt 4; $f++) {
    for ($y = 0; $y -lt 16; $y++) { for ($x = 0; $x -lt 16; $x++) {
        $ch = [string]$arrowRows[$y][$x]
        $pal = if ($x -lt 8) { $frames[$f][0] } else { $frames[$f][1] }
        if ($pal.ContainsKey($ch)) { $sheet.SetPixel($f * 16 + $x, $y, $pal[$ch]) } else { $sheet.SetPixel($f * 16 + $x, $y, [System.Drawing.Color]::Transparent) } } } }
$sheet.Save("$Out\Stage1_GuideArrow_Sheet_4f.png")

# 3) Gate icon for the screen edge
$gateRows = @(
'................',
'..K..........K..',
'.KLK.KKKKKK.KLK.',
'.KLKKWWWWWWKKLK.',
'.KLKWLLLLLLWKLK.',
'.KLKWLDDDDLWKLK.',
'.KLKWLDKKDLWKLK.',
'.KLKWLDKKDLWKLK.',
'.KLKWLDKKDLWKLK.',
'.KLKWLDKKDLWKLK.',
'.KLKWLDKKDLWKLK.',
'.KLKWLDKKDLWKLK.',
'.KLKKKKKKKKKKLK.',
'.KLK........KLK.',
'.KKK........KKK.',
'................')
$gate = Ascii $gateRows @{ 'K' = (C '#141018'); 'L' = (C '#b07840'); 'W' = (C '#8a5a30'); 'D' = (C '#5a3a20') }
$gate.Save("$Out\Stage1_GateEdgeIcon.png")

# Preview: x6 nearest-neighbour on grass, plus a 3x stretched 9-slice strip
$S = 6
$pv = New-Object System.Drawing.Bitmap -ArgumentList (256 * $S), (64 * $S)
$g = [System.Drawing.Graphics]::FromImage($pv)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$g.Clear((C '#4f7a34'))
$g.DrawImage($path, 8 * $S, 8 * $S, 48 * $S, 48 * $S)
$g.DrawImage($arrow, 72 * $S, 24 * $S, 16 * $S, 16 * $S)
$fp = New-Object System.Drawing.Bitmap -ArgumentList (80 * 8), (24 * 8)
$fg = [System.Drawing.Graphics]::FromImage($fp)
$fg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$fg.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$fg.Clear((C '#a8704a'))
for ($f = 0; $f -lt 4; $f++) {
    $fg.DrawImage($sheet, (New-Object System.Drawing.Rectangle (($f * 20 + 2) * 8), (4 * 8), (16 * 8), (16 * 8)), (New-Object System.Drawing.Rectangle ($f * 16), 0, 16, 16), [System.Drawing.GraphicsUnit]::Pixel) }
$fp.Save("$Out\Stage1_GuideArrow_Frames_x8.png")
$fg.Dispose()
$g.DrawImage($gate, 104 * $S, 24 * $S, 16 * $S, 16 * $S)
# 9-slice stretch demo: 112x32 strip built from the decal
$strip = New-Object System.Drawing.Bitmap 112, 32
$sg = [System.Drawing.Graphics]::FromImage($strip)
$sg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$sg.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$srcX = @(0, 16, 32); $dstX = @(0, 16, 96); $dstW = @(16, 80, 16)
$srcY = @(0, 16, 32); $dstY = @(0, 16, 16); $dstH = @(16, 0, 16)
foreach ($j in 0, 2) {
    $dy = @(0, 0, 16)[$j]
    $sg.DrawImage($path, (New-Object System.Drawing.Rectangle 0, $dy, 16, 16), (New-Object System.Drawing.Rectangle 0, $srcY[$j], 16, 16), [System.Drawing.GraphicsUnit]::Pixel)
    $sg.DrawImage($path, (New-Object System.Drawing.Rectangle 96, $dy, 16, 16), (New-Object System.Drawing.Rectangle 32, $srcY[$j], 16, 16), [System.Drawing.GraphicsUnit]::Pixel)
    for ($tx = 16; $tx -lt 96; $tx += 16) {
        $sg.DrawImage($path, (New-Object System.Drawing.Rectangle $tx, $dy, 16, 16), (New-Object System.Drawing.Rectangle 16, $srcY[$j], 16, 16), [System.Drawing.GraphicsUnit]::Pixel) } }
$g.DrawImage($strip, 136 * $S, 16 * $S, 112 * $S, 32 * $S)
$pv.Save("$Out\Stage1_Preview_x6.png")
$g.Dispose(); $sg.Dispose()
"ok"
