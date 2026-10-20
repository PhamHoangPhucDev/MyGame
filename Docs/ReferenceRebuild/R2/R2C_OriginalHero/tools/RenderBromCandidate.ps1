# Brom candidate board -> ..\brom\BROM_CANDIDATE_v1.png
$D = Split-Path $PSScriptRoot -Parent
$O = Join-Path $D "brom"; New-Item -ItemType Directory -Force $O | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\VfxConceptGen.cs", "$D\tools\BromGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$pal = [BromGen]::Pal
foreach ($v in @("Up", "Side")) { $rows = [BromGen]::Base($v); if ($rows.Length -ne 21) { throw "$v rows" }; for ($y = 0; $y -lt 21; $y++) { if ($rows[$y].Length -ne 14) { throw "$v row $y len $($rows[$y].Length)" }; foreach ($ch in $rows[$y].ToCharArray()) { if ($ch -ne '.' -and -not $pal.ContainsKey($ch)) { throw "$v row $y char $ch" } } } }

$grassC = [System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87); $grass = New-Object System.Drawing.SolidBrush $grassC
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$f = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fs = New-Object System.Drawing.Font "Consolas", 9
$K = 5; $sz = 48 * $K
$b = New-Object System.Drawing.Bitmap (4 * ($sz + 10) + 10), (2 * ($sz + 34) + 420); $g = [System.Drawing.Graphics]::FromImage($b); $g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
function Tile($bmp, $x, $y, $label) { $g.FillRectangle($grass, $x, $y, $sz, $sz); $g.FillEllipse($shadow, $x + 16 * $K, $y + 36 * $K, 16 * $K, 3 * $K); $g.DrawImage($bmp, (New-Object System.Drawing.Rectangle $x, $y, $sz, $sz)); $g.DrawString($label, $fs, [System.Drawing.Brushes]::White, $x + 3, $y + 3) }
$g.DrawString("Brom CANDIDATE - dung yen + trang bi (Down da duyet; Up, Side MOI) | x5", $f, [System.Drawing.Brushes]::Yellow, 10, 6)
$turn = @(@("Down", $false, "Down (da duyet)"), @("Side", $false, "Side 3/4 phai (MOI)"), @("Up", $false, "Up sau lung (MOI)"), @("Side", $true, "Side trai (flipX)"))
for ($i = 0; $i -lt 4; $i++) { Tile ([BromGen]::Compose($turn[$i][0], $false, $turn[$i][1])) (10 + $i * ($sz + 10)) 30 $turn[$i][2] }
$y = 30 + $sz + 34
$g.DrawString("Don danh (frame strike): bua bo xuong / qua dau / ngang ve truoc, khien thu", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
$st = @(@("Down", $false, "Strike Down"), @("Side", $false, "Strike Side"), @("Up", $false, "Strike Up"), @("Side", $true, "Strike Side trai"))
for ($i = 0; $i -lt 4; $i++) { Tile ([BromGen]::Compose($st[$i][0], $true, $st[$i][1])) (10 + $i * ($sz + 10)) $y $st[$i][2] }
$y += $sz + 34
$g.DrawString("VFX key frame: 2 Nen Dat (vong nut dat + bui + da) | kich thuoc that x1", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
$gs = [BromGen]::GroundSlamTile()
$g.DrawImage($gs, (New-Object System.Drawing.Rectangle 10, $y, 384, 256))
$xx = 420
for ($i = 0; $i -lt 4; $i++) { $bm = [BromGen]::Compose($turn[$i][0], $false, $turn[$i][1]); $g.FillRectangle($grass, $xx, $y, 52, 52); $g.DrawImage($bm, (New-Object System.Drawing.Rectangle ($xx + 2), ($y + 2), 48, 48)); $xx += 60 }
$g.DrawImage($gs, (New-Object System.Drawing.Rectangle 420, ($y + 70), 96, 64))
$b.Save((Join-Path $O "BROM_CANDIDATE_v1.png")); "ok"
