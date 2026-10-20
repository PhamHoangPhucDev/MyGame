# Lyra candidate board -> ..\lyra\LYRA_CANDIDATE_v6.png
$D = Split-Path $PSScriptRoot -Parent
$O = Join-Path $D "lyra"; New-Item -ItemType Directory -Force $O | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\VfxConceptGen.cs", "$D\tools\ArenKitSheets.cs", "$D\tools\LyraGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$pal = [LyraGen]::Pal
foreach ($v in @("Up", "Side")) { $rows = [LyraGen]::Base($v); if ($rows.Length -ne 21) { throw "$v rows" }; for ($y = 0; $y -lt 21; $y++) { if ($rows[$y].Length -ne 14) { throw "$v row $y len $($rows[$y].Length)" }; foreach ($ch in $rows[$y].ToCharArray()) { if ($ch -ne '.' -and -not $pal.ContainsKey($ch)) { throw "$v row $y char $ch" } } } }

$grassC = [System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87); $grass = New-Object System.Drawing.SolidBrush $grassC
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$f = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fs = New-Object System.Drawing.Font "Consolas", 9
$K = 5; $sz = 48 * $K
$b = New-Object System.Drawing.Bitmap (4 * ($sz + 10) + 10), (2 * ($sz + 34) + 700); $g = [System.Drawing.Graphics]::FromImage($b); $g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
function Tile($bmp, $tx, $ty, $label) { $g.FillRectangle($grass, $tx, $ty, $sz, $sz); $g.FillEllipse($shadow, $tx + 16 * $K, $ty + 36 * $K, 16 * $K, 3 * $K); $g.DrawImage($bmp, (New-Object System.Drawing.Rectangle $tx, $ty, $sz, $sz)); $g.DrawString($label, $fs, [System.Drawing.Brushes]::White, $tx + 3, $ty + 3) }
$g.DrawString("Lyra CANDIDATE v6 - cung giua lung, Down thay dau cung nho sau vai phai; attack moi rut cung | x5", $f, [System.Drawing.Brushes]::Yellow, 10, 6)
$turn = @(@("Down", $false, "Down (da duyet)"), @("Side", $false, "Side 3/4 phai (MOI)"), @("Up", $false, "Up sau lung (MOI)"), @("Side", $true, "Side trai (flipX)"))
for ($i = 0; $i -lt 4; $i++) { Tile ([LyraGen]::Compose($turn[$i][0], [LyraGen]::Rest($turn[$i][0]), $turn[$i][1])) (10 + $i * ($sz + 10)) 30 $turn[$i][2] }
$y = 30 + $sz + 34
$g.DrawString("Ban cung: lap ten (F0) / keo day (F1, 2 tay) / buong (F2) - day cung keo that, khong xoay sprite", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
$st = @(@("Side", 0, "Side F0 lap ten"), @("Side", 1, "Side F1 keo day"), @("Down", 1, "Down F1 keo day"), @("Up", 1, "Up F1 keo day"))
for ($i = 0; $i -lt 4; $i++) { Tile ([LyraGen]::Compose($st[$i][0], [LyraGen]::Shot($st[$i][0], $st[$i][1]), $false)) (10 + $i * ($sz + 10)) $y $st[$i][2] }
$y += $sz + 34

# bow: 8 directions, rest + full draw (x4)
$g.DrawString("Cung 8 huong (R UR U UL L DL D DR): hang tren nghi, hang duoi keo day + ten | x4", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
for ($r = 0; $r -lt 2; $r++) {
    for ($d = 0; $d -lt 8; $d++) {
        $s = [LyraGen]::Bow($d, (4 * $r), ($r -eq 1)); $cell = New-Object System.Drawing.Bitmap 24, 24
        foreach ($kv in $s.Px.GetEnumerator()) { $cell.SetPixel(12 + $kv.Key.X, 12 + $kv.Key.Y, $kv.Value) }
        $tx = 10 + $d * 104; $ty = $y + $r * 104
        $g.FillRectangle($grass, $tx, $ty, 96, 96); $g.DrawImage($cell, (New-Object System.Drawing.Rectangle $tx, $ty, 96, 96)); $cell.Dispose()
    }
}
$y += 2 * 104 + 30

# arrows (x5)
$g.DrawString("Mui ten 16x16 (8 huong, ve san): thuong | nap (R3, 2 frame)  | x5", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
$tx = 10
foreach ($ch in @($false, $true)) {
    for ($d = 0; $d -lt 8; $d++) { $a = [LyraGen]::Arrow($d, $ch, 0); $g.FillRectangle($grass, $tx, $y, 80, 80); $g.DrawImage($a, (New-Object System.Drawing.Rectangle $tx, $y, 80, 80)); $tx += 84 }
    if ($ch) { $a = [LyraGen]::Arrow(0, $true, 1); $g.FillRectangle($grass, $tx, $y, 80, 80); $g.DrawImage($a, (New-Object System.Drawing.Rectangle $tx, $y, 80, 80)) }
    $tx = 10; $y += 88
}
$y += 30

$g.DrawString("VFX key frame: R3 ten nap xuyen (vet xanh + la bay + tia vang) | x4 va x1", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
$ct = [LyraGen]::ChargedTile()
$g.DrawImage($ct, (New-Object System.Drawing.Rectangle 10, $y, 448, 192))
$g.DrawImage($ct, (New-Object System.Drawing.Rectangle 480, ($y + 70), 112, 48))
$xx = 620
for ($i = 0; $i -lt 4; $i++) { $bm = [LyraGen]::Compose($turn[$i][0], [LyraGen]::Rest($turn[$i][0]), $turn[$i][1]); $g.FillRectangle($grass, $xx, ($y + 70), 52, 52); $g.DrawImage($bm, (New-Object System.Drawing.Rectangle ($xx + 2), ($y + 72), 48, 48)); $xx += 60 }
$b.Save((Join-Path $O "LYRA_CANDIDATE_v6.png")); "ok"
