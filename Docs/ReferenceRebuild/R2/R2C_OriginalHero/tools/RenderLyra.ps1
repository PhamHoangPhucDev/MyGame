# Lyra full art set: export (..\unity_export_lyra\) + review boards (..\lyra\) + pixel checks
$D = Split-Path $PSScriptRoot -Parent
$X = Join-Path $D "unity_export_lyra"; $O = Join-Path $D "lyra"
New-Item -ItemType Directory -Force $X, $O | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\FxGen.cs", "$D\tools\VfxConceptGen.cs", "$D\tools\ArenKitSheets.cs", "$D\tools\LyraGen.cs", "$D\tools\LyraKit.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
[LyraKit]::Run($X)

$grassC = [System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87); $grass = New-Object System.Drawing.SolidBrush $grassC
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$f = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fs = New-Object System.Drawing.Font "Consolas", 9
$bg = [System.Drawing.Color]::FromArgb(255, 28, 28, 32)
function NewB($w, $h) { $b = New-Object System.Drawing.Bitmap $w, $h; $g = [System.Drawing.Graphics]::FromImage($b); $g.Clear($bg); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'; return @($b, $g) }
function Img($g, $file, $dx, $dy, $dw, $dh) { $i = [System.Drawing.Bitmap]::FromFile((Join-Path $X $file)); $g.FillRectangle($grass, $dx, $dy, $dw, $dh); $g.DrawImage($i, (New-Object System.Drawing.Rectangle $dx, $dy, $dw, $dh)); $i.Dispose() }
$views = @("Down", "Up", "Side")

# A) body sheet x4 + Hurt / Death
$bb = NewB 860 1240; $b = $bb[0]; $g = $bb[1]
$g.DrawString("Lyra_Sheet.png (x4): Idle D/U/S, Walk D/U/S, Attack D/U/S (lap ten, keo day, buong) - Side quay trai", $f, [System.Drawing.Brushes]::Yellow, 10, 6)
Img $g "Lyra_Sheet.png" 10 30 512 1152
$g.DrawString("Extra/ Hurt D/U/S (0,1)", $fs, [System.Drawing.Brushes]::Yellow, 560, 30)
$ey = 50
foreach ($v in $views) { for ($i = 0; $i -lt 2; $i++) { Img $g "Extra\Lyra_Hurt_${v}_$i.png" (560 + $i * 136) $ey 128 128 }; $ey += 136 }
$g.DrawString("Extra/ Death Down 0,1,2", $fs, [System.Drawing.Brushes]::Yellow, 560, ($ey + 6)); $ey += 26
for ($i = 0; $i -lt 3; $i++) { Img $g "Extra\Lyra_Death_Down_$i.png" 560 $ey 128 128; $ey += 136 }
$b.Save((Join-Path $O "Lyra_body_sheet_review.png"))

# B) combo per view (right-facing preview, gear + VFX): Idle | Walk F0 | nock | draw | release 1 | release 2 | draw 3 (nap) | release 3
$K = 4; $sz = 48 * $K
$tiles = @(@("Idle", 0, 0, "Idle (cung sau lung)"), @("Walk", 0, 0, "Walk F0"), @("Attack", 0, 0, "F0 lap ten"), @("Attack", 1, 0, "F1 keo (phat 1-2)"), @("Attack", 2, 0, "F2 buong phat 1"), @("Attack", 2, 1, "F2 buong phat 2"), @("Attack", 1, 2, "F1 nap (phat 3)"), @("Attack", 2, 2, "F2 ten nap xuyen"))
$bb = NewB (8 * ($sz + 8) + 10) (3 * ($sz + 30) + 20); $b = $bb[0]; $g = $bb[1]
for ($r = 0; $r -lt 3; $r++) {
    $v = $views[$r]; $y = 26 + $r * ($sz + 30)
    $g.DrawString("Lyra $v (Side = quay phai) - combo 3 phat: 2 phat nhanh + phat nap xuyen; mui ten + vet bay chi minh hoa (runtime la dan)", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 22)
    for ($i = 0; $i -lt 8; $i++) {
        $t = $tiles[$i]; $tx = 10 + $i * ($sz + 8)
        $g.FillRectangle($grass, $tx, $y, $sz, $sz); $g.FillEllipse($shadow, $tx + 16 * $K, $y + 36 * $K, 16 * $K, 3 * $K)
        $g.DrawImage([LyraKit]::Preview($t[0], $v, $t[1], $t[2], $true), (New-Object System.Drawing.Rectangle $tx, $y, $sz, $sz))
        $g.DrawString($t[3], $fs, [System.Drawing.Brushes]::White, $tx + 3, $y + 3)
    }
}
$b.Save((Join-Path $O "Lyra_combo_review.png"))

# C) skills + gear
$idleDown = [LyraKit]::Preview("Idle", "Down", 0, 0, $false)
$sideShot = [LyraKit]::Preview("Attack", "Side", 2, 0, $false)
function Frames($g, $file, $cw, $ch, $n, $y, $k, $label, $hero, $px, $py, $behind, $row) {
    $g.DrawString($label, $f, [System.Drawing.Brushes]::Yellow, 10, $y - 22)
    $sheet = [System.Drawing.Bitmap]::FromFile((Join-Path $X $file)); $cols = [int]($sheet.Width / $cw)
    for ($i = 0; $i -lt $n; $i++) {
        $idx = $row * $cols + $i
        $tile = New-Object System.Drawing.Bitmap $cw, $ch; $tg = [System.Drawing.Graphics]::FromImage($tile); $tg.Clear($grassC); $tg.InterpolationMode = 'NearestNeighbor'; $tg.PixelOffsetMode = 'Half'
        $src = New-Object System.Drawing.Rectangle (($idx % $cols) * $cw), ([math]::Floor($idx / $cols) * $ch), $cw, $ch; $dst = New-Object System.Drawing.Rectangle 0, 0, $cw, $ch
        if ($behind) { $tg.DrawImage($sheet, $dst, $src, [System.Drawing.GraphicsUnit]::Pixel) }
        if ($hero) { $tg.DrawImage($hero, (New-Object System.Drawing.Rectangle ($px - 24), ($py - 38), 48, 48)) }
        if (-not $behind) { $tg.DrawImage($sheet, $dst, $src, [System.Drawing.GraphicsUnit]::Pixel) }
        $g.DrawImage($tile, (New-Object System.Drawing.Rectangle (10 + $i * ($cw * $k + 8)), $y, ($cw * $k), ($ch * $k))); $tile.Dispose()
    }
    $sheet.Dispose()
}
$bb = NewB 1800 2800; $b = $bb[0]; $g = $bb[1]
$y = 30
for ($r = 0; $r -lt 3; $r++) {
    $v = $views[$r]; $hero = [LyraKit]::Preview("Attack", $v, 2, 0, $false); if ($v -eq "Side") { $hero.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX) }
    Frames $g "Skills\Lyra_FanBurst.png" 64 64 3 ($y) 3 "2 Ten Quat - Lyra_FanBurst.png 64x64, hang $v (ghep len Lyra buong day; Side quay trai nhu file) - 5 mui ten la dan runtime" $hero 32 42 $false $r
    $y += 64 * 3 + 30
}
# FanBurst row 3 (Side) is stored mirrored; the board shows it as exported (faces left) over a right-facing hero only for size reference
Frames $g "Skills\Lyra_LeafBurst.png" 32 32 3 $y 4 "3 Nhay Lui - Lyra_LeafBurst.png 32x32 x3 (ghep len Lyra)" $idleDown 16 30 $false 0
Frames $g "Skills\Lyra_VineSnare.png" 64 40 5 ($y + 170) 3 "3 Nhay Lui - Lyra_VineSnare.png 64x40 x5 (mam, lon, giu, giu, heo) - mat dat" $null 0 0 $true 0
$y += 170 + 150
Frames $g "Skills\Lyra_RainZone.png" 80 48 2 $y 3 "4 Mua Ten - Lyra_RainZone.png 80x48 x2 (vung ban kinh 2 o) | Lyra_RainArrow.png 16x32 x3 (roi, cham, tan)" $null 0 0 $true 0
$y += 48 * 3 + 30
Frames $g "Skills\Lyra_RainArrow.png" 16 32 3 $y 4 "" $null 0 0 $true 0
$y += 32 * 4 + 40
Frames $g "Skills\Lyra_WindAura.png" 32 32 4 $y 4 "V Buoc Gio - Lyra_WindAura.png 32x32 x4 lap (ghep len Lyra) | Lyra_DodgePuff.png x3 khi ne don" $idleDown 16 30 $false 0
$y += 32 * 4 + 40
Frames $g "Skills\Lyra_DodgePuff.png" 32 32 3 $y 4 "" $idleDown 16 30 $false 0
$y += 32 * 4 + 40
# beam assembled: Lyra side release + Start + Mid x3 + End (x3), and the vertical row
$g.DrawString("C Ten Xuyen Rung - Lyra_Beam.png: Start + Mid x3 (lat) + End, frame 0 / 1 / 2 (x3); hang doc ben phai", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 22)
$beam = [System.Drawing.Bitmap]::FromFile((Join-Path $X "Skills\Lyra_Beam.png"))
for ($fr = 0; $fr -lt 3; $fr++) {
    $strip = New-Object System.Drawing.Bitmap 208, 48; $sg = [System.Drawing.Graphics]::FromImage($strip); $sg.Clear($grassC); $sg.InterpolationMode = 'NearestNeighbor'; $sg.PixelOffsetMode = 'Half'
    $sg.DrawImage($sideShot, 0, 0, 48, 48)
    $bx = 8 + 25 + 5 - 8   # muzzle x in the hero tile minus the Start burst centre (8)
    $by = 8 + 21 - 16
    $parts = @(0, 1, 1, 1, 2)
    for ($p = 0; $p -lt 5; $p++) { $col = $parts[$p] * 3 + $fr; $sg.DrawImage($beam, (New-Object System.Drawing.Rectangle ($bx + $p * 32), $by, 32, 32), (New-Object System.Drawing.Rectangle ($col * 32), 0, 32, 32), [System.Drawing.GraphicsUnit]::Pixel) }
    $g.DrawImage($strip, (New-Object System.Drawing.Rectangle 10, ($y + $fr * 152), 624, 144)); $strip.Dispose()
}
$vx = 680
for ($fr = 0; $fr -lt 3; $fr++) {
    $strip = New-Object System.Drawing.Bitmap 32, 160; $sg = [System.Drawing.Graphics]::FromImage($strip); $sg.Clear($grassC)
    $parts = @(0, 1, 1, 1, 2)
    for ($p = 0; $p -lt 5; $p++) { $col = $parts[$p] * 3 + $fr; $sg.DrawImage($beam, (New-Object System.Drawing.Rectangle 0, ($p * 32), 32, 32), (New-Object System.Drawing.Rectangle ($col * 32), 32, 32, 32), [System.Drawing.GraphicsUnit]::Pixel) }
    $g.DrawImage($strip, (New-Object System.Drawing.Rectangle ($vx + $fr * 110), $y, 96, 480)); $strip.Dispose()
}
$beam.Dispose()
$y += 3 * 152 + 40
$g.DrawString("Gear: Lyra_Bow.png (hang nghi / lap ten / keo day + o 24 dau cung sau lung) x3 | Lyra_Arrow.png x4 | Lyra_ArrowTrail.png x2", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 22)
Img $g "Gear\Lyra_Bow.png" 10 $y 768 384
Img $g "Gear\Lyra_Arrow.png" 800 $y 512 192
Img $g "Gear\Lyra_ArrowTrail.png" 800 ($y + 200) 512 256
$y += 470
$g.DrawString("Lyra_ComboVfx.png 18 o 48x48 (step*6 + view*2 + frame; Side quay trai) x2", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 22)
Img $g "Gear\Lyra_ComboVfx.png" 10 $y 1728 96
$b.Save((Join-Path $O "Lyra_skills_gear_review.png"))

# D) pixel checks: edge pixels per cell, semi-alpha, palette (body colours + FX + gear colours)
$allowed = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($h in [LyraGen]::Pal.Values) { [void]$allowed.Add($h.ToUpper()) }
foreach ($h in @("F8FFE0", "B8F078", "6AC040", "2E7A2E", "F0D060", "8A5A34", "B07A48", "5E8A34", "EEE8DC", "2A2218", "D8DCE4", "FCDCC0", "E8B494", "8AB04A", "7A6A50", "9A8A60", "FFFFFF")) { [void]$allowed.Add($h) }
$m = Import-Csv (Join-Path $X "LYRA_MANIFEST.csv")
foreach ($r in $m) {
    $bmp = [System.Drawing.Bitmap]::FromFile((Join-Path $X $r.file)); $cw = [int]$r.cell_w; $ch = [int]$r.cell_h; $bad = 0; $semi = 0; $off = 0
    $cols = [int]($bmp.Width / $cw); $rows = [int]($bmp.Height / $ch)
    for ($cy = 0; $cy -lt $rows; $cy++) { for ($cx = 0; $cx -lt $cols; $cx++) {
        for ($i = 0; $i -lt $cw; $i++) { if ($bmp.GetPixel($cx * $cw + $i, $cy * $ch).A -gt 0 -or $bmp.GetPixel($cx * $cw + $i, $cy * $ch + $ch - 1).A -gt 0) { $bad++ } }
        for ($j = 0; $j -lt $ch; $j++) { if ($bmp.GetPixel($cx * $cw, $cy * $ch + $j).A -gt 0 -or $bmp.GetPixel($cx * $cw + $cw - 1, $cy * $ch + $j).A -gt 0) { $bad++ } } } }
    for ($yy = 0; $yy -lt $bmp.Height; $yy++) { for ($xx = 0; $xx -lt $bmp.Width; $xx++) { $c = $bmp.GetPixel($xx, $yy); if ($c.A -gt 0 -and $c.A -lt 255) { $semi++ }; if ($c.A -eq 255 -and $r.file -notlike "Extra/Lyra_Hurt*") { $hx = "{0:X2}{1:X2}{2:X2}" -f $c.R, $c.G, $c.B; if (-not $allowed.Contains($hx)) { $off++ } } } }
    "{0,-30} {1}x{2} grid {3}x{4} edgePixels={5} semiAlpha={6} offPalette={7}" -f $r.file, $cw, $ch, $cols, $rows, $bad, $semi, $off
    $bmp.Dispose()
}
