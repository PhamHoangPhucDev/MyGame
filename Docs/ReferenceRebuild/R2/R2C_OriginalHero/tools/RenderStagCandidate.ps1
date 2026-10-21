# Dryad rider (centaur on a red deer body) candidate board -> ..\monsters\DRYAD_RIDER_CANDIDATE_v1.png
$D = Split-Path $PSScriptRoot -Parent
$Out = Join-Path $D "monsters"; New-Item -ItemType Directory -Force $Out | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\MonsterGen.cs", "$D\tools\RiderGen.cs", "$D\tools\CreatureGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
foreach ($g in [CreatureGen]::AllGrids()) { $w = $g[0].Length; for ($i = 0; $i -lt $g.Length; $i++) { if ($g[$i].Length -ne $w) { throw "row $i width $($g[$i].Length) != $w in grid starting '$($g[0])'" }; foreach ($ch in $g[$i].ToCharArray()) { if ($ch -ne '.' -and -not [CreatureGen]::Pal.ContainsKey($ch)) { throw "char $ch" } } } }
$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$font = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fontS = New-Object System.Drawing.Font "Consolas", 9
$K = 4; $S = 48; $sz = $S * $K
$board = New-Object System.Drawing.Bitmap 1120, 1100; $gr = [System.Drawing.Graphics]::FromImage($board); $gr.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'
$gr.DrawString("Dryad cuoi nai CANDIDATE v1 - nhu video: than Dryad lien voi minh nai do (kieu nhan ma), 4 chan manh, duoi tron | x4", $font, [System.Drawing.Brushes]::Yellow, 10, 6)
$views = @(@("Down", $false, "Down"), @("Side", $false, "Side phai"), @("Up", $false, "Up sau lung"), @("Side", $true, "Side trai (flipX)"))
$rows = @(@(0, -1, "Dung yen"), @(2, -1, "Tu phep (qua cau la, ban dan la nhu Dryad)"), @(0, 0, "Buoc di (nhac chan xen ke)"))
$y = 30
foreach ($r in $rows) {
    $gr.DrawString($r[2], $font, [System.Drawing.Brushes]::Yellow, 10, $y); $y += 20
    for ($i = 0; $i -lt 4; $i++) {
        $tx = 10 + $i * ($sz + 8)
        $gr.FillRectangle($grass, $tx, $y, $sz, $sz)
        $sw = [int]($S * 0.45 * $K); $gr.FillEllipse($shadow, ($tx + $sz / 2 - $sw / 2), ($y + 40 * $K), $sw, 3 * $K)
        $gr.DrawImage([CreatureGen]::Stag($views[$i][0], $r[0], $views[$i][1], $r[1], 0), (New-Object System.Drawing.Rectangle $tx, $y, $sz, $sz))
        $gr.DrawString($views[$i][2], $fontS, [System.Drawing.Brushes]::White, $tx + 3, $y + 3)
    }
    $y += $sz + 10
}
$gr.DrawString("So ti le x2: Aren | Dryad (di bo) | Gobrider | Dryad cuoi nai", $font, [System.Drawing.Brushes]::Yellow, 10, $y); $y += 20
$gr.FillRectangle($grass, 10, $y, 520, 130); $ground = $y + 112
$aren = [EquipGen]::ArenMoveComposite("Idle", "Side", 0, [EquipGen]::SteelLongsword, [Func[string, EquipGen+Spr]] { param($v) [EquipGen]::HeaterShield($v) }, $null)
$items = @(@($aren, 37), @([CreatureGen]::Compose("Dryad", "Side", 0, $false, 0), 37), @([RiderGen]::Compose("Gobrider", "Side", $false), 41), @([CreatureGen]::Stag("Side", 0, $false, -1, 0), 41))
$x = 20; foreach ($it in $items) { $bm = $it[0]; $gr.DrawImage($bm, (New-Object System.Drawing.Rectangle $x, ($ground - $it[1] * 2), 96, 96)); $x += 110 }
$p = "C:\Users\PC-119\AppData\Local\Temp\claude\D--My-Doc-You-Game01-My-project\e5078040-e9f0-4f5a-9e07-4c7a67115704\scratchpad\goblin_ref\deer_crop.png"
if (Test-Path $p) { $ref = [System.Drawing.Bitmap]::FromFile($p); $gr.DrawString("Tham khao video (Dryad):", $font, [System.Drawing.Brushes]::Yellow, 560, ($y - 20)); $gr.DrawImage($ref, (New-Object System.Drawing.Rectangle 560, $y, 540, 180)); $ref.Dispose() }
$board.Save((Join-Path $Out "DRYAD_RIDER_CANDIDATE_v1.png")); "ok"
