# Vex candidate: daggers sheathed crossed (X) on the back in Idle/Walk -> ..\vex\VEX_BACK_DAGGERS_v1.png (no export)
$D = Split-Path $PSScriptRoot -Parent
$O = Join-Path $D "vex"
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\FxGen.cs", "$D\tools\VfxConceptGen.cs", "$D\tools\VexGen.cs", "$D\tools\VexEquip.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$f = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fs = New-Object System.Drawing.Font "Consolas", 9
$K = 5; $sz = 48 * $K
$b = New-Object System.Drawing.Bitmap (4 * ($sz + 10) + 10), (3 * ($sz + 34) + 20); $g = [System.Drawing.Graphics]::FromImage($b); $g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
function Mirror($bmp) { $bmp.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX); return $bmp }
function Tile($bmp, $tx, $ty, $label) { $g.FillRectangle($grass, $tx, $ty, $sz, $sz); $g.FillEllipse($shadow, $tx + 16 * $K, $ty + 36 * $K, 16 * $K, 3 * $K); $g.DrawImage($bmp, (New-Object System.Drawing.Rectangle $tx, $ty, $sz, $sz)); $g.DrawString($label, $fs, [System.Drawing.Brushes]::White, $tx + 3, $ty + 3) }
$g.DrawString("Vex v6 - Idle/Walk: doi dao gam vat cheo chu X giua lung, chuoi huong len; attack moi rut dao | x5", $f, [System.Drawing.Brushes]::Yellow, 10, 6)
$row = @(@("Down", $false, "Down (chuoi dao nho sau 2 vai)"), @("Side", $false, "Side 3/4 phai"), @("Up", $false, "Up sau lung (chu X)"), @("Side", $true, "Side trai (flipX)"))
for ($i = 0; $i -lt 4; $i++) { $bm = [VexEquip]::Preview("Idle", $row[$i][0], 0, 0); if ($row[$i][1]) { $bm = Mirror $bm }; Tile $bm (10 + $i * ($sz + 10)) 30 ("Idle " + $row[$i][2]) }
$y = 30 + $sz + 34
$g.DrawString("Walk F0 (ha nguoi 1 px, dao di theo)", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
for ($i = 0; $i -lt 4; $i++) { $bm = [VexEquip]::Preview("Walk", $row[$i][0], 0, 0); if ($row[$i][1]) { $bm = Mirror $bm }; Tile $bm (10 + $i * ($sz + 10)) $y ("Walk " + $row[$i][0]) }
$y += $sz + 34
$g.DrawString("Attack giu nguyen (dao trong tay): Down / Side / Up don 1 + don 4 chu X", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
$at = @(@("Down", 1, 0), @("Side", 1, 0), @("Up", 1, 0), @("Side", 1, 3))
for ($i = 0; $i -lt 4; $i++) { Tile ([VexEquip]::Preview("Attack", $at[$i][0], $at[$i][1], $at[$i][2])) (10 + $i * ($sz + 10)) $y ("Attack " + $at[$i][0] + " don " + ($at[$i][2] + 1)) }
$b.Save((Join-Path $O "VEX_BACK_DAGGERS_v1.png")); "ok"
