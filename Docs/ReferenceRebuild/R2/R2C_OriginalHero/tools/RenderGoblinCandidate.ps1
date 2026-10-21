# Goblin candidate board -> ..\monsters\GOBLIN_CANDIDATE_v3.png
$D = Split-Path $PSScriptRoot -Parent
$Out = Join-Path $D "monsters"; New-Item -ItemType Directory -Force $Out | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\MonsterGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$pal = [MonsterGen]::GoblinPal
foreach ($v in @("Down", "Up", "Side")) { $rows = [MonsterGen]::GoblinBase($v); if ($rows.Length -ne 21) { throw "$v rows" }; for ($y = 0; $y -lt 21; $y++) { if ($rows[$y].Length -ne 14) { throw "$v row $y len $($rows[$y].Length)" }; foreach ($ch in $rows[$y].ToCharArray()) { if ($ch -ne '.' -and -not $pal.ContainsKey($ch)) { throw "$v row $y char $ch" } } } }
$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$font = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fontS = New-Object System.Drawing.Font "Consolas", 9
$K = 5; $sz = 48 * $K
$board = New-Object System.Drawing.Bitmap (4 * ($sz + 10) + 10), (2 * ($sz + 34) + 300); $gr = [System.Drawing.Graphics]::FromImage($board); $gr.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'
function Tile($bmp, $tx, $ty, $label) { $gr.FillRectangle($grass, $tx, $ty, $sz, $sz); $gr.FillEllipse($shadow, $tx + 17 * $K, $ty + 36 * $K, 14 * $K, 3 * $K); $gr.DrawImage($bmp, (New-Object System.Drawing.Rectangle $tx, $ty, $sz, $sz)); $gr.DrawString($label, $fontS, [System.Drawing.Brushes]::White, $tx + 3, $ty + 3) }
$gr.DrawString("Goblin CANDIDATE v3 - bam video: vien mu trum mong, mat to sang, tai ngang, mat vang-cam | x5 | than 12 px (hero 21 px)", $font, [System.Drawing.Brushes]::Yellow, 10, 6)
$t1 = @(@("Idle", "Down", 0, $false, "Down"), @("Idle", "Side", 0, $false, "Side 3/4 phai"), @("Idle", "Up", 0, $false, "Up sau lung"), @("Idle", "Side", 0, $true, "Side trai (flipX)"))
for ($i = 0; $i -lt 4; $i++) { Tile ([MonsterGen]::Goblin($t1[$i][0], $t1[$i][1], $t1[$i][2], $t1[$i][3])) (10 + $i * ($sz + 10)) 30 $t1[$i][4] }
$y = 30 + $sz + 34
$gr.DrawString("Di chuyen + danh: Walk F0 / Attack vung dao (F0) / dam (F1)", $font, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
$t2 = @(@("Walk", "Side", 0, "Walk Side F0"), @("Attack", "Side", 0, "Attack Side F0 vung"), @("Attack", "Side", 1, "Attack Side F1 dam"), @("Attack", "Down", 1, "Attack Down F1 dam"))
for ($i = 0; $i -lt 4; $i++) { Tile ([MonsterGen]::Goblin($t2[$i][0], $t2[$i][1], $t2[$i][2], $false)) (10 + $i * ($sz + 10)) $y $t2[$i][3] }
$y += $sz + 34
$gr.DrawString("So ti le voi Aren (x3) va kich thuoc that (x1)", $font, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
$aren = [EquipGen]::ArenMoveComposite("Idle", "Down", 0, [EquipGen]::SteelLongsword, [Func[string, EquipGen+Spr]] { param($v) [EquipGen]::HeaterShield($v) }, $null)
$gob = [MonsterGen]::Goblin("Idle", "Down", 0, $false)
$gr.FillRectangle($grass, 10, $y, 300, 160)
$gr.DrawImage($aren, (New-Object System.Drawing.Rectangle 10, ($y + 10), 144, 144)); $gr.DrawImage($gob, (New-Object System.Drawing.Rectangle 150, ($y + 10), 144, 144))
$gr.FillRectangle($grass, 330, $y, 120, 70)
$gr.DrawImage($aren, 336, ($y + 10), 48, 48); $gr.DrawImage($gob, 384, ($y + 10), 48, 48)
$board.Save((Join-Path $Out "GOBLIN_CANDIDATE_v3.png")); "ok"
# reference crop from the video (frame_22_8.0) for side-by-side comparison
$refPath = "C:\Users\PC-119\AppData\Local\Temp\claude\D--My-Doc-You-Game01-My-project\e5078040-e9f0-4f5a-9e07-4c7a67115704\scratchpad\goblin_ref\goblins_bottom.png"
if (Test-Path $refPath) {
    $b2 = [System.Drawing.Bitmap]::FromFile((Join-Path $Out "GOBLIN_CANDIDATE_v3.png")); $g2 = [System.Drawing.Graphics]::FromImage($b2); $g2.InterpolationMode = 'NearestNeighbor'
    $ref = [System.Drawing.Bitmap]::FromFile($refPath)
    $g2.DrawString("Tham khao video (frame_22_8.0, phong to):", (New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold)), [System.Drawing.Brushes]::Yellow, 480, ($b2.Height - 290))
    $g2.DrawImage($ref, (New-Object System.Drawing.Rectangle 480, ($b2.Height - 264), 440, 200))
    $ref.Dispose(); $g2.Dispose()
    $tmp = Join-Path $Out "GOBLIN_CANDIDATE_v3_tmp.png"; $b2.Save($tmp); $b2.Dispose(); Move-Item -Force $tmp (Join-Path $Out "GOBLIN_CANDIDATE_v3.png")
}
"ok2"
