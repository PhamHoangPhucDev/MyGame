# Satyr + Dryad candidate board -> ..\monsters\FOREST_CANDIDATE_v1.png
$D = Split-Path $PSScriptRoot -Parent
$Out = Join-Path $D "monsters"; New-Item -ItemType Directory -Force $Out | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\MonsterGen.cs", "$D\tools\ArcherGen.cs", "$D\tools\CreatureGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
foreach ($g in [CreatureGen]::AllGrids()) { $w = $g[0].Length; for ($i = 0; $i -lt $g.Length; $i++) { if ($g[$i].Length -ne $w) { throw "row $i width $($g[$i].Length) != $w in grid starting '$($g[0])'" }; foreach ($ch in $g[$i].ToCharArray()) { if ($ch -ne '.' -and -not [CreatureGen]::Pal.ContainsKey($ch)) { throw "char $ch" } } } }
$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$font = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fontS = New-Object System.Drawing.Font "Consolas", 9
$K = 3; $S = 48; $sz = $S * $K
$board = New-Object System.Drawing.Bitmap 1280, 1100; $gr = [System.Drawing.Graphics]::FromImage($board); $gr.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'
$gr.DrawString("Forest CANDIDATE v1 - Satyr (nguoi de, sung chu Y, huc dau) + Dryad (mu la, mat trang, ban la phep) | x3", $font, [System.Drawing.Brushes]::Yellow, 10, 6)
$views = @(@("Down", $false, "Down"), @("Side", $false, "Side phai"), @("Up", $false, "Up"), @("Side", $true, "Side trai"))
$rows = @(@("Satyr", 0, "Satyr - dung yen"), @("Satyr", 2, "Satyr - huc dau (lao toi 2 px)"), @("Dryad", 0, "Dryad - dung yen"), @("Dryad", 2, "Dryad - tu phep (qua cau la)"))
$y = 30
foreach ($r in $rows) {
    $gr.DrawString($r[2], $font, [System.Drawing.Brushes]::Yellow, 10, $y); $y += 20
    for ($i = 0; $i -lt 4; $i++) {
        $tx = 10 + $i * ($sz + 6)
        $gr.FillRectangle($grass, $tx, $y, $sz, $sz)
        $sw = [int]($S * 0.32 * $K); $gr.FillEllipse($shadow, ($tx + $sz / 2 - $sw / 2), ($y + 36 * $K), $sw, 3 * $K)
        $bm = [CreatureGen]::Compose($r[0], $views[$i][0], $r[1], $views[$i][1], 0)
        $dx = 0; $dy = 0
        if ($r[0] -eq "Satyr" -and $r[1] -eq 2) { if ($views[$i][0] -eq "Down") { $dy = 2 } elseif ($views[$i][0] -eq "Up") { $dy = -2 } elseif ($views[$i][1]) { $dx = -2 } else { $dx = 2 } }
        $gr.DrawImage($bm, (New-Object System.Drawing.Rectangle ($tx + $dx * $K), ($y + $dy * $K), $sz, $sz))
        $gr.DrawString($views[$i][2], $fontS, [System.Drawing.Brushes]::White, $tx + 3, $y + 3)
    }
    $y += $sz + 10
}
# right column: leaf bolt, size line-up, references
$cx = 640
$gr.DrawString("Dan la cua Dryad (o 16, 8 huong R UR U UL L DL D DR)", $font, [System.Drawing.Brushes]::Yellow, $cx, 30)
$gr.FillRectangle($grass, $cx, 52, 8 * 70, 70)
for ($d = 0; $d -lt 8; $d++) { $gr.DrawImage([CreatureGen]::LeafBolt($d), (New-Object System.Drawing.Rectangle ($cx + $d * 70 + 3), 55, 64, 64)) }
$gr.DrawString("So ti le x2: Aren | Goblin | Archer | Satyr | Dryad", $font, [System.Drawing.Brushes]::Yellow, $cx, 140)
$gr.FillRectangle($grass, $cx, 162, 560, 120); $ground = 162 + 104
$aren = [EquipGen]::ArenMoveComposite("Idle", "Side", 0, [EquipGen]::SteelLongsword, [Func[string, EquipGen+Spr]] { param($v) [EquipGen]::HeaterShield($v) }, $null)
$items = @($aren, [MonsterGen]::Goblin("Idle", "Side", 0, $false), [ArcherGen]::Compose("Side", 0, $false, 0), [CreatureGen]::Compose("Satyr", "Side", 0, $false, 0), [CreatureGen]::Compose("Dryad", "Side", 0, $false, 0))
$x = $cx + 10; foreach ($bm in $items) { $gr.DrawImage($bm, (New-Object System.Drawing.Rectangle $x, ($ground - 37 * 2), 96, 96)); $x += 100 }
$refDir = "C:\Users\PC-119\AppData\Local\Temp\claude\D--My-Doc-You-Game01-My-project\e5078040-e9f0-4f5a-9e07-4c7a67115704\scratchpad\goblin_ref"
$gr.DrawString("Tham khao video: Satyr | Dryad", $font, [System.Drawing.Brushes]::Yellow, $cx, 300)
$i = 0; foreach ($f in @("satyr_crop.png", "dryad_crop.png")) { $p = Join-Path $refDir $f; if (Test-Path $p) { $ref = [System.Drawing.Bitmap]::FromFile($p); $gr.DrawImage($ref, (New-Object System.Drawing.Rectangle ($cx + $i * 290), 322, 280, 280)); $ref.Dispose() }; $i++ }
$board.Save((Join-Path $Out "FOREST_CANDIDATE_v1.png")); "ok"
