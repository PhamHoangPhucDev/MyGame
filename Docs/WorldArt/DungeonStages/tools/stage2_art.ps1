param([string]$Out)
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $Out | Out-Null

function C([string]$h, [int]$a = 255) { [System.Drawing.Color]::FromArgb($a, [Convert]::ToInt32($h.Substring(1,2),16), [Convert]::ToInt32($h.Substring(3,2),16), [Convert]::ToInt32($h.Substring(5,2),16)) }
function NewBmp([int]$w, [int]$h) { New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb) }
function Paint($bmp, [string[]]$rows, [hashtable]$pal, [int]$ox, [int]$oy) {
    for ($y = 0; $y -lt $rows.Length; $y++) { for ($x = 0; $x -lt $rows[$y].Length; $x++) {
        $ch = [string]$rows[$y][$x]
        if ($pal.ContainsKey($ch)) { $bmp.SetPixel($ox + $x, $oy + $y, $pal[$ch]) } } }
}

$ink = C '#141018'; $wl = C '#b07840'; $wm = C '#8a5a30'; $wd = C '#5a3a20'; $tip = C '#d4b483'; $shadow = C '#141018' 70
$gold = C '#f2d84a'; $goldD = C '#ba7517'; $red = C '#e04848'; $redD = C '#a32d2d'

# 1) Fence autotile: 64x64 sheet, 4x4 tiles of 16x16. Tile index = mask, N=1 E=2 S=4 W=8, laid out row-major.
$fence = NewBmp 64 64
function Px($b, [int]$x, [int]$y, $c) { if ($x -ge 0 -and $x -lt 64 -and $y -ge 0 -and $y -lt 64) { $b.SetPixel($x, $y, $c) } }
function HArm($b, [int]$ox, [int]$oy, [int]$x0, [int]$x1) {
    for ($x = $x0; $x -le $x1; $x++) {
        $c = $x % 3; $top = if ($c -eq 1) { 3 } else { 4 }
        Px $b ($ox + $x) ($oy + 14) $shadow
        for ($y = $top; $y -le 13; $y++) {
            $col = if ($c -eq 0) { $ink } elseif ($c -eq 1) { $wl } else { $wm }
            if ($y -eq $top) { $col = if ($c -eq 1) { $tip } else { $ink } }
            if ($y -eq 13) { $col = $ink }
            if ($c -ne 0 -and ($y -eq 8 -or $y -eq 9)) { $col = $wd }
            Px $b ($ox + $x) ($oy + $y) $col } }
}
function VArm($b, [int]$ox, [int]$oy, [int]$y0, [int]$y1) {
    for ($y = $y0; $y -le $y1; $y++) {
        $r = $y % 3
        Px $b ($ox + 10) ($oy + $y) $shadow
        for ($x = 6; $x -le 9; $x++) {
            $col = if ($x -eq 6 -or $x -eq 9) { $ink } elseif ($r -eq 0) { $ink } elseif ($r -eq 1) { $tip } else { $wm }
            Px $b ($ox + $x) ($oy + $y) $col } }
}
function Post($b, [int]$ox, [int]$oy) {
    for ($y = 1; $y -le 13; $y++) { for ($x = 6; $x -le 9; $x++) {
        $col = if ($x -eq 6 -or $x -eq 9) { $ink } elseif ($x -eq 7) { $wl } else { $wm }
        if ($y -eq 13) { $col = $ink }
        if ($y -eq 1) { $col = if ($x -eq 7 -or $x -eq 8) { $ink } else { [System.Drawing.Color]::Transparent } }
        if ($y -eq 2 -and ($x -eq 7 -or $x -eq 8)) { $col = $tip }
        if ($y -eq 8 -and ($x -eq 7 -or $x -eq 8)) { $col = $wd }
        if ($col.A -gt 0) { Px $b ($ox + $x) ($oy + $y) $col } } }
    Px $b ($ox + 10) ($oy + 14) $shadow; Px $b ($ox + 7) ($oy + 14) $shadow; Px $b ($ox + 8) ($oy + 14) $shadow
}
for ($m = 0; $m -lt 16; $m++) {
    $ox = ($m % 4) * 16; $oy = [int][Math]::Floor($m / 4) * 16
    if ($m -band 1) { VArm $fence $ox $oy 0 5 }
    if ($m -band 8) { HArm $fence $ox $oy 0 6 }
    if ($m -band 2) { HArm $fence $ox $oy 9 15 }
    Post $fence $ox $oy
    if ($m -band 4) { VArm $fence $ox $oy 12 15 }
}
$fence.Save("$Out\Stage2_Fence_Autotile_4x4.png")

# 2) Gate in an E-W fence run, 2 frames (32x16): closed | open
$gate = NewBmp 32 16
foreach ($f in 0, 1) {
    $ox = $f * 16
    $b = NewBmp 16 16
    HArm $b 0 0 0 1; HArm $b 0 0 14 15
    $g2 = [System.Drawing.Graphics]::FromImage($gate); $g2.DrawImage($b, $ox, 0); $g2.Dispose()
    $postRows = @('KK', 'KT', 'KL', 'KL', 'KL', 'KL', 'KL', 'KL', 'KL', 'KL', 'KL', 'KL', 'KK')
    Paint $gate $postRows @{ 'K' = $ink; 'L' = $wl; 'T' = $tip } ($ox + 2) 1
    Paint $gate $postRows @{ 'K' = $ink; 'L' = $wm; 'T' = $tip } ($ox + 12) 1
    if ($f -eq 0) {
        $door = @(
        'KKKKKKKK',
        'KLMLMLMK',
        'KLMLMLMK',
        'KDDDDDDK',
        'KLMLMLMK',
        'KLMGMLMK',
        'KLMLMLMK',
        'KDDDDDDK',
        'KLMLMLMK',
        'KKKKKKKK')
        Paint $gate $door @{ 'K' = $ink; 'L' = $wl; 'M' = $wm; 'D' = $wd; 'G' = $gold } ($ox + 4) 4
    } else {
        $swing = @('KK', 'KL', 'KL', 'KD', 'KL', 'KL', 'KL', 'KD', 'KL', 'KK')
        Paint $gate $swing @{ 'K' = $ink; 'L' = $wl; 'D' = $wd } ($ox + 4) 4
        Paint $gate $swing @{ 'K' = $ink; 'L' = $wm; 'D' = $wd } ($ox + 10) 4
    }
    for ($x = 3; $x -le 13; $x++) { $gate.SetPixel($ox + $x, 14, $shadow) }
}
$gate.Save("$Out\Stage2_FenceGate_2f.png")

# 3) Spike hazard (16x16): sharpened stakes leaning out of the ground
$spikes = NewBmp 16 16
$sp = @(
'......K.........',
'.....KTK....K...',
'.....KLK...KTK..',
'.K...KLK...KLK..',
'KTK..KMK...KLK..',
'KLK..KMK...KMK..',
'KLK..KMK...KMK..',
'KMK.KKKKK..KMK..',
'KMK.KDDDK.KKKKK.',
'KKKKKKKKK.KDDDK.',
'KDDDK.....KKKKK.',
'KKKKK....K......',
'........KTK.....',
'.......KKKKK....',
'.......KDDDK....',
'.......KKKKK....')
Paint $spikes $sp @{ 'K' = $ink; 'T' = $tip; 'L' = $wl; 'M' = $wm; 'D' = $wd } 0 0
$spikes.Save("$Out\Stage2_Spikes.png")

# 4) Small chest, 2 frames (32x16): closed | open
$chest = NewBmp 32 16
$closed = @(
'................',
'................',
'................',
'...KKKKKKKKKK...',
'..KLLLLLLLLLLK..',
'..KMMMMMMMMMMK..',
'..KOOOOOOOOOOK..',
'..KGGGGKKGGGGK..',
'..KMMMMGGMMMMK..',
'..KMMMMKKMMMMK..',
'..KLLLLLLLLLLK..',
'..KMMMMMMMMMMK..',
'..KOOOOOOOOOOK..',
'..KKKKKKKKKKKK..',
'...SSSSSSSSSS...',
'................')
$open = @(
'...KKKKKKKKKK...',
'..KMMMMMMMMMMK..',
'..KLLLLLLLLLLK..',
'..KOOOOOOOOOOK..',
'..KKKKKKKKKKKK..',
'..KDDDDDDDDDDK..',
'..KDYYGYYYGYDK..',
'..KGGGGGGGGGGK..',
'..KMMMMGGMMMMK..',
'..KMMMMKKMMMMK..',
'..KLLLLLLLLLLK..',
'..KMMMMMMMMMMK..',
'..KOOOOOOOOOOK..',
'..KKKKKKKKKKKK..',
'...SSSSSSSSSS...',
'................')
$cp = @{ 'K' = $ink; 'L' = $wl; 'M' = $wm; 'D' = $wd; 'O' = $goldD; 'G' = $gold; 'Y' = (C '#fff6c0'); 'S' = $shadow }
Paint $chest $closed $cp 0 0; Paint $chest $open $cp 16 0
$chest.Save("$Out\Stage2_Chest_2f.png")

# 5) Minimap dead-end mark (8x8) and exit compass (16x16, needle points up = toward exit, rotate in UI)
$x8 = NewBmp 8 8
Paint $x8 @('KK....KK', 'KRK..KRK', '.KRKKRK.', '..KRRK..', '..KRRK..', '.KRKKRK.', 'KRK..KRK', 'KK....KK') @{ 'K' = $ink; 'R' = $red } 0 0
$x8.Save("$Out\Stage2_MiniMap_DeadEnd.png")
$comp = NewBmp 16 16
$cr = @(
'.....KKKKKK.....',
'...KKPPPPPPKK...',
'..KPPPPRRPPPPK..',
'.KPPPPRRRRPPPPK.',
'.KPPPPRRRRPPPPK.',
'KPPPPRRRRRRPPPPK',
'KPPPPPRRRRPPPPPK',
'KPPPPPPKKPPPPPPK',
'KPPPPPPKKPPPPPPK',
'KPPPPPWWWWPPPPPK',
'KPPPPWWWWWWPPPPK',
'.KPPPPWWWWPPPPK.',
'.KPPPPPWWPPPPPK.',
'..KPPPPPPPPPPK..',
'...KKPPPPPPKK...',
'.....KKKKKK.....')
Paint $comp $cr @{ 'K' = $ink; 'P' = (C '#2c2f38'); 'R' = $red; 'W' = (C '#e8e0c8') } 0 0
$comp.Save("$Out\Stage2_ExitCompass.png")

# Preview x4: a small maze built from the autotile + props, then the loose pieces
$maze = @(
'#############',
'#.....#.....#',
'#.###.#.###.#',
'#.#.....#S..#',
'#.#.###.#####',
'#...#C#.....#',
'#####.#####.#',
'#.........#.#',
'###########=#')
$S = 4; $W = $maze[0].Length; $H = $maze.Length
$pv = New-Object System.Drawing.Bitmap -ArgumentList (($W * 16 + 96) * $S), ([Math]::Max($H * 16, 80) * $S)
$g = [System.Drawing.Graphics]::FromImage($pv)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$g.Clear((C '#5a7a3a'))
function IsWall([int]$cx, [int]$cy) { if ($cy -lt 0 -or $cy -ge $H -or $cx -lt 0 -or $cx -ge $W) { return $false }; $ch = $maze[$cy][$cx]; return ($ch -eq '#' -or $ch -eq '=') }
function Blit($src, [int]$sx, [int]$sy, [int]$sw, [int]$sh, [int]$dx, [int]$dy) {
    $g.DrawImage($src, (New-Object System.Drawing.Rectangle ($dx * $S), ($dy * $S), ($sw * $S), ($sh * $S)), (New-Object System.Drawing.Rectangle $sx, $sy, $sw, $sh), [System.Drawing.GraphicsUnit]::Pixel)
}
for ($cy = 0; $cy -lt $H; $cy++) { for ($cx = 0; $cx -lt $W; $cx++) {
    $ch = $maze[$cy][$cx]
    if ($ch -eq '#') {
        $m = 0
        if (IsWall $cx ($cy - 1)) { $m += 1 }; if (IsWall ($cx + 1) $cy) { $m += 2 }; if (IsWall $cx ($cy + 1)) { $m += 4 }; if (IsWall ($cx - 1) $cy) { $m += 8 }
        Blit $fence (($m % 4) * 16) ([int][Math]::Floor($m / 4) * 16) 16 16 ($cx * 16) ($cy * 16) }
    elseif ($ch -eq '=') { Blit $gate 0 0 16 16 ($cx * 16) ($cy * 16) }
    elseif ($ch -eq 'S') { Blit $spikes 0 0 16 16 ($cx * 16) ($cy * 16) }
    elseif ($ch -eq 'C') { Blit $chest 0 0 16 16 ($cx * 16) ($cy * 16) } } }
$px0 = $W * 16 + 8
Blit $gate 0 0 32 16 $px0 4
Blit $chest 0 0 32 16 $px0 26
Blit $spikes 0 0 16 16 $px0 48
Blit $x8 0 0 8 8 ($px0 + 24) 52
Blit $comp 0 0 16 16 ($px0 + 40) 48
Blit $fence 0 0 64 64 ($px0 + 8) 72
$pv.Save("$Out\Stage2_Preview_x4.png")
$g.Dispose()
"ok"
