# Selene full art set: export (..\unity_export_selene\) + review boards (..\selene\) + pixel checks
$D = Split-Path $PSScriptRoot -Parent
$X = Join-Path $D "unity_export_selene"; $Out = Join-Path $D "selene"
New-Item -ItemType Directory -Force $X, $Out | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\FxGen.cs", "$D\tools\VfxConceptGen.cs", "$D\tools\ArenKitSheets.cs", "$D\tools\SeleneGen.cs", "$D\tools\SeleneKit.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
[SeleneKit]::Run($X)

$grassC = [System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87); $grass = New-Object System.Drawing.SolidBrush $grassC
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$font = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fontS = New-Object System.Drawing.Font "Consolas", 9
$bgC = [System.Drawing.Color]::FromArgb(255, 28, 28, 32)
function NewBoard($w, $h) { $bm = New-Object System.Drawing.Bitmap $w, $h; $gr = [System.Drawing.Graphics]::FromImage($bm); $gr.Clear($bgC); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'; return @($bm, $gr) }
function Img($gr, $file, $dx, $dy, $dw, $dh) { $im = [System.Drawing.Bitmap]::FromFile((Join-Path $X $file)); $gr.FillRectangle($grass, $dx, $dy, $dw, $dh); $gr.DrawImage($im, (New-Object System.Drawing.Rectangle $dx, $dy, $dw, $dh)); $im.Dispose() }
function Label($gr, $text, $lx, $ly) { $gr.DrawString($text, $font, [System.Drawing.Brushes]::Yellow, $lx, $ly) }
$views = @("Down", "Up", "Side")

# A) body sheet x4 + Hurt / Death
$bb = NewBoard 860 1240; $board = $bb[0]; $gr = $bb[1]
Label $gr "Selene_Sheet.png (x4): Idle D/U/S, Walk D/U/S, Attack D/U/S (gio gay, niem, thu) - Side quay trai" 10 6
Img $gr "Selene_Sheet.png" 10 30 512 1152
$gr.DrawString("Extra/ Hurt D/U/S (0,1)", $fontS, [System.Drawing.Brushes]::Yellow, 560, 30)
$ey = 50
foreach ($vw in $views) { for ($i = 0; $i -lt 2; $i++) { Img $gr "Extra\Selene_Hurt_${vw}_$i.png" (560 + $i * 136) $ey 128 128 }; $ey += 136 }
$gr.DrawString("Extra/ Death Down 0,1,2", $fontS, [System.Drawing.Brushes]::Yellow, 560, ($ey + 6)); $ey += 26
for ($i = 0; $i -lt 3; $i++) { Img $gr "Extra\Selene_Death_Down_$i.png" 560 $ey 128 128; $ey += 136 }
$board.Save((Join-Path $Out "Selene_body_sheet_review.png")); $gr.Dispose(); $board.Dispose()

# B) combo per view: Idle | Walk F0 | F0 gio (phat 1-2) | F1 niem phat 1 | F2 thu (cau bay) | F0 gio nap (phat 3) | F1 niem phat 3 | F2 phat 3
$K = 4; $sz = 48 * $K
$tiles = @(@("Idle", 0, 0, "Idle"), @("Walk", 0, 0, "Walk F0"), @("Attack", 0, 0, "F0 gio gay"), @("Attack", 1, 0, "F1 niem phat 1"), @("Attack", 2, 0, "F2 cau phep bay"), @("Attack", 0, 2, "F0 gio (phat 3)"), @("Attack", 1, 2, "F1 niem phat 3"), @("Attack", 2, 2, "F2 phat 3"))
$bb = NewBoard (8 * ($sz + 8) + 10) (3 * ($sz + 30) + 20); $board = $bb[0]; $gr = $bb[1]
for ($r = 0; $r -lt 3; $r++) {
    $vw = $views[$r]; $ty = 26 + $r * ($sz + 30)
    Label $gr "Selene $vw (Side = quay phai) - combo 3 phat: 2 cau phep + phat 3 no sao tai muc tieu; cau phep + lap lanh chi minh hoa (runtime la dan)" 10 ($ty - 22)
    for ($i = 0; $i -lt 8; $i++) {
        $t = $tiles[$i]; $tx = 10 + $i * ($sz + 8)
        $gr.FillRectangle($grass, $tx, $ty, $sz, $sz); $gr.FillEllipse($shadow, $tx + 16 * $K, $ty + 36 * $K, 16 * $K, 3 * $K)
        $gr.DrawImage([SeleneKit]::Preview($t[0], $vw, $t[1], $t[2], $true), (New-Object System.Drawing.Rectangle $tx, $ty, $sz, $sz))
        $gr.DrawString($t[3], $fontS, [System.Drawing.Brushes]::White, $tx + 3, $ty + 3)
    }
}
$board.Save((Join-Path $Out "Selene_combo_review.png")); $gr.Dispose(); $board.Dispose()

# C) skills + gear
$idleDown = [SeleneKit]::Preview("Idle", "Down", 0, 0, $false)
function Frames($gr, $file, $cw, $ch, $n, $fy, $k, $text, $hero, $px, $py, $behind) {
    if ($text) { Label $gr $text 10 ($fy - 22) }
    $sheet = [System.Drawing.Bitmap]::FromFile((Join-Path $X $file)); $cols = [int]($sheet.Width / $cw)
    for ($i = 0; $i -lt $n; $i++) {
        $tile = New-Object System.Drawing.Bitmap $cw, $ch; $tg = [System.Drawing.Graphics]::FromImage($tile); $tg.Clear($grassC); $tg.InterpolationMode = 'NearestNeighbor'; $tg.PixelOffsetMode = 'Half'
        $src = New-Object System.Drawing.Rectangle (($i % $cols) * $cw), ([math]::Floor($i / $cols) * $ch), $cw, $ch; $dst = New-Object System.Drawing.Rectangle 0, 0, $cw, $ch
        if ($behind) { $tg.DrawImage($sheet, $dst, $src, [System.Drawing.GraphicsUnit]::Pixel) }
        if ($hero) { $tg.DrawImage($hero, (New-Object System.Drawing.Rectangle ($px - 24), ($py - 38), 48, 48)) }
        if (-not $behind) { $tg.DrawImage($sheet, $dst, $src, [System.Drawing.GraphicsUnit]::Pixel) }
        $gr.DrawImage($tile, (New-Object System.Drawing.Rectangle (10 + $i * ($cw * $k + 8)), $fy, ($cw * $k), ($ch * $k))); $tg.Dispose(); $tile.Dispose()
    }
    $sheet.Dispose()
}
$bb = NewBoard 1500 2300; $board = $bb[0]; $gr = $bb[1]
$fy = 30
Frames $gr "Gear\Selene_StarBurst.png" 48 48 4 $fy 4 "R3 no sao - Selene_StarBurst.png 48x48 x4 (tai muc tieu, vong dat ban kinh 1.2 o) | OrbHit x3 (phat 1-2)" $null 0 0 $true
$fy += 48 * 4 + 40
Frames $gr "Gear\Selene_OrbHit.png" 32 32 3 $fy 4 "" $null 0 0 $true
$fy += 32 * 4 + 40
Frames $gr "Skills\Selene_Nova.png" 80 56 4 $fy 3 "2 Cau Tinh Tu - Selene_Nova.png 80x56 x4 (no, vong dat ban kinh 1.8 o)" $null 0 0 $true
$fy += 56 * 3 + 40
Frames $gr "Skills\Selene_Blink.png" 32 32 4 $fy 4 "3 Dich Chuyen - Selene_Blink.png 32x32 x4 (ghep len Selene: tu, cot sang, bui sao, tan)" $idleDown 16 30 $false
$fy += 32 * 4 + 40
Frames $gr "Skills\Selene_Rune.png" 80 40 5 $fy 3 "4 Vong Phu Chu - Selene_Rune.png 80x40 x5 (hien, xoay x3, tat) - mat dat, ban kinh 2.2 o" $null 0 0 $true
$fy += 40 * 3 + 40
Frames $gr "Skills\Selene_Bubble.png" 48 48 6 $fy 3 "V Khien Ma Luc - Selene_Bubble.png 48x48 x6 (hien, giu x2, do don, vo x2) ghep len Selene" $idleDown 24 40 $false
$fy += 48 * 3 + 40
Frames $gr "Skills\Selene_Meteor.png" 32 48 4 $fy 3 "C Mua Sao Bang - Selene_Meteor.png 32x48 x4 (roi x2, cham, tan) | Selene_MeteorZone.png 112x56 x2" $null 0 0 $true
$fy += 48 * 3 + 30
Frames $gr "Skills\Selene_MeteorZone.png" 112 56 2 $fy 3 "" $null 0 0 $true
$fy += 56 * 3 + 40
Label $gr "Gear: Selene_Staff.png (dai / ngan) x3 | Selene_Orb x4 | Selene_Sparkle x6 | Selene_Charge x4" 10 ($fy - 22)
Img $gr "Gear\Selene_Staff.png" 10 $fy 768 192
Img $gr "Gear\Selene_Orb.png" 800 $fy 256 64
Img $gr "Gear\Selene_Sparkle.png" 800 ($fy + 80) 144 48
Img $gr "Gear\Selene_Charge.png" 1080 $fy 192 192
$fy += 240
Label $gr "Selene_ComboVfx.png 18 o 48x48 (step*6 + view*2 + frame; Side quay trai) x2" 10 ($fy - 22)
Img $gr "Gear\Selene_ComboVfx.png" 10 $fy 1728 96
$board.Save((Join-Path $Out "Selene_skills_gear_review.png")); $gr.Dispose(); $board.Dispose()

# D) pixel checks: edge pixels per cell, semi-alpha, palette
$allowed = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($h in [SeleneGen]::Pal.Values) { [void]$allowed.Add($h.ToUpper()) }
foreach ($h in @("FFFFFF", "FFB8F0", "D860C8", "7A3AA8", "B8D8FF", "6A4428", "9A6A3C", "E8C048", "FFD0F0", "C04A8A", "7A2A5A", "1A1426", "FCDCC0", "E8B494")) { [void]$allowed.Add($h) }
$m = Import-Csv (Join-Path $X "SELENE_MANIFEST.csv")
foreach ($r in $m) {
    $bm = [System.Drawing.Bitmap]::FromFile((Join-Path $X $r.file)); $cw = [int]$r.cell_w; $ch = [int]$r.cell_h; $bad = 0; $semi = 0; $off = 0
    $cols = [int]($bm.Width / $cw); $rows = [int]($bm.Height / $ch)
    for ($cy = 0; $cy -lt $rows; $cy++) { for ($cx = 0; $cx -lt $cols; $cx++) {
        for ($i = 0; $i -lt $cw; $i++) { if ($bm.GetPixel($cx * $cw + $i, $cy * $ch).A -gt 0 -or $bm.GetPixel($cx * $cw + $i, $cy * $ch + $ch - 1).A -gt 0) { $bad++ } }
        for ($j = 0; $j -lt $ch; $j++) { if ($bm.GetPixel($cx * $cw, $cy * $ch + $j).A -gt 0 -or $bm.GetPixel($cx * $cw + $cw - 1, $cy * $ch + $j).A -gt 0) { $bad++ } } } }
    for ($yy = 0; $yy -lt $bm.Height; $yy++) { for ($xx = 0; $xx -lt $bm.Width; $xx++) { $c = $bm.GetPixel($xx, $yy); if ($c.A -gt 0 -and $c.A -lt 255) { $semi++ }; if ($c.A -eq 255 -and $r.file -notlike "Extra/Selene_Hurt*") { $hx = "{0:X2}{1:X2}{2:X2}" -f $c.R, $c.G, $c.B; if (-not $allowed.Contains($hx)) { $off++ } } } }
    "{0,-30} {1}x{2} grid {3}x{4} edgePixels={5} semiAlpha={6} offPalette={7}" -f $r.file, $cw, $ch, $cols, $rows, $bad, $semi, $off
    $bm.Dispose()
}
