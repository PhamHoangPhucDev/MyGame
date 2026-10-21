# Goblin Archer candidate board -> ..\monsters\ARCHER_CANDIDATE_v2.png
$D = Split-Path $PSScriptRoot -Parent
$Out = Join-Path $D "monsters"; New-Item -ItemType Directory -Force $Out | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\MonsterGen.cs", "$D\tools\ArcherGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
foreach ($g in [ArcherGen]::AllGrids()) { $w = $g[0].Length; for ($i = 0; $i -lt $g.Length; $i++) { if ($g[$i].Length -ne $w) { throw "row $i width $($g[$i].Length) != $w in grid starting '$($g[0])'" }; foreach ($ch in $g[$i].ToCharArray()) { if ($ch -ne '.' -and -not [ArcherGen]::Pal.ContainsKey($ch)) { throw "char $ch" } } } }
$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$font = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fontS = New-Object System.Drawing.Font "Consolas", 9
$K = 4; $S = 48; $sz = $S * $K
$board = New-Object System.Drawing.Bitmap 1120, 1060; $gr = [System.Drawing.Graphics]::FromImage($board); $gr.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'
$gr.DrawString("Goblin Archer CANDIDATE v2 - to hon goblin thuong (16 px), tai vinh cao, cung ngang hong khi ngam | x4", $font, [System.Drawing.Brushes]::Yellow, 10, 6)
$views = @(@("Down", $false, "Down"), @("Side", $false, "Side phai"), @("Up", $false, "Up sau lung"), @("Side", $true, "Side trai (flipX)"))
$poses = @(@(0, "Dung yen (cam cung)"), @(1, "Ngam ban (keo day)"))
$y = 34
foreach ($p in $poses) {
    $gr.DrawString($p[1], $font, [System.Drawing.Brushes]::Yellow, 10, $y); $y += 22
    for ($i = 0; $i -lt 4; $i++) {
        $tx = 10 + $i * ($sz + 8)
        $gr.FillRectangle($grass, $tx, $y, $sz, $sz)
        $sw = [int]($S * 0.32 * $K); $gr.FillEllipse($shadow, ($tx + $sz / 2 - $sw / 2), ($y + 36 * $K), $sw, 3 * $K)
        $gr.DrawImage([ArcherGen]::Compose($views[$i][0], $p[0], $views[$i][1], 0), (New-Object System.Drawing.Rectangle $tx, $y, $sz, $sz))
        $gr.DrawString($views[$i][2], $fontS, [System.Drawing.Brushes]::White, $tx + 3, $y + 3)
    }
    $y += $sz + 14
}
# arrow 8 directions (x4)
$gr.DrawString("Mui ten (o 16, 8 huong R UR U UL L DL D DR) | So ti le x2: Aren | Goblin | Archer", $font, [System.Drawing.Brushes]::Yellow, 10, $y); $y += 22
$gr.FillRectangle($grass, 10, $y, 8 * 72, 72)
for ($d = 0; $d -lt 8; $d++) { $gr.DrawImage([ArcherGen]::Arrow($d), (New-Object System.Drawing.Rectangle (10 + $d * 72 + 4), ($y + 4), 64, 64)) }
$gr.FillRectangle($grass, 600, $y, 300, 120); $ground = $y + 104
$aren = [EquipGen]::ArenMoveComposite("Idle", "Side", 0, [EquipGen]::SteelLongsword, [Func[string, EquipGen+Spr]] { param($v) [EquipGen]::HeaterShield($v) }, $null)
$items = @(@($aren, 37), @([MonsterGen]::Goblin("Idle", "Side", 0, $false), 37), @([ArcherGen]::Compose("Side", 1, $false, 0), 37))
$x = 610; foreach ($it in $items) { $bm = $it[0]; $gr.DrawImage($bm, (New-Object System.Drawing.Rectangle $x, ($ground - $it[1] * 2), ($bm.Width * 2), ($bm.Height * 2))); $x += 80 }
# reference crop
$refPath = "C:\Users\PC-119\AppData\Local\Temp\claude\D--My-Doc-You-Game01-My-project\e5078040-e9f0-4f5a-9e07-4c7a67115704\scratchpad\goblin_ref\archer_crop.png"
if (Test-Path $refPath) { $ref = [System.Drawing.Bitmap]::FromFile($refPath); $gr.DrawString("Tham khao video (Gob Archer):", $font, [System.Drawing.Brushes]::Yellow, 10, ($y + 136)); $gr.DrawImage($ref, (New-Object System.Drawing.Rectangle 10, ($y + 158), 300, 300)); $ref.Dispose() }
$board.Save((Join-Path $Out "ARCHER_CANDIDATE_v2.png")); "ok"
