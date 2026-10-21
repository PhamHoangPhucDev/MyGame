# Vex new skill VFX candidate (Shadow Step + Blade Dance) -> ..\vex\VEX_VFX_ADD_CANDIDATE_v2.png
$D = Split-Path $PSScriptRoot -Parent
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\FxGen.cs", "$D\tools\VfxConceptGen.cs", "$D\tools\VexGen.cs", "$D\tools\VexEquip.cs", "$D\tools\VexVfxGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$font = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fs = New-Object System.Drawing.Font "Consolas", 9
$board = New-Object System.Drawing.Bitmap 1240, 900; $g = [System.Drawing.Graphics]::FromImage($board); $g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$g.DrawString("Vex VFX bo sung v2 - 2 Buoc Bong (cot khoi muc boc len, vet do, mat do) | 4 Vu Dieu Luoi Dao (6 luoi liem do xoay quanh than) | x4", $font, [System.Drawing.Brushes]::Yellow, 10, 6)
$ss = [VexVfxGen]::ShadowStep(); $back = [VexVfxGen]::BladeDance(0); $front = [VexVfxGen]::BladeDance(1)
$vex = [VexEquip]::Preview("Idle", "Down", 0, 0)
# Row 1: Shadow Step frames alone (x4) + in context (x4): departure on Vex, arrival behind a target spot
$g.DrawString("2 Buoc Bong - o 32x32, 4 frame (diem di va diem den deu phat)", $font, [System.Drawing.Brushes]::Yellow, 10, 30)
for ($f = 0; $f -lt 4; $f++) {
    $x = 10 + $f * 136; $g.FillRectangle($grass, $x, 52, 128, 128)
    $g.DrawImage($ss, (New-Object System.Drawing.Rectangle $x, 52, 128, 128), (New-Object System.Drawing.Rectangle ($f * 32), 0, 32, 32), [System.Drawing.GraphicsUnit]::Pixel)
    $g.DrawString("f$f", $fs, [System.Drawing.Brushes]::White, $x + 3, 55)
}
for ($f = 0; $f -lt 2; $f++) {
    $x = 600 + $f * 200; $g.FillRectangle($grass, $x, 52, 192, 192)
    $g.FillEllipse($shadow, $x + 72, 52 + 144, 48, 12)
    if ($f -eq 1) { $g.DrawImage($vex, (New-Object System.Drawing.Rectangle $x, 52, 192, 192)) }
    $g.DrawImage($ss, (New-Object System.Drawing.Rectangle ($x + 32), (52 + 36), 128, 128), (New-Object System.Drawing.Rectangle ($f * 32), 0, 32, 32), [System.Drawing.GraphicsUnit]::Pixel)
    $g.DrawString($(if ($f -eq 0) { "diem di (Vex bien mat)" } else { "diem den (Vex hien ra)" }), $fs, [System.Drawing.Brushes]::White, $x + 3, 55)
}
# Row 2: Blade Dance 6 frames around Vex (back layer, body, front layer) x3
$g.DrawString("4 Vu Dieu Luoi Dao - o 64x48, 6 frame = 6 nhat; lop Back sau than, lop Front truoc than", $font, [System.Drawing.Brushes]::Yellow, 10, 260)
for ($f = 0; $f -lt 6; $f++) {
    $x = 10 + $f * 202; $y = 284
    $g.FillRectangle($grass, $x, $y, 192, 144); $g.FillEllipse($shadow, $x + 72, $y + 108, 48, 12)
    $src = New-Object System.Drawing.Rectangle ($f * 64), 0, 64, 48; $dst = New-Object System.Drawing.Rectangle $x, $y, 192, 144
    $g.DrawImage($back, $dst, $src, [System.Drawing.GraphicsUnit]::Pixel)
    $g.DrawImage($vex, (New-Object System.Drawing.Rectangle ($x + 24), $y, 144, 144))
    $g.DrawImage($front, $dst, $src, [System.Drawing.GraphicsUnit]::Pixel)
    $g.DrawString("nhat $($f + 1)", $fs, [System.Drawing.Brushes]::White, $x + 3, $y + 3)
}
# Row 3: layers alone (x3) so the split is visible
$g.DrawString("Tach lop (frame 1): Back | Front", $font, [System.Drawing.Brushes]::Yellow, 10, 446)
$g.FillRectangle($grass, 10, 470, 192, 144); $g.DrawImage($back, (New-Object System.Drawing.Rectangle 10, 470, 192, 144), (New-Object System.Drawing.Rectangle 0, 0, 64, 48), [System.Drawing.GraphicsUnit]::Pixel)
$g.FillRectangle($grass, 212, 470, 192, 144); $g.DrawImage($front, (New-Object System.Drawing.Rectangle 212, 470, 192, 144), (New-Object System.Drawing.Rectangle 0, 0, 64, 48), [System.Drawing.GraphicsUnit]::Pixel)
# existing Vex VFX for comparison (smoke bomb, execute X)
$g.DrawString("So sanh VFX Vex da co: Bom Khoi (Vex_Smoke) | Doat Menh (Vex_ExecuteX)", $font, [System.Drawing.Brushes]::Yellow, 440, 446)
$sm = [VexEquip]::Smoke(); $ex = [VexEquip]::ExecuteX()
for ($f = 0; $f -lt 2; $f++) { $g.FillRectangle($grass, 440 + $f * 136, 470, 128, 128); $g.DrawImage($sm, (New-Object System.Drawing.Rectangle (440 + $f * 136), 470, 128, 128), (New-Object System.Drawing.Rectangle ($f * 32), 0, 32, 32), [System.Drawing.GraphicsUnit]::Pixel) }
$g.FillRectangle($grass, 720, 470, 144, 144); $g.DrawImage($ex, (New-Object System.Drawing.Rectangle 720, 470, 144, 144), (New-Object System.Drawing.Rectangle 0, 0, 48, 48), [System.Drawing.GraphicsUnit]::Pixel)
$board.Save((Join-Path $D "vex\VEX_VFX_ADD_CANDIDATE_v2.png")); "ok"
