# Brom full art set: export (..\unity_export_brom\) + review boards (..\brom\) + pixel checks
$D = Split-Path $PSScriptRoot -Parent
$X = Join-Path $D "unity_export_brom"; $O = Join-Path $D "brom"
New-Item -ItemType Directory -Force $X, $O | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\FxGen.cs", "$D\tools\VfxConceptGen.cs", "$D\tools\ArenKitSheets.cs", "$D\tools\BromGen.cs", "$D\tools\BromKit.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
[BromKit]::Run($X)

$grassC = [System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87); $grass = New-Object System.Drawing.SolidBrush $grassC
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$f = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fs = New-Object System.Drawing.Font "Consolas", 9
$bg = [System.Drawing.Color]::FromArgb(255, 28, 28, 32)
function NewB($w, $h) { $b = New-Object System.Drawing.Bitmap $w, $h; $g = [System.Drawing.Graphics]::FromImage($b); $g.Clear($bg); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'; return @($b, $g) }
$views = @("Down", "Up", "Side")

# A) body sheet x4 (as exported: Side faces left)
$s = [System.Drawing.Bitmap]::FromFile((Join-Path $X "Brom_Sheet.png"))
$bb = NewB 860 1240; $b = $bb[0]; $g = $bb[1]
$g.DrawString("Brom_Sheet.png (x4): Idle D/U/S, Walk D/U/S, Attack D/U/S - Side quay trai", $f, [System.Drawing.Brushes]::Yellow, 10, 6)
$g.FillRectangle($grass, 10, 30, 512, 1152); $g.DrawImage($s, (New-Object System.Drawing.Rectangle 10, 30, 512, 1152))
$g.DrawString("Extra/ Hurt D/U/S (0,1)", $fs, [System.Drawing.Brushes]::Yellow, 560, 30)
$ey = 50
foreach ($v in $views) { for ($i = 0; $i -lt 2; $i++) { $e = [System.Drawing.Bitmap]::FromFile((Join-Path $X "Extra\Brom_Hurt_${v}_$i.png")); $g.FillRectangle($grass, (560 + $i * 136), $ey, 128, 128); $g.DrawImage($e, (New-Object System.Drawing.Rectangle (560 + $i * 136), $ey, 128, 128)); $e.Dispose() }; $ey += 136 }
$g.DrawString("Extra/ Death Down 0,1,2", $fs, [System.Drawing.Brushes]::Yellow, 560, ($ey + 6)); $ey += 26
for ($i = 0; $i -lt 3; $i++) { $e = [System.Drawing.Bitmap]::FromFile((Join-Path $X "Extra\Brom_Death_Down_$i.png")); $g.FillRectangle($grass, 560, $ey, 128, 128); $g.DrawImage($e, (New-Object System.Drawing.Rectangle 560, $ey, 128, 128)); $e.Dispose(); $ey += 136 }
$b.Save((Join-Path $O "Brom_body_sheet_review.png")); $s.Dispose()

# B) combo per view (right-facing preview): Idle | windup | hit 1 | hit 2 | hit 3 (ket) | recover
$K = 4; $sz = 48 * $K
$bb = NewB (6 * ($sz + 8) + 10) (3 * ($sz + 30) + 20); $b = $bb[0]; $g = $bb[1]
for ($r = 0; $r -lt 3; $r++) {
    $v = $views[$r]; $y = 26 + $r * ($sz + 30)
    $g.DrawString("Brom $v (Side = quay phai) - Idle | windup | don 1 vung ngang | don 2 bo xuong | don 3 nen dat (ket) | recover", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 22)
    $tiles = @(@("Idle", 0, 0, "Idle"), @("Attack", 0, 0, "F0 windup"), @("Attack", 1, 0, "F1 hit 1"), @("Attack", 1, 1, "F1 hit 2"), @("Attack", 1, 2, "F1 hit 3"), @("Attack", 2, 2, "F2 recover"))
    for ($i = 0; $i -lt 6; $i++) {
        $t = $tiles[$i]; $tx = 10 + $i * ($sz + 8)
        $g.FillRectangle($grass, $tx, $y, $sz, $sz); $g.FillEllipse($shadow, $tx + 16 * $K, $y + 36 * $K, 16 * $K, 3 * $K)
        $g.DrawImage([BromKit]::Preview($t[0], $v, $t[1], $t[2]), (New-Object System.Drawing.Rectangle $tx, $y, $sz, $sz))
        $g.DrawString($t[3], $fs, [System.Drawing.Brushes]::White, $tx + 3, $y + 3)
    }
}
$b.Save((Join-Path $O "Brom_combo_review.png"))

# C) skills: frames composited on the Down idle body at the sheet pivot (ground/back layers behind the body)
$idleDown = [BromKit]::Preview("Idle", "Down", 0, 0)
function Strip($g, $file, $cw, $ch, $n, $px, $py, $behind, $front2, $y, $k, $label) {
    $g.DrawString($label, $f, [System.Drawing.Brushes]::Yellow, 10, $y - 22)
    $sheet = [System.Drawing.Bitmap]::FromFile((Join-Path $X $file)); $sheet2 = $null; if ($front2) { $sheet2 = [System.Drawing.Bitmap]::FromFile((Join-Path $X $front2)) }
    $cols = [int]($sheet.Width / $cw)
    for ($i = 0; $i -lt $n; $i++) {
        $tile = New-Object System.Drawing.Bitmap $cw, $ch; $tg = [System.Drawing.Graphics]::FromImage($tile); $tg.Clear($grassC); $tg.InterpolationMode = 'NearestNeighbor'; $tg.PixelOffsetMode = 'Half'
        $src = New-Object System.Drawing.Rectangle (($i % $cols) * $cw), ([math]::Floor($i / $cols) * $ch), $cw, $ch; $dst = New-Object System.Drawing.Rectangle 0, 0, $cw, $ch
        if ($behind) { $tg.DrawImage($sheet, $dst, $src, [System.Drawing.GraphicsUnit]::Pixel) }
        $tg.DrawImage($idleDown, (New-Object System.Drawing.Rectangle ($px - 24), ($py - 38), 48, 48))
        if (-not $behind) { $tg.DrawImage($sheet, $dst, $src, [System.Drawing.GraphicsUnit]::Pixel) }
        if ($sheet2) { $tg.DrawImage($sheet2, $dst, $src, [System.Drawing.GraphicsUnit]::Pixel) }
        $g.DrawImage($tile, (New-Object System.Drawing.Rectangle (10 + $i * ($cw * $k + 8)), $y, ($cw * $k), ($ch * $k))); $tile.Dispose()
    }
}
$bb = NewB 1760 1720; $b = $bb[0]; $g = $bb[1]
Strip $g "Skills\Brom_GroundSlam.png" 80 56 4 40 36 $true $null 30 3 "2 Nen Dat - Brom_GroundSlam.png 80x56 x4 (sau than)"
Strip $g "Skills\Brom_Roar.png" 64 64 4 32 48 $false $null 250 3 "4 Gam Thach - Brom_Roar.png 64x64 x4 (truoc than)"
Strip $g "Skills\Brom_IronWall_Back.png" 48 48 6 24 40 $true "Skills\Brom_IronWall_Front.png" 470 3 "V Tuong Sat - Back + Front 48x48 x6 (0-1 hien, 2-3 giu, 4-5 vo)"
Strip $g "Skills\Brom_Quake.png" 112 64 6 56 40 $true $null 640 2 "C Dong Dat - Brom_Quake.png 112x64: dot 1 manh/nhat, dot 2, dot 3"
$y = 830; $g.DrawString("3 Huc Khien: Brom_ShieldGlow 24x24 x3 | Brom_ChargeDust 16x16 x3 | 4: Brom_TauntMark 16x16 x2 (x6)", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 22)
$xx = 10
foreach ($it in @(@("Skills\Brom_ShieldGlow.png", 24, 3), @("Skills\Brom_ChargeDust.png", 16, 3), @("Skills\Brom_TauntMark.png", 16, 2))) {
    $sheet = [System.Drawing.Bitmap]::FromFile((Join-Path $X $it[0]))
    for ($i = 0; $i -lt $it[2]; $i++) { $sz2 = $it[1] * 6; $g.FillRectangle($grass, $xx, $y, $sz2, $sz2); $g.DrawImage($sheet, (New-Object System.Drawing.Rectangle $xx, $y, $sz2, $sz2), (New-Object System.Drawing.Rectangle ($i * $it[1]), 0, $it[1], $it[1]), [System.Drawing.GraphicsUnit]::Pixel); $xx += $sz2 + 8 }
    $xx += 24
}
$y = 1000; $g.DrawString("Gear: Brom_Hammer.png 8 tu the (x4) | Brom_Shield.png Down/Up/Side (x6)", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 22)
$hm = [System.Drawing.Bitmap]::FromFile((Join-Path $X "Gear\Brom_Hammer.png")); $g.FillRectangle($grass, 10, $y, 1536, 192); $g.DrawImage($hm, (New-Object System.Drawing.Rectangle 10, $y, 1536, 192))
$shd = [System.Drawing.Bitmap]::FromFile((Join-Path $X "Gear\Brom_Shield.png")); $g.FillRectangle($grass, 10, ($y + 210), 288, 96); $g.DrawImage($shd, (New-Object System.Drawing.Rectangle 10, ($y + 210), 288, 96))
$cb = [System.Drawing.Bitmap]::FromFile((Join-Path $X "Gear\Brom_ComboVfx.png")); $y = 1350
$g.DrawString("Brom_ComboVfx.png 18 o 48x48 (step*6 + view*2 + frame; Side quay trai) x2", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 22)
$g.FillRectangle($grass, 10, $y, 1728, 96); $g.DrawImage($cb, (New-Object System.Drawing.Rectangle 10, $y, 1728, 96))
$b.Save((Join-Path $O "Brom_skills_gear_review.png"))

# D) pixel checks over every exported PNG with a grid
$m = Import-Csv (Join-Path $X "BROM_MANIFEST.csv")
foreach ($r in $m) {
    $bmp = [System.Drawing.Bitmap]::FromFile((Join-Path $X $r.file)); $cw = [int]$r.cell_w; $ch = [int]$r.cell_h; $bad = 0; $semi = 0
    $cols = [int]($bmp.Width / $cw); $rows = [int]($bmp.Height / $ch)
    for ($cy = 0; $cy -lt $rows; $cy++) { for ($cx = 0; $cx -lt $cols; $cx++) {
        for ($i = 0; $i -lt $cw; $i++) { if ($bmp.GetPixel($cx * $cw + $i, $cy * $ch).A -gt 0 -or $bmp.GetPixel($cx * $cw + $i, $cy * $ch + $ch - 1).A -gt 0) { $bad++ } }
        for ($j = 0; $j -lt $ch; $j++) { if ($bmp.GetPixel($cx * $cw, $cy * $ch + $j).A -gt 0 -or $bmp.GetPixel($cx * $cw + $cw - 1, $cy * $ch + $j).A -gt 0) { $bad++ } } } }
    for ($yy = 0; $yy -lt $bmp.Height; $yy++) { for ($xx = 0; $xx -lt $bmp.Width; $xx++) { $a = $bmp.GetPixel($xx, $yy).A; if ($a -gt 0 -and $a -lt 255) { $semi++ } } }
    "{0,-34} {1}x{2} grid {3}x{4} edgePixels={5} semiAlpha={6}" -f $r.file, $cw, $ch, $cols, $rows, $bad, $semi
    $bmp.Dispose()
}
