# Selene candidate board -> ..\selene\SELENE_CANDIDATE_v2.png
$D = Split-Path $PSScriptRoot -Parent
$O = Join-Path $D "selene"; New-Item -ItemType Directory -Force $O | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\VfxConceptGen.cs", "$D\tools\ArenKitSheets.cs", "$D\tools\SeleneGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$pal = [SeleneGen]::Pal
foreach ($v in @("Up", "Side")) { $rows = [SeleneGen]::Base($v); if ($rows.Length -ne 21) { throw "$v rows" }; for ($y = 0; $y -lt 21; $y++) { if ($rows[$y].Length -ne 14) { throw "$v row $y len $($rows[$y].Length)" }; foreach ($ch in $rows[$y].ToCharArray()) { if ($ch -ne '.' -and -not $pal.ContainsKey($ch)) { throw "$v row $y char $ch" } } } }

$grassC = [System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87); $grass = New-Object System.Drawing.SolidBrush $grassC
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$f = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fs = New-Object System.Drawing.Font "Consolas", 9
$K = 5; $sz = 48 * $K
$b = New-Object System.Drawing.Bitmap (4 * ($sz + 10) + 10), (2 * ($sz + 34) + 560); $g = [System.Drawing.Graphics]::FromImage($b); $g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
function Tile($bmp, $tx, $ty, $label) { $g.FillRectangle($grass, $tx, $ty, $sz, $sz); $g.FillEllipse($shadow, $tx + 16 * $K, $ty + 36 * $K, 16 * $K, 3 * $K); $g.DrawImage($bmp, (New-Object System.Drawing.Rectangle $tx, $ty, $sz, $sz)); $g.DrawString($label, $fs, [System.Drawing.Brushes]::White, $tx + 3, $ty + 3) }
$g.DrawString("Selene CANDIDATE v2 - gay phep ngan lai (toi vanh mu), dau gay moi: dai vang + ngoc tron | x5", $f, [System.Drawing.Brushes]::Yellow, 10, 6)
$turn = @(@("Down", $false, "Down (da duyet)"), @("Side", $false, "Side 3/4 phai (MOI)"), @("Up", $false, "Up sau lung (MOI)"), @("Side", $true, "Side trai (flipX)"))
for ($i = 0; $i -lt 4; $i++) { Tile ([SeleneGen]::Compose($turn[$i][0], [SeleneGen]::Rest($turn[$i][0]), $turn[$i][1])) (10 + $i * ($sz + 10)) 30 $turn[$i][2] }
$y = 30 + $sz + 34
$g.DrawString("Niem phep: F0 gio gay (pha le sang) / F1 nghieng gay ve muc tieu - cau phep bay ra tu pha le", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
$st = @(@("Side", 0, "Side F0 gio gay"), @("Side", 1, "Side F1 niem"), @("Down", 1, "Down F1 niem"), @("Up", 1, "Up F1 niem"))
for ($i = 0; $i -lt 4; $i++) {
    $p = [SeleneGen]::Cast($st[$i][0], $st[$i][1]); $bm = [SeleneGen]::Compose($st[$i][0], $p, $false)
    [SeleneGen]::ChargeGlow($bm, [SeleneGen]::Crystal($p))
    Tile $bm (10 + $i * ($sz + 10)) $y $st[$i][2]
}
$y += $sz + 34
$g.DrawString("Gay 8 huong (R UR U UL L DL D DR): hang tren dai (nghi/gio), hang duoi ngan (niem cheo) | x3", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
for ($r = 0; $r -lt 2; $r++) {
    for ($d = 0; $d -lt 8; $d++) {
        $s = [SeleneGen]::Staff($d, ($r -eq 1)); $cell = New-Object System.Drawing.Bitmap 48, 48
        foreach ($kv in $s.Px.GetEnumerator()) { $px = 24 + $kv.Key.X; $py = 24 + $kv.Key.Y; if ($px -ge 0 -and $py -ge 0 -and $px -lt 48 -and $py -lt 48) { $cell.SetPixel($px, $py, $kv.Value) } }
        $tx = 10 + $d * 120; $ty = $y + $r * 120
        $g.FillRectangle($grass, $tx, $ty, 112, 112); $g.DrawImage($cell, (New-Object System.Drawing.Rectangle ($tx - 16), ($ty - 16), 144, 144)); $cell.Dispose()
    }
}
$y += 2 * 120 + 30
$g.DrawString("Cau phep 16x16: thuong f0/f1 | lon (Cau Tinh Tu) f0/f1  | x5          VFX key frame: R3 cau phep + vet lap lanh + no sao 4 canh | x3", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
$tx = 10
foreach ($big in @($false, $true)) { for ($fr = 0; $fr -lt 2; $fr++) { $orbB = [SeleneGen]::Orb($big, $fr); $g.FillRectangle($grass, $tx, $y, 80, 80); $g.DrawImage($orbB, (New-Object System.Drawing.Rectangle $tx, $y, 80, 80)); $tx += 88 } }
$kt = [SeleneGen]::KeyTile()
$g.DrawImage($kt, (New-Object System.Drawing.Rectangle 380, $y, 384, 144))
$g.DrawImage($kt, (New-Object System.Drawing.Rectangle 790, ($y + 40), 128, 48))
$b.Save((Join-Path $O "SELENE_CANDIDATE_v2.png")); "ok"
