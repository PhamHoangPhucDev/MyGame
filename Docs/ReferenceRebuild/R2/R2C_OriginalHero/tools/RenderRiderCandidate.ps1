# Gobrider / Elite / Warlord candidate board -> ..\monsters\RIDERS_CANDIDATE_v7.png
$D = Split-Path $PSScriptRoot -Parent
$Out = Join-Path $D "monsters"; New-Item -ItemType Directory -Force $Out | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\MonsterGen.cs", "$D\tools\RiderGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
# grid sanity: equal row widths, known palette chars
foreach ($g in [RiderGen]::AllGrids()) { $w = $g[0].Length; for ($i = 0; $i -lt $g.Length; $i++) { if ($g[$i].Length -ne $w) { throw "row $i width $($g[$i].Length) != $w in grid starting '$($g[0])'" }; foreach ($ch in $g[$i].ToCharArray()) { if ($ch -ne '.' -and -not [RiderGen]::Pal.ContainsKey($ch)) { throw "char $ch" } } } }
$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$font = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fontS = New-Object System.Drawing.Font "Consolas", 9
$K = 4
$board = New-Object System.Drawing.Bitmap 1120, 1300; $gr = [System.Drawing.Graphics]::FromImage($board); $gr.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'
$gr.DrawString("Riders CANDIDATE v7 - heo rung den, nanh nga, mat do; Elite giu mat na sat | x4", $font, [System.Drawing.Brushes]::Yellow, 10, 6)
$views = @(@("Down", $false, "Down"), @("Side", $false, "Side 3/4 phai"), @("Up", $false, "Up sau lung"), @("Side", $true, "Side trai (flipX)"))
$rows = @(@("Gobrider", 48, "Gobrider (o 48) - mu trum chop cao, cuoi heo rung den"), @("Elite", 48, "Elite Gobrider (o 48) - mu do den vien vang, heo rung den deo mat na sat, chuy xuong"), @("Warlord", 64, "Gobrider Warlord - boss (o 64) - mu dau lau sung, ao choang do, heo chien, chuy xuong lon"))
$y = 34
foreach ($r in $rows) {
    $S = $r[1]; $sz = $S * $K
    $gr.DrawString($r[2], $font, [System.Drawing.Brushes]::Yellow, 10, $y)
    $y += 22
    for ($i = 0; $i -lt 4; $i++) {
        $tx = 10 + $i * ($sz + 8)
        $gr.FillRectangle($grass, $tx, $y, $sz, $sz)
        $sw = [int]($S * 0.45 * $K); $gr.FillEllipse($shadow, ($tx + $sz / 2 - $sw / 2), ($y + ($S - 7) * $K), $sw, 4 * $K)
        $gr.DrawImage([RiderGen]::Compose($r[0], $views[$i][0], $views[$i][1]), (New-Object System.Drawing.Rectangle $tx, $y, $sz, $sz))
        $gr.DrawString($views[$i][2], $fontS, [System.Drawing.Brushes]::White, $tx + 3, $y + 3)
    }
    $y += $sz + 14
}
# size line-up at x2: Aren, Goblin, Gobrider, Elite, Warlord (shared ground line)
$gr.DrawString("So ti le (x2, cung mat dat): Aren | Goblin | Gobrider | Elite | Warlord", $font, [System.Drawing.Brushes]::Yellow, 10, $y)
$y += 22; $gr.FillRectangle($grass, 10, $y, 560, 150); $ground = $y + 130
$aren = [EquipGen]::ArenMoveComposite("Idle", "Side", 0, [EquipGen]::SteelLongsword, [Func[string, EquipGen+Spr]] { param($v) [EquipGen]::HeaterShield($v) }, $null)
$items = @(@($aren, 37), @([MonsterGen]::Goblin("Idle", "Side", 0, $false), 37), @([RiderGen]::Compose("Gobrider", "Side", $false), 41), @([RiderGen]::Compose("Elite", "Side", $false), 41), @([RiderGen]::Compose("Warlord", "Side", $false), 57))
$x = 20
foreach ($it in $items) { $bm = $it[0]; $gr.DrawImage($bm, (New-Object System.Drawing.Rectangle $x, ($ground - $it[1] * 2), ($bm.Width * 2), ($bm.Height * 2))); $x += $bm.Width * 2 - 10 }
# reference crop
$refPath = "C:\Users\PC-119\AppData\Local\Temp\claude\D--My-Doc-You-Game01-My-project\e5078040-e9f0-4f5a-9e07-4c7a67115704\scratchpad\goblin_ref\gobriders_right.png"
if (Test-Path $refPath) { $ref = [System.Drawing.Bitmap]::FromFile($refPath); $gr.DrawString("Tham khao video (Gobrider):", $font, [System.Drawing.Brushes]::Yellow, 600, ($y - 22)); $gr.DrawImage($ref, (New-Object System.Drawing.Rectangle 600, $y, 315, 510)); $ref.Dispose() }
$board.Save((Join-Path $Out "RIDERS_CANDIDATE_v7.png")); "ok"
