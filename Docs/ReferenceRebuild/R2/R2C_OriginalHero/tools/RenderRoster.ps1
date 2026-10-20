# Regenerates the roster PNGs and R2C_roster_board.png (+ version comparison for the redrawn Vex).
$D = Split-Path $PSScriptRoot -Parent
Add-Type -Path "$D\tools\HeroConceptGen.cs" -ReferencedAssemblies System.Drawing
Add-Type -AssemblyName System.Drawing
[HeroConceptGen]::Run("$D\roster", 3)

$labels = @("Nguoi lun - Tank", "Elf - Cung thu", "Phu thuy - Phap su", "Nhan toc - Kiem si", "Ma toc - Sat thu")
$f = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold)
$grassC = [System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87)
$grass = New-Object System.Drawing.SolidBrush $grassC
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(90, 0, 0, 0))
$R = Join-Path $D "roster"
function Img($c) { [System.Drawing.Bitmap]::FromFile((Join-Path $R "HeroConcept_$($c.Id)_$($c.Name)_IdleDown_F0_roster.png")) }
function NewBoard($w, $h) { $b = New-Object System.Drawing.Bitmap $w, $h; $g = [System.Drawing.Graphics]::FromImage($b); $g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'; return @($b, $g) }
function Tile($g, $img, $x, $y, $label) { $g.FillRectangle($grass, $x, $y, 256, 256); $g.FillEllipse($shadow, $x + 80, $y + 224, 96, 24); $g.DrawImage($img, (New-Object System.Drawing.Rectangle $x, $y, 256, 256)); $g.DrawString($label, $f, [System.Drawing.Brushes]::White, $x + 4, $y + 4) }

# Full roster board
$bg = NewBoard 1350 700; $b = $bg[0]; $g = $bg[1]
$g.DrawString("R2C roster - Idle Down F0 (x8, cell 32x32, sole row 29)", $f, [System.Drawing.Brushes]::Yellow, 10, 6)
$i = 0
foreach ($c in [HeroConceptGen]::Roster) { $x = 10 + $i * 266; Tile $g (Img $c) $x 30 $c.Name; $g.DrawString($labels[$i], $f, [System.Drawing.Brushes]::White, $x, 290); $i++ }
$y = 340; $g.DrawString("Party at Portrait PPC x4 (phone size) and 1x", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24); $g.FillRectangle($grass, 10, $y, 1330, 340)
$i = 0
foreach ($c in [HeroConceptGen]::Roster) { $img = Img $c; $g.FillEllipse($shadow, (60 + $i * 150) + 40, ($y + 40) + 112, 48, 12); $g.DrawImage($img, (New-Object System.Drawing.Rectangle (60 + $i * 150), ($y + 40), 128, 128)); $g.DrawImage($img, (New-Object System.Drawing.Rectangle (900 + $i * 36), ($y + 120), 32, 32)); $i++ }
$b.Save((Join-Path $R "R2C_roster_board.png"))

# Vex versions: v1..v5 (current)
$bg = NewBoard 1350 330; $b = $bg[0]; $g = $bg[1]
$g.DrawString("Vex redraw: v1 -> v2 -> v3 -> v4 -> v5 (current)", $f, [System.Drawing.Brushes]::Yellow, 10, 6)
$files = @("_roster_v1", "_roster_v2", "_roster_v3", "_roster_v4", "_roster")
$names = @("v1", "v2", "v3", "v4", "v5")
for ($i = 0; $i -lt 5; $i++) { Tile $g ([System.Drawing.Bitmap]::FromFile((Join-Path $R "HeroConcept_5_Vex_IdleDown_F0$($files[$i]).png"))) (10 + $i * 266) 30 "Vex $($names[$i])" }
$b.Save((Join-Path $R "R2C_redraw_vex.png"))

# Aren versions: v1..v3 (current)
$bg = NewBoard 818 330; $b = $bg[0]; $g = $bg[1]
$g.DrawString("Aren redraw: v1 -> v2 -> v3 (current)", $f, [System.Drawing.Brushes]::Yellow, 10, 6)
$files = @("_roster_v1", "_roster_v2", "_roster")
$names = @("v1", "v2", "v3")
for ($i = 0; $i -lt 3; $i++) { Tile $g ([System.Drawing.Bitmap]::FromFile((Join-Path $R "HeroConcept_4_Aren_IdleDown_F0$($files[$i]).png"))) (10 + $i * 266) 30 "Aren $($names[$i])" }
$b.Save((Join-Path $R "R2C_redraw_aren.png"))

# Lyra versions: v1..v3 (current)
$bg = NewBoard 818 330; $b = $bg[0]; $g = $bg[1]
$g.DrawString("Lyra redraw: v1 -> v2 -> v3 (current)", $f, [System.Drawing.Brushes]::Yellow, 10, 6)
$files = @("_roster_v1", "_roster_v2", "_roster")
$names = @("v1", "v2", "v3")
for ($i = 0; $i -lt 3; $i++) { Tile $g ([System.Drawing.Bitmap]::FromFile((Join-Path $R "HeroConcept_2_Lyra_IdleDown_F0$($files[$i]).png"))) (10 + $i * 266) 30 "Lyra $($names[$i])" }
$b.Save((Join-Path $R "R2C_redraw_lyra.png"))
