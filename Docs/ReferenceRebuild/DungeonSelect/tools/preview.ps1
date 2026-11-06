# Composes a 1920x1080 preview of the stage-select screen from the exported art.
# Text here is only a stand-in for Unity text. Run after run.ps1.
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..\..\..')).Path
$ex = (Resolve-Path (Join-Path $here '..\export')).Path
$wm = Join-Path $root 'Assets\Art\UI\Game01\WorldMap'
Add-Type -AssemblyName System.Drawing

function Img($p) { [System.Drawing.Image]::FromFile($p) }
function Col($h) { [System.Drawing.ColorTranslator]::FromHtml($h) }
$bmp = New-Object System.Drawing.Bitmap 1920, 1080
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$g.TextRenderingHint = 'SingleBitPerPixelGridFit'
$g.Clear((Col '#14161C'))
function Draw($img, $x, $y, $s) { $g.DrawImage($img, [int]$x, [int]$y, [int]($img.Width * $s), [int]($img.Height * $s)) }
function Box($x, $y, $w, $h, $fill, $border) {
  $g.FillRectangle((New-Object System.Drawing.SolidBrush (Col $fill)), $x, $y, $w, $h)
  $p = New-Object System.Drawing.Pen (Col $border), 3; $g.DrawRectangle($p, $x, $y, $w, $h)
}
function Txt($s, $x, $y, $size, $col, [switch]$center) {
  $f = New-Object System.Drawing.Font 'Consolas', $size, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
  $sf = New-Object System.Drawing.StringFormat; if ($center) { $sf.Alignment = 'Center' }
  $sh = New-Object System.Drawing.SolidBrush (Col '#141018')
  foreach ($o in @(@(-2,0),@(2,0),@(0,-2),@(0,2),@(2,2))) { $g.DrawString($s, $f, $sh, ($x + $o[0]), ($y + $o[1]), $sf) }
  $g.DrawString($s, $f, (New-Object System.Drawing.SolidBrush (Col $col)), $x, $y, $sf)
}

# header and dungeon tabs
Txt 'Bản đồ phó bản' 40 34 34 '#F2D84A'
$tabs = @(@('Trại Goblin', '#F4F4F0', '#F2D84A', '#252A33'), @('Đầm Lầy Mục', '#6A7080', '#4A5260', '#1C1F26'), @('Hẻm Núi Xương', '#6A7080', '#4A5260', '#1C1F26'))
$tx = 560
foreach ($t in $tabs) { Box $tx 28 300 54 $t[3] $t[2]; Txt $t[0] ($tx + 136) 40 26 $t[1] -center; $tx += 316 }
$lock = Img (Join-Path $wm 'UI_WorldMap_Pin_Locked.png')
Draw $lock 1130 35 3; Draw $lock 1446 35 3
Txt 'X' 1862 34 30 '#AEB6C8'

# board, scaled 3x, with fog over locked stages 4 and 5
$bx = 40; $by = 112; $s = 3
Box ($bx - 6) ($by - 6) (408 * $s + 12) (308 * $s + 12) '#1C1F26' '#4A5260'
Draw (Img (Join-Path $ex 'UI_Dungeon_Board_GoblinCamp.png')) $bx $by $s
Draw (Img (Join-Path $ex 'UI_Dungeon_Fog_GoblinCamp_Stage4.png')) $bx $by $s
Draw (Img (Join-Path $ex 'UI_Dungeon_Fog_GoblinCamp_Stage5.png')) $bx $by $s

# pins: 1-2 cleared, 3 selected, 4 locked, 5 boss locked
$pins = @(@(54, 262), @(146, 181), @(228, 236), @(318, 171), @(352, 120))
$types = @('Winding', 'Maze', 'Field', 'Ledge')
$halo = Img (Join-Path $wm 'UI_WorldMap_Pin_SelectHalo.png')
for ($i = 0; $i -lt 5; $i++) {
  $px = $bx + $pins[$i][0] * $s; $py = $by + $pins[$i][1] * $s
  if ($i -eq 2) { Draw $halo ($px - 12 * $s) ($py - 12 * $s) $s }
  $name = @('UI_WorldMap_Pin_Cleared', 'UI_WorldMap_Pin_Cleared', 'UI_WorldMap_Pin_Selected', 'UI_WorldMap_Pin_Locked')
  if ($i -lt 4) { Draw (Img (Join-Path $wm ($name[$i] + '.png'))) ($px - 6 * $s) ($py - 6 * $s) $s }
  else { Draw (Img (Join-Path $ex 'UI_Dungeon_Pin_Boss_Locked.png')) ($px - 6 * $s) ($py - 6 * $s) $s }
  if ($i -lt 3) { Txt ([string]($i + 1)) ($px + 1) ($py - 15) 26 '#F4F4F0' -center }
  if ($i -lt 2) { Draw (Img (Join-Path $wm 'UI_WorldMap_Pin_Check.png')) ($px + 3 * $s) ($py - 10 * $s) $s }
  if ($i -lt 4) { Draw (Img (Join-Path $ex ('UI_Dungeon_MapType_' + $types[$i] + '.png'))) ($px - 5 * $s) ($py + 8 * $s) $s }
}
Draw (Img (Join-Path $wm 'UI_WorldMap_YouMarker.png')) ($bx + 224 * $s) ($by + 214 * $s) $s

# side panel
$sx = 1300; $sy = 112; $sw = 580
Box $sx ($sy - 6) $sw 936 '#1C1F26' '#4A5260'
Txt 'Ải 3 · Đang mở' ($sx + 28) ($sy + 22) 28 '#F2D84A'
Txt 'Bãi lửa trại' ($sx + 28) ($sy + 66) 40 '#F4F4F0'
Draw (Img (Join-Path $ex 'UI_Dungeon_MapType_Field.png')) ($sx + 28) ($sy + 130) 4
Txt 'Bãi đất trống' ($sx + 82) ($sy + 136) 26 '#AEB6C8'
Txt 'Đề nghị Lv.8' ($sx + 28) ($sy + 194) 28 '#5ED0E0'
$g.FillRectangle((New-Object System.Drawing.SolidBrush (Col '#4A5260')), ($sx + 28), ($sy + 246), ($sw - 56), 3)
Txt 'Phần thưởng' ($sx + 28) ($sy + 270) 24 '#AEB6C8'
$rx = $sx + 28
foreach ($r in 0..2) { Box $rx ($sy + 310) 96 96 '#20202A' '#5A5E6A'; $rx += 112 }
$g.FillPolygon((New-Object System.Drawing.SolidBrush (Col '#D8B040')), [System.Drawing.Point[]]@((New-Object System.Drawing.Point ($sx + 76), ($sy + 330)), (New-Object System.Drawing.Point ($sx + 104), ($sy + 358)), (New-Object System.Drawing.Point ($sx + 76), ($sy + 386)), (New-Object System.Drawing.Point ($sx + 48), ($sy + 358))))
Box ($sx + 162) ($sy + 340) 52 38 '#8A5A2A' '#D8B040'
$g.FillEllipse((New-Object System.Drawing.SolidBrush (Col '#BFE6EE')), ($sx + 290), ($sy + 336), 44, 44)
Txt 'Hạ ải 3 để mở ải 4' ($sx + 28) ($sy + 432) 26 '#7ED67E'
Box ($sx + 28) ($sy + 500) ($sw - 56) 90 '#4A3140' '#B89AA0'
Txt 'Dịch chuyển' ($sx + $sw / 2) ($sy + 524) 36 '#7ED67E' -center

# guard strip at the bottom of the panel
Box ($sx + 28) ($sy + 640) ($sw - 56) 260 '#1E2230' '#AEB6C8'
Box ($sx + 48) ($sy + 664) 96 96 '#3A4250' '#5A5E6A'
$g.FillRectangle((New-Object System.Drawing.SolidBrush (Col '#8A94A6')), ($sx + 66), ($sy + 676), 60, 30)
$g.FillRectangle((New-Object System.Drawing.SolidBrush (Col '#C9A58A')), ($sx + 72), ($sy + 706), 48, 42)
$g.FillRectangle((New-Object System.Drawing.SolidBrush (Col '#141018')), ($sx + 82), ($sy + 718), 8, 8)
$g.FillRectangle((New-Object System.Drawing.SolidBrush (Col '#141018')), ($sx + 102), ($sy + 718), 8, 8)
Txt 'Lính canh' ($sx + 164) ($sy + 676) 28 '#F2D84A'
Txt "Bọn goblin dựng lửa trại`nngay giữa bãi trống.`nCoi chừng bị vây đấy." ($sx + 48) ($sy + 780) 26 '#F6EFE2'

$out = Join-Path $here '..\preview\DungeonSelect_GoblinCamp_Preview.png'
New-Item -ItemType Directory -Force (Split-Path $out) | Out-Null
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
"saved $out"
