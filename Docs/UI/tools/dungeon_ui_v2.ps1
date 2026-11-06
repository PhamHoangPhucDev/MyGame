# Dungeon map + minimap mockups v2, built only from the in-game UI kit sprites and the VT323 font.
# Layout is drawn on the 640x360 kit canvas, scaled x3 (nearest), then text is drawn at 1920x1080 like Unity does.
# -Game: project checkout that holds Assets/Art/UI/Game01, the VT323 font and Docs/UI/screens_runtime.
param([string]$Game = "D:\My Doc You\Game01\My project", [string]$Out = "Docs/UI/mockups/landscape/dungeon_v2")
Add-Type -AssemblyName System.Drawing
$P = (Resolve-Path "$PSScriptRoot\..\..\..").Path
$K = "$Game\Assets\Art\UI\Game01"
$S2 = "$P\Docs\WorldArt\DungeonStages\Stage2"
New-Item -ItemType Directory -Force "$P\$Out" | Out-Null

$fc = New-Object System.Drawing.Text.PrivateFontCollection
$fc.AddFontFile("$Game\Assets\Resources\Fonts\VT323-Regular.ttf")
$fam = $fc.Families[0]

function C([string]$h, [int]$a = 255) { [System.Drawing.Color]::FromArgb($a, [Convert]::ToInt32($h.Substring(1,2),16), [Convert]::ToInt32($h.Substring(3,2),16), [Convert]::ToInt32($h.Substring(5,2),16)) }
$cache = @{}
function Img([string]$path) { if (-not $cache.ContainsKey($path)) { $cache[$path] = [System.Drawing.Image]::FromFile($path) }; $cache[$path] }
function Kit([string]$name) { $f = Get-ChildItem $K -Recurse -Filter "$name.png" | Select-Object -First 1; Img $f.FullName }
function G($bmp) { $g = [System.Drawing.Graphics]::FromImage($bmp); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'; $g }
function Blit($g, $img, [int]$x, [int]$y, [int]$w = -1, [int]$h = -1) { if ($w -lt 0) { $w = $img.Width; $h = $img.Height }; $g.DrawImage($img, (New-Object System.Drawing.Rectangle $x, $y, $w, $h), 0, 0, $img.Width, $img.Height, [System.Drawing.GraphicsUnit]::Pixel) }
# 9-slice like Unity Sliced: l, t, r, b borders in sprite px.
function Slice($g, $img, [int]$x, [int]$y, [int]$w, [int]$h, [int]$l, [int]$t, [int]$r, [int]$b) {
    $iw = $img.Width; $ih = $img.Height
    $sx = @(0, $l, ($iw - $r)); $sw = @($l, ($iw - $l - $r), $r)
    $sy = @(0, $t, ($ih - $b)); $sh = @($t, ($ih - $t - $b), $b)
    $dx = @($x, ($x + $l), ($x + $w - $r)); $dw = @($l, ($w - $l - $r), $r)
    $dy = @($y, ($y + $t), ($y + $h - $b)); $dh = @($t, ($h - $t - $b), $b)
    for ($j = 0; $j -lt 3; $j++) { for ($i = 0; $i -lt 3; $i++) {
        if ($sw[$i] -le 0 -or $sh[$j] -le 0 -or $dw[$i] -le 0 -or $dh[$j] -le 0) { continue }
        $g.DrawImage($img, (New-Object System.Drawing.Rectangle $dx[$i], $dy[$j], $dw[$i], $dh[$j]), $sx[$i], $sy[$j], $sw[$i], $sh[$j], [System.Drawing.GraphicsUnit]::Pixel) } }
}
# Text on the x3 canvas; x/y/w in kit px, size in VT323 kit px (UiKit size 8 -> 12).
$ink = C '#141018'
function T($g, [string]$s, [double]$x, [double]$y, [double]$size, $col, [string]$align = 'L', [double]$w = 0, [bool]$outline = $true) {
    $f = New-Object System.Drawing.Font $fam, ([float]($size * 3)), ([System.Drawing.GraphicsUnit]::Pixel)
    $fmt = New-Object System.Drawing.StringFormat
    $fmt.Alignment = switch ($align) { 'C' { 'Center' } 'R' { 'Far' } default { 'Near' } }
    $rw = if ($w -le 0) { 3000 } else { [float]($w * 3) }
    $rect = New-Object System.Drawing.RectangleF ([float]($x * 3)), ([float]($y * 3)), $rw, ([float]($size * 3 + 12))
    if ($outline) { $ib = New-Object System.Drawing.SolidBrush $ink
        foreach ($o in @(@(-3,0),@(3,0),@(0,-3),@(0,3),@(-3,-3),@(3,-3),@(-3,3),@(3,3),@(0,6),@(3,6),@(-3,6))) {
            $r2 = New-Object System.Drawing.RectangleF ($rect.X + $o[0]), ($rect.Y + $o[1]), $rect.Width, $rect.Height
            $g.DrawString($s, $f, $ib, $r2, $fmt) } }
    $g.DrawString($s, $f, (New-Object System.Drawing.SolidBrush $col), $rect, $fmt)
}
function Up($small, $full = $null, [int]$dim = 0) { $big = New-Object System.Drawing.Bitmap -ArgumentList 1920, 1080; $g = G $big; if ($full) { $g.DrawImage($full, 0, 0, 1920, 1080); if ($dim -gt 0) { $g.FillRectangle((Brush '#0e1014' $dim), 0, 0, 1920, 1080) } }; $g.DrawImage($small, 0, 0, 1920, 1080); $g.TextRenderingHint = 'SingleBitPerPixelGridFit'; @($big, $g) }
function Brush([string]$h, [int]$a = 255) { New-Object System.Drawing.SolidBrush (C $h $a) }

$Gold = C '#f2d84a'; $White = C '#f4f4f0'; $Cyan = C '#5ed0e0'; $Green = C '#7ed67e'; $Mute = C '#aeb6c8'; $Grey = C '#7a8090'

# ---------- 1) Dungeon map screen ----------
$L = New-Object System.Drawing.Bitmap -ArgumentList 640, 360
$g = G $L
$bg = Img "$Game\Docs\UI\screens_runtime\FONT_L3_16x9.png"
$g.DrawImage($bg, (New-Object System.Drawing.Rectangle 0, 0, 640, 360), 0, 0, 1920, 1080, [System.Drawing.GraphicsUnit]::Pixel)
$g.FillRectangle((Brush '#0e1014' 170), 0, 0, 640, 360)
Slice $g (Kit 'UI_Panel_Main') 6 8 628 350 9 9 9 9
# tabs: wood buttons like the BẢN ĐỒ list; selected = gold, locked = grey
Slice $g (Kit 'UI_Button_Primary_Normal') 17 19 128 18 4 4 4 5
Slice $g (Kit 'UI_Button_Secondary_Disabled') 149 19 128 18 4 4 4 5
Slice $g (Kit 'UI_Button_Secondary_Disabled') 281 19 128 18 4 4 4 5
Blit $g (Kit 'UI_Icon_Lock') 259 21 14 14
Blit $g (Kit 'UI_Icon_Lock') 391 21 14 14
# board 1:1
Slice $g (Kit 'UI_Panel_Inset') 16 40 412 312 2 2 2 2
Blit $g (Kit 'UI_Dungeon_Board_GoblinCamp') 18 42
Blit $g (Kit 'UI_Dungeon_Fog_GoblinCamp_Stage4') 18 42
Blit $g (Kit 'UI_Dungeon_Fog_GoblinCamp_Stage5') 18 42
$pins = @(@(54,262),@(146,181),@(228,236),@(318,171),@(352,120))
$pinSprite = @('UI_WorldMap_Pin_Cleared','UI_WorldMap_Pin_Cleared','UI_WorldMap_Pin_Selected','UI_WorldMap_Pin_Locked','UI_Dungeon_Pin_Boss_Locked')
$badge = @('UI_Dungeon_MapType_Winding','UI_Dungeon_MapType_Maze','UI_Dungeon_MapType_Field')
for ($i = 0; $i -lt 4; $i++) {
    $a = $pins[$i]; $b = $pins[$i + 1]; $n = [int]([Math]::Sqrt([Math]::Pow($b[0]-$a[0],2)+[Math]::Pow($b[1]-$a[1],2)) / 6)
    $col = if ($i -lt 2) { '#f2d84a' } else { '#7a8090' }
    for ($kk = 2; $kk -lt $n - 1; $kk++) { $px = 18 + $a[0] + ($b[0]-$a[0]) * $kk / $n; $py = 42 + $a[1] + ($b[1]-$a[1]) * $kk / $n
        $g.FillRectangle((Brush '#141018'), [int]$px - 1, [int]$py - 1, 4, 4)
        $g.FillRectangle((Brush $col), [int]$px, [int]$py, 2, 2) } }
for ($i = 0; $i -lt 5; $i++) {
    $cx = 18 + $pins[$i][0]; $cy = 42 + $pins[$i][1]
    if ($i -eq 2) { Blit $g (Kit 'UI_WorldMap_Pin_SelectHalo') ($cx - 12) ($cy - 12) }
    Blit $g (Kit $pinSprite[$i]) ($cx - 6) ($cy - 6)
    if ($i -lt 3) { Blit $g (Kit $badge[$i]) ($cx - 5) ($cy + 8) }
    if ($i -lt 2) { Blit $g (Kit 'UI_WorldMap_Pin_Check') ($cx + 3) ($cy - 10) }
}
Blit $g (Kit 'UI_WorldMap_YouMarker') (18 + 228 - 4) (42 + 236 - 22)
Blit $g (Kit 'UI_WorldMap_Compass') 406 48
# side panel
Slice $g (Kit 'UI_Panel_Inset') 436 40 188 312 2 2 2 2
Blit $g (Kit 'UI_Dungeon_MapType_Field') 446 80 13 13
$div = Kit 'UI_Divider_Line'
foreach ($dy in @(134, 156, 214)) { $g.DrawImage($div, (New-Object System.Drawing.Rectangle 446, $dy, 168, 2), 0, 0, $div.Width, $div.Height, [System.Drawing.GraphicsUnit]::Pixel) }
foreach ($sx in @(446, 460, 474)) { Blit $g (Kit 'UI_Star_Large_Filled') $sx 139 12 12 }
$items = @('UI_Item_Coin','UI_Item_Wood','UI_Item_Spoils')
for ($i = 0; $i -lt 3; $i++) { $x = 446 + $i * 42; Slice $g (Kit 'UI_Dungeon_RewardSlot') $x 174 36 36 3 3 3 3; Blit $g (Kit $items[$i]) ($x + 2) 176 32 32 }
Slice $g (Kit 'UI_Button_Positive_Normal') 446 232 168 22 4 4 4 5
Slice $g (Kit 'UI_KeyCap') 594 236 13 13 3 3 3 3
Slice $g (Kit 'UI_NPC_DialoguePanel') 446 262 168 84 8 8 8 8
Blit $g (Kit 'UI_PortraitFrame') 454 270 30 30
Blit $g (Kit 'UI_Dungeon_Guard_Portrait') 457 273
Slice $g (Kit 'UI_Header_Ribbon') 240 0 160 18 12 0 12 0
Blit $g (Kit 'UI_Button_Close_Normal') 618 2
$r = Up $L; $F = $r[0]; $h = $r[1]
T $h 'BẢN ĐỒ PHÓ BẢN' 240 1 12 $Gold 'C' 160
T $h 'TRẠI GOBLIN' 17 21 12 (C '#3a2412') 'C' 128 $false
T $h 'ĐẦM LẦY MỤC' 149 21 12 $Grey 'C' 112
T $h 'HẺM NÚI XƯƠNG' 281 21 12 $Grey 'C' 112
foreach ($i in 0..2) { T $h ([string]($i + 1)) (18 + $pins[$i][0] - 6) (42 + $pins[$i][1] - 7) 12 $White 'C' 13 }
T $h 'ẢI 3 · ĐANG MỞ' 446 46 12 $Gold
T $h 'BÃI LỬA TRẠI' 446 57 20 $White
T $h 'BÃI ĐẤT TRỐNG' 464 80 12 $Mute
T $h 'ĐỀ NGHỊ LV.8' 446 94 12 $Cyan
T $h '4 ĐỢT · KHÔNG BOSS' 446 106 12 $White
T $h 'MỤC TIÊU: DIỆT HẾT QUÁI' 446 118 12 $Mute
T $h '1174 ĐIỂM · 02:21' 492 139 12 $White
T $h 'PHẦN THƯỞNG' 446 160 12 $Mute
foreach ($q in @(@(0,'120'),@(1,'8'),@(2,'1'))) { T $h $q[1] (446 + $q[0] * 42) 199 12 $White 'R' 34 }
T $h 'HẠ ẢI 3 ĐỂ MỞ ẢI 4' 446 218 12 $Green
T $h 'DỊCH CHUYỂN' 446 235 12 $White 'C' 146
T $h 'E' 594 235 12 $Gold 'C' 13
T $h 'LÍNH CANH' 490 271 12 $Gold
T $h 'Lính Khiên' 490 283 12 $Mute
T $h 'Bọn goblin dựng lửa trại' 454 303 12 $White
T $h 'ngay giữa bãi trống.' 454 314 12 $White
T $h 'Coi chừng bị vây đấy.' 454 325 12 $White
$F.Save("$P\$Out\L07c_Dungeon_Map_v2.png")

# ---------- minimap terrain: painted per pixel like a real chart (2 shades + noise, outlined canopies) ----------
function Hash([int]$x, [int]$y, [int]$k) { $n = ($x * 374761393 + $y * 668265263 + $k * 1442695041) -band 0x7fffffff; $n = (($n -bxor ($n -shr 13)) * 1274126177) -band 0x7fffffff; ($n % 1000) / 1000.0 }
$pal = @{
  'G' = @('#5f9e3e','#548f36'); 'D' = @('#b89466','#a8845a'); 'T' = @('#6a9e98','#5c8e88'); 'o' = @('#3e625e','#3e625e')
  'W' = @('#8a5a30','#7a4e2a'); 'Q' = @('#d4b483','#d4b483'); 'S' = @('#7a6656','#6e5a4a'); 'R' = @('#e8e0c8','#b4a88a')
  'K' = @('#1c1820','#26202a'); 'B' = @('#a86a40','#8a5a30'); 'X' = @('#5a3a20','#4e3220'); 'N' = @('#3f6e34','#3f6e34') }
# kind(x,y) -> palette key; returns a 68x68 bitmap
function Chart([scriptblock]$kind, [int]$seed) {
  $bm = New-Object System.Drawing.Bitmap -ArgumentList 68, 68
  $k = New-Object 'string[,]' 68, 68
  for ($y = 0; $y -lt 68; $y++) { for ($x = 0; $x -lt 68; $x++) { $k[$x, $y] = & $kind $x $y } }
  for ($y = 0; $y -lt 68; $y++) { for ($x = 0; $x -lt 68; $x++) {
    $c = $k[$x, $y]
    if ($c -eq 'T') { foreach ($d in @(@(1,0),@(-1,0),@(0,1),@(0,-1))) { $nx = $x + $d[0]; $ny = $y + $d[1]; if ($nx -ge 0 -and $ny -ge 0 -and $nx -lt 68 -and $ny -lt 68 -and $k[$nx, $ny] -ne 'T') { $c = 'o' } } }
    if ($c -eq 'G' -and $y -gt 0 -and $k[$x, ($y - 1)] -eq 'T') { $c = 'N' }
    $p = $pal[$c]; $col = if ((Hash $x $y $seed) -lt 0.7) { $p[0] } else { $p[1] }
    $bm.SetPixel($x, $y, (C $col)) } }
  $bm
}
function InBlob($x, $y, $blobs) { foreach ($b in $blobs) { $dx = $x - $b[0]; $dy = $y - $b[1]; if ($dx * $dx + $dy * $dy -le $b[2] * $b[2]) { return $true } }; $false }

$campTrees = @(@(6,10,5),@(9,32,6),@(5,54,5),@(60,30,6),@(61,8,5),@(60,54,6),@(20,48,4),@(48,50,4))
$camp = Chart { param($x, $y)
  if (InBlob $x $y $campTrees) { return 'T' }
  $e = [Math]::Pow(($x - 34) / 22.0, 2) + [Math]::Pow(($y - 16) / 13.0, 2)
  if ($e -le 1 -or ($x -ge 27 -and $x -le 40)) { return 'D' }
  'G' } 3
$trailTrees = @(@(50,50,7),@(60,28,5),@(6,22,6),@(22,6,5),@(58,62,4))
$trail = Chart { param($x, $y)
  if (InBlob $x $y $trailTrees) { return 'T' }
  $cx = 12 + (67 - $y) * 0.55 + 5 * [Math]::Sin($y / 7.0)
  if ([Math]::Abs($x - $cx) -le 4.5) { return 'D' }
  'G' } 5
$mazeRows = @(
'#################','#.....#.......#.#','#.###.#.#####.#.#','#.#...#.#...#...#','#.#.###.#.#.###.#','#.#.....#.#.....#',
'#.#######.#####.#','#.......#.....#.#','#######.#####.#.#','#.....#.....#...#','#.###.#####.###.#','#...#.....#...#.#',
'###.#####.###.#.#','#...#...#...#.#.#','#.###.#.###.#.#.#','#.....#.....#...#','########.########')
$maze = Chart { param($x, $y)
  $cx = [Math]::Min(16, [int][Math]::Floor($x / 4)); $cy = [Math]::Min(16, [int][Math]::Floor($y / 4))
  if ($mazeRows[$cy][$cx] -eq '.') { return 'D' }
  if (($y % 4) -eq 3 -and $cy -lt 16 -and $mazeRows[$cy + 1][$cx] -eq '.') { return 'Q' }
  if (($y % 4) -eq 3) { return 'W' }
  'X' } 7
$boss = Chart { param($x, $y)
  if ($x -lt 3 -or $x -gt 64 -or $y -lt 3) { if (($y % 4) -eq 0 -and $y -ge 3) { return 'Q' }; if ($y -lt 3 -and ($x % 4) -eq 0) { return 'Q' }; return 'W' }
  if ($x -ge 30 -and $x -le 37 -and $y -gt 58) { return 'D' }
  $e = [Math]::Sqrt([Math]::Pow(($x - 34) / 20.0, 2) + [Math]::Pow(($y - 28) / 12.0, 2))
  if ($e -gt 0.78 -and $e -lt 1.0) { return 'R' }
  'S' } 9
# soft fog for the maze: seen cells clear, one cell out half, the rest dark
$seenCell = { param($cx, $cy) ($cy -ge 7 -and $cx -le 11) -or ($cx -le 2 -and $cy -ge 1) -or ($cy -le 1 -and $cx -le 6) }
$fogBm = New-Object System.Drawing.Bitmap -ArgumentList 68, 68
for ($y = 0; $y -lt 68; $y++) { for ($x = 0; $x -lt 68; $x++) {
  $cx = [Math]::Min(16, [int][Math]::Floor($x / 4)); $cy = [Math]::Min(16, [int][Math]::Floor($y / 4))
  if (& $seenCell $cx $cy) { continue }
  $near = $false; foreach ($d in @(@(1,0),@(-1,0),@(0,1),@(0,-1))) { if (& $seenCell ($cx + $d[0]) ($cy + $d[1])) { $near = $true } }
  $a = if ($near) { if ((($x + $y) % 2) -eq 0) { 200 } else { 120 } } else { 225 }
  $fogBm.SetPixel($x, $y, (C '#0e1014' $a)) } }

# ---------- 2) Minimap in a run: real L3 camp HUD screenshot + new minimap block ----------
$M = New-Object System.Drawing.Bitmap -ArgumentList 640, 360
$g = G $M
$fx = 553; $fy = 38
Blit $g (Kit 'UI_Minimap_Frame') $fx $fy
$ox = $fx + 6; $oy = $fy + 6
Blit $g $camp $ox $oy
Blit $g (Kit 'UI_Minimap_Marker_Enemy') ($ox + 30) ($oy + 10)
Blit $g (Kit 'UI_Minimap_Marker_Enemy') ($ox + 42) ($oy + 18)
Blit $g (Kit 'UI_Minimap_Marker_Enemy') ($ox + 22) ($oy + 20)
Blit $g (Kit 'UI_Minimap_Marker_Chest') ($ox + 48) ($oy + 8)
Blit $g (Kit 'UI_Minimap_Marker_Gate') ($ox + 30) ($oy + 0)
Blit $g (Kit 'UI_Minimap_Marker_Player') ($ox + 30) ($oy + 54)
Slice $g (Kit 'UI_Header_Ribbon') ($fx - 6) ($fy + 80) 92 18 12 0 12 0
Slice $g (Kit 'UI_NamePlate') ($fx + 6) ($fy + 99) 68 15 4 4 4 4
$r = Up $M $bg; $F = $r[0]; $h = $r[1]
T $h 'BÃI LỬA TRẠI' ($fx - 6) ($fy + 81) 12 $Cyan 'C' 92
T $h 'QUÁI CÒN: 6' ($fx + 6) ($fy + 100) 12 $White 'C' 68
$F.Save("$P\$Out\L08_Dungeon_Minimap_v2.png")

# ---------- 3) Minimap states: Ải 1 trail / Ải 2 maze with fog / Ải 5 boss ----------
$B = New-Object System.Drawing.Bitmap -ArgumentList 640, 360
$g = G $B
$cols = @(60, 280, 500)
foreach ($x in $cols) { Blit $g (Kit 'UI_Minimap_Frame') $x 90 }
$ox = 66; $oy = 96
Blit $g $trail $ox $oy
Blit $g (Kit 'UI_Minimap_Marker_Player') ($ox + 14) ($oy + 56)
Blit $g (Kit 'UI_Minimap_Marker_Enemy') ($ox + 26) ($oy + 34)
Blit $g (Kit 'UI_Minimap_Marker_Enemy') ($ox + 32) ($oy + 24)
Blit $g (Kit 'UI_Map_EdgePlate') ($ox + 44) ($oy - 4); Blit $g (Kit 'UI_Minimap_Marker_Gate') ($ox + 45) ($oy - 3)
$ox = 286; $oy = 96
Blit $g $maze $ox $oy
Blit $g $fogBm $ox $oy
Blit $g (Kit 'UI_Minimap_Marker_Player') ($ox + 3 * 4 - 2) ($oy + 13 * 4 - 2)
$de = Img "$S2\Stage2_MiniMap_DeadEnd.png"
Blit $g $de ($ox + 1 * 4 - 2) ($oy + 11 * 4 - 2); Blit $g $de ($ox + 7 * 4 - 2) ($oy + 13 * 4 - 2)
Blit $g (Kit 'UI_Minimap_Marker_Chest') ($ox + 1 * 4 - 2) ($oy + 15 * 4 - 2)
Blit $g (Img "$S2\Stage2_ExitCompass.png") ($ox - 10) ($oy + 56)
$ox = 506; $oy = 96
Blit $g $boss $ox $oy
Blit $g (Kit 'UI_Minimap_Marker_Boss') ($ox + 31) ($oy + 25)
Blit $g (Kit 'UI_Minimap_Marker_Enemy') ($ox + 20) ($oy + 22)
Blit $g (Kit 'UI_Minimap_Marker_Enemy') ($ox + 44) ($oy + 30)
Blit $g (Kit 'UI_Minimap_Marker_Player') ($ox + 31) ($oy + 56)
foreach ($x in $cols) { Slice $g (Kit 'UI_Header_Ribbon') ($x - 6) 172 92 18 12 0 12 0 }
$r = Up $B $bg 185; $F = $r[0]; $h = $r[1]
T $h 'MINIMAP TRONG ẢI' 0 232 18 $Gold 'C' 640
T $h 'Cùng khung gỗ với nút túi / bản đồ, tên vùng trên ribbon xanh' 0 254 12 $White 'C' 640
$names = @('ĐƯỜNG ĐẤT ĐỎ', 'HÀNG RÀO NHỌN', 'LỀU XƯƠNG')
$notes = @(@('Cổng ngoài khung:', 'ghim ở mép chỉ hướng'), @('Sương che ô chưa đi,', 'dấu X ngõ cụt, la bàn chỉ cổng'), @('Sọ đỏ = boss,', 'vòng xương giữa sân'))
for ($i = 0; $i -lt 3; $i++) { $x = $cols[$i]
    T $h $names[$i] ($x - 6) 173 12 $Cyan 'C' 92
    T $h $notes[$i][0] ($x - 40) 196 12 $White 'C' 160
    T $h $notes[$i][1] ($x - 40) 208 12 $Mute 'C' 160 }
$F.Save("$P\$Out\L08b_Minimap_States_v2.png")
"ok"
