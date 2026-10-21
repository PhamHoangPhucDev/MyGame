# Cyclops boss + AoE telegraph candidate board -> ..\monsters\BOSS_CANDIDATE_v4.png
$D = Split-Path $PSScriptRoot -Parent
$Out = Join-Path $D "monsters"; New-Item -ItemType Directory -Force $Out | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\RiderGen.cs", "$D\tools\BossGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
foreach ($g in [BossGen]::AllGrids()) { $w = $g[0].Length; for ($i = 0; $i -lt $g.Length; $i++) { if ($g[$i].Length -ne $w) { throw "row $i width $($g[$i].Length) != $w in grid starting '$($g[0])'" }; foreach ($ch in $g[$i].ToCharArray()) { if ($ch -ne '.' -and -not [BossGen]::Pal.ContainsKey($ch)) { throw "char $ch" } } } }
$dirt = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0xE2, 0xAE, 0x81))
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$font = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fontS = New-Object System.Drawing.Font "Consolas", 9
$K = 3; $S = 64; $sz = $S * $K
$board = New-Object System.Drawing.Bitmap 1280, 1440; $gr = [System.Drawing.Graphics]::FromImage($board); $gr.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'
$gr.DrawString("Boss Cyclops CANDIDATE v4 - map va luc luong hon; 2 bieu cam: hien (chua bi danh) / hung du (bi danh) | x3", $font, [System.Drawing.Brushes]::Yellow, 10, 6)
$views = @(@("Down", $false, "Down"), @("Side", $false, "Side phai"), @("Up", $false, "Up"), @("Side", $true, "Side trai"))
$poses = @(@(0, "Dung yen - HIEN (chua bi danh): mi mat sup, mieng khep", $false), @(0, "Dung yen - HUNG DU (da bi danh): may chau chu V, dong tu do, ha mieng nhe rang", $true), @(1, "Lay da (gio chuy) - hung du", $true), @(2, "Nen dat (chuy dap xuong) - hung du", $true))
$y = 30
foreach ($p in $poses) {
    $gr.DrawString($p[1], $font, [System.Drawing.Brushes]::Yellow, 10, $y); $y += 20
    for ($i = 0; $i -lt 4; $i++) {
        $tx = 10 + $i * ($sz + 6)
        $gr.FillRectangle($dirt, $tx, $y, $sz, $sz)
        $sw = [int]($S * 0.42 * $K); $gr.FillEllipse($shadow, ($tx + $sz / 2 - $sw / 2), ($y + 55 * $K), $sw, 4 * $K)
        $gr.DrawImage([BossGen]::Cyclops($views[$i][0], $p[0], $views[$i][1], 0, $p[2]), (New-Object System.Drawing.Rectangle $tx, $y, $sz, $sz))
        $gr.DrawString($views[$i][2], $fontS, [System.Drawing.Brushes]::Black, $tx + 3, $y + 3)
    }
    $y += $sz + 10
}
# telegraph frames x2 + crack decal x3
$gr.DrawString("Vong canh bao AoE (o 96, ban kinh 46 px): 0-3 lan dan vao tam, 4 loe luc dap, 5 mo dan  |  Vet nut dat (64x32)", $font, [System.Drawing.Brushes]::Yellow, 10, $y); $y += 20
for ($f = 0; $f -lt 6; $f++) { $tx = 10 + $f * 200; $gr.FillRectangle($dirt, $tx, $y, 192, 192); $gr.DrawImage([BossGen]::Telegraph($f), (New-Object System.Drawing.Rectangle $tx, $y, 192, 192)); $gr.DrawString("$f", $fontS, [System.Drawing.Brushes]::Black, $tx + 3, $y + 3) }
$y += 200
$gr.FillRectangle($dirt, 10, $y, 192, 96); $gr.DrawImage([BossGen]::Crack(), (New-Object System.Drawing.Rectangle 10, $y, 192, 96))
# size line-up x2 on dirt + reference
$gr.DrawString("So ti le x2: Aren | Gobrider Warlord | Cyclops", $font, [System.Drawing.Brushes]::Yellow, 230, ($y - 2))
$gr.FillRectangle($dirt, 230, ($y + 18), 420, 150); $ground = $y + 18 + 134
$aren = [EquipGen]::ArenMoveComposite("Idle", "Side", 0, [EquipGen]::SteelLongsword, [Func[string, EquipGen+Spr]] { param($v) [EquipGen]::HeaterShield($v) }, $null)
$items = @(@($aren, 37, 48), @([RiderGen]::Compose("Warlord", "Side", $false), 57, 64), @([BossGen]::Cyclops("Side", 0, $false, 0), 57, 64))
$x = 240; foreach ($it in $items) { $gr.DrawImage($it[0], (New-Object System.Drawing.Rectangle $x, ($ground - $it[1] * 2), ($it[2] * 2), ($it[2] * 2))); $x += $it[2] * 2 - 10 }
$p = "C:\Users\PC-119\AppData\Local\Temp\claude\D--My-Doc-You-Game01-My-project\e5078040-e9f0-4f5a-9e07-4c7a67115704\scratchpad\goblin_ref\cyclops_crop.png"
if (Test-Path $p) { $ref = [System.Drawing.Bitmap]::FromFile($p); $gr.DrawString("Tham khao video (Cyclops):", $font, [System.Drawing.Brushes]::Yellow, 820, 30); $gr.DrawImage($ref, (New-Object System.Drawing.Rectangle 820, 52, 300, 300)); $ref.Dispose() }
$board.Save((Join-Path $Out "BOSS_CANDIDATE_v4.png")); "ok"
