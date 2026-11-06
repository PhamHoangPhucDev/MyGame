param([string]$Out)
# Stage 2 maze: stone wall autotile (replaces the stake fence look). 64x64 = 16 tiles 16x16.
# Tile index = neighbour mask N=1 E=2 S=4 W=8, column mask%4, row mask/4 (same rule as the fence sheet).
# 2.5D: flat cobbled top face; a brick front face shows under any top edge that is not joined to the south.
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $Out | Out-Null
function C([string]$h, [int]$a = 255) { [System.Drawing.Color]::FromArgb($a, [Convert]::ToInt32($h.Substring(1,2),16), [Convert]::ToInt32($h.Substring(3,2),16), [Convert]::ToInt32($h.Substring(5,2),16)) }
function Hash([int]$x, [int]$y, [int]$k) { $n = ($x * 374761393 + $y * 668265263 + $k * 1442695041) -band 0x7fffffff; $n = (($n -bxor ($n -shr 13)) * 1274126177) -band 0x7fffffff; ($n % 1000) / 1000.0 }

$ink = C '#141018'
$topSeam = C '#55535f'; $top = C '#8e8c9a'; $topHi = C '#aaa8b6'; $topLo = C '#7a7886'; $moss = C '#6a8a3c'; $mossHi = C '#8aac4e'
$face = C '#5a5866'; $faceSeam = C '#44424e'; $faceHi = C '#6e6c7a'; $faceLo = C '#34323c'; $shadow = C '#141018' 90

function IsTop([int]$x, [int]$y, [int]$mask) {
    $n = ($mask -band 1) -ne 0; $e = ($mask -band 2) -ne 0; $s = ($mask -band 4) -ne 0; $w = ($mask -band 8) -ne 0
    if ($x -ge 2 -and $x -le 13 -and $y -ge 1 -and $y -le 9) { return $true }
    if ($n -and $x -ge 2 -and $x -le 13 -and $y -le 1) { return $true }
    if ($s -and $x -ge 2 -and $x -le 13 -and $y -ge 9) { return $true }
    if ($e -and $x -ge 13 -and $y -ge 1 -and $y -le 9) { return $true }
    if ($w -and $x -le 2 -and $y -ge 1 -and $y -le 9) { return $true }
    $false
}
function IsFace([int]$x, [int]$y, [int]$mask) { ($y -ge 10 -and $y -le 14) -and (IsTop $x 9 $mask) -and -not (IsTop $x $y $mask) }
# out-of-tile pixels count as solid only across a joined side
function Solid([int]$x, [int]$y, [int]$mask) {
    if ($x -lt 0) { return (($mask -band 8) -ne 0) -and $y -ge 1 -and $y -le 14 }
    if ($x -gt 15) { return (($mask -band 2) -ne 0) -and $y -ge 1 -and $y -le 14 }
    if ($y -lt 0) { return (($mask -band 1) -ne 0) -and $x -ge 2 -and $x -le 13 }
    if ($y -gt 15) { return (($mask -band 4) -ne 0) -and $x -ge 2 -and $x -le 13 }
    (IsTop $x $y $mask) -or (IsFace $x $y $mask)
}

function Tile($bmp, [int]$mask, [int]$ox, [int]$oy) {
    for ($y = 0; $y -lt 16; $y++) { for ($x = 0; $x -lt 16; $x++) {
        $isT = IsTop $x $y $mask; $isF = IsFace $x $y $mask
        if (-not $isT -and -not $isF) {
            if ($y -eq 15 -and (IsFace $x 14 $mask)) { $bmp.SetPixel($ox + $x, $oy + $y, $shadow) }
            continue }
        $edge = -not ((Solid ($x - 1) $y $mask) -and (Solid ($x + 1) $y $mask) -and (Solid $x ($y - 1) $mask) -and (Solid $x ($y + 1) $mask))
        if ($edge) { $bmp.SetPixel($ox + $x, $oy + $y, $ink); continue }
        if ($isT) {
            $row = [Math]::Floor(($y + 1) / 4); $off = if ($row % 2 -eq 0) { 0 } else { 3 }
            if ((($y + 1) % 4) -eq 0) { $col = $topSeam }
            elseif ((($x + $off) % 6) -eq 0) { $col = $topSeam }
            elseif ((($y + 1) % 4) -eq 1) { $col = $topHi }
            elseif ((($y + 1) % 4) -eq 3) { $col = $topLo }
            else { $col = $top }
            $hm = Hash ([int][Math]::Floor(($x + $off) / 6) + $mask * 7) $row 5
            if ($hm -lt 0.18 -and $col -ne $topSeam -and (Hash $x $y ($mask + 11)) -lt 0.55) { $col = if ((Hash $x $y 3) -lt 0.4) { $mossHi } else { $moss } }
        } else {
            if ($y -eq 10) { $col = $faceHi } elseif ($y -eq 14) { $col = $faceLo }
            elseif ($y -eq 12) { $col = $faceSeam }
            elseif ((($x + $(if ($y -lt 12) { 0 } else { 2 })) % 5) -eq 0) { $col = $faceSeam }
            else { $col = $face }
        }
        $bmp.SetPixel($ox + $x, $oy + $y, $col) } }
}

$sheet = New-Object System.Drawing.Bitmap -ArgumentList 64, 64
for ($m = 0; $m -lt 16; $m++) { Tile $sheet $m (($m % 4) * 16) ([Math]::Floor($m / 4) * 16) }
$sheet.Save("$Out\Stage2_StoneWall_Autotile_4x4.png")

# preview: the same 13x9 test maze as the fence sheet, on dirt, x4
$maze = @(
'#############',
'#...#.......#',
'#.#.#.#####.#',
'#.#...#...#.#',
'#.#####.#.#.#',
'#.....#.#...#',
'#####.#.#####',
'#.......#...#',
'#############')
$S = 4
$pv = New-Object System.Drawing.Bitmap -ArgumentList (13 * 16 * $S), (9 * 16 * $S)
$g = [System.Drawing.Graphics]::FromImage($pv); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$g.Clear((C '#b89466'))
for ($y = 0; $y -lt 9; $y++) { for ($x = 0; $x -lt 13; $x++) {
    if ($maze[$y][$x] -ne '#') { continue }
    $mask = 0
    if ($y -gt 0 -and $maze[$y - 1][$x] -eq '#') { $mask += 1 }; if ($x -lt 12 -and $maze[$y][$x + 1] -eq '#') { $mask += 2 }
    if ($y -lt 8 -and $maze[$y + 1][$x] -eq '#') { $mask += 4 }; if ($x -gt 0 -and $maze[$y][$x - 1] -eq '#') { $mask += 8 }
    $g.DrawImage($sheet, (New-Object System.Drawing.Rectangle ($x * 16 * $S), ($y * 16 * $S), (16 * $S), (16 * $S)), (($mask % 4) * 16), ([Math]::Floor($mask / 4) * 16), 16, 16, [System.Drawing.GraphicsUnit]::Pixel) } }
$pv.Save("$Out\Stage2_StoneWall_Preview_x4.png")
"ok"
