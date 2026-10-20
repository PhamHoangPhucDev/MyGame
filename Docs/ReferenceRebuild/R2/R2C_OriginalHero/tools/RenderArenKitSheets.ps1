# Aren designed-kit VFX sheets -> ..\aren_kit\sheets\ + preview board
$D = Split-Path $PSScriptRoot -Parent
$O = Join-Path $D "aren_kit\sheets"; New-Item -ItemType Directory -Force $O | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\FxGen.cs", "$D\tools\VfxConceptGen.cs", "$D\tools\ArenKitSheets.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
[ArenKitSheets]::Run($O)

$heater = [Func[string, EquipGen+Spr]] { param($v) [EquipGen]::HeaterShield($v) }
function Body($kind, $v) {
    if ($kind -eq "atk") { return [EquipGen]::ArenAttackComposite($v, 1, [EquipGen]::SteelLongsword, $heater, $null) }
    return [EquipGen]::ArenMoveComposite("Idle", $v, 0, [EquipGen]::SteelLongsword, $heater, $null)
}
$grassC = [System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87)
$f = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold)
$fs = New-Object System.Drawing.Font "Consolas", 9

# draw one cell (sheet frame) composited with the Aren body at the sheet pivot; Side body mirrored to face left
function Cell($g, $sheet, $col, $row, $cw, $ch, $px, $py, $bodyBmp, $mirror, $behind, $x, $y, $k) {
    $tile = New-Object System.Drawing.Bitmap $cw, $ch; $tg = [System.Drawing.Graphics]::FromImage($tile); $tg.Clear($grassC); $tg.InterpolationMode = 'NearestNeighbor'; $tg.PixelOffsetMode = 'Half'
    $src = New-Object System.Drawing.Rectangle ($col * $cw), ($row * $ch), $cw, $ch
    $dst = New-Object System.Drawing.Rectangle 0, 0, $cw, $ch
    if ($behind) { $tg.DrawImage($sheet, $dst, $src, [System.Drawing.GraphicsUnit]::Pixel) }
    if ($bodyBmp) {
        $bx = $px - 16 - 8; $by = $py - 30 - 8
        if ($mirror) { $tg.DrawImage($bodyBmp, (New-Object System.Drawing.Rectangle ($bx + 48), $by, -48, 48), (New-Object System.Drawing.Rectangle 0, 0, 48, 48), [System.Drawing.GraphicsUnit]::Pixel) }
        else { $tg.DrawImage($bodyBmp, (New-Object System.Drawing.Rectangle $bx, $by, 48, 48), (New-Object System.Drawing.Rectangle 0, 0, 48, 48), [System.Drawing.GraphicsUnit]::Pixel) }
    }
    if (-not $behind) { $tg.DrawImage($sheet, $dst, $src, [System.Drawing.GraphicsUnit]::Pixel) }
    $g.DrawImage($tile, (New-Object System.Drawing.Rectangle $x, $y, ($cw * $k), ($ch * $k)))
    $tile.Dispose()
}

$views = @("Down", "Up", "Side")
$b = New-Object System.Drawing.Bitmap 1900, 2350; $g = [System.Drawing.Graphics]::FromImage($b); $g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$y = 10
# view-based sheets: Thrust 80x80, ShieldBash 64x64, Dawn 96x96 (3 views x 3 frames); Up row drawn behind the body
$vs = @(@("Aren_Thrust.png", 80, 80, 40, 50, 2, "R3 Dam thang (ket)"), @("Aren_ShieldBash.png", 64, 64, 32, 40, 2, "2 Khien Dap"), @("Aren_Dawn.png", 96, 96, 48, 64, 2, "C Chem Binh Minh"))
$x0 = 10
foreach ($s in $vs) {
    $sheet = [System.Drawing.Bitmap]::FromFile((Join-Path $O $s[0])); $cw = $s[1]; $ch = $s[2]; $k = $s[5]
    $g.DrawString("$($s[6]) - $($s[0]) ($cw x $ch, pivot $($s[3]),$($s[4])) - hang: Down / Up (sau than) / Side (quay trai)", $f, [System.Drawing.Brushes]::Yellow, 10, $y)
    $y += 22
    for ($r = 0; $r -lt 3; $r++) {
        $v = $views[$r]; $bodyBmp = Body "atk" $v
        for ($c = 0; $c -lt 3; $c++) { Cell $g $sheet $c $r $cw $ch $s[3] $s[4] $bodyBmp ($v -eq "Side") ($v -eq "Up") (10 + ($r * 3 + $c) * ($cw * $k + 6)) $y $k }
    }
    $y += $ch * $k + 16
    $sheet.Dispose()
}
# whirl ring: back + body + front, 4 frames
$back = [System.Drawing.Bitmap]::FromFile((Join-Path $O "Aren_WhirlRing_Back.png")); $front = [System.Drawing.Bitmap]::FromFile((Join-Path $O "Aren_WhirlRing_Front.png"))
$g.DrawString("4 Loc Kiem - Aren_WhirlRing_Back.png + _Front.png (64 x 48, pivot 32,34) - Back sau than, Front truoc than", $f, [System.Drawing.Brushes]::Yellow, 10, $y); $y += 22
$bodyBmp = Body "atk" "Down"
for ($c = 0; $c -lt 4; $c++) {
    $tile = New-Object System.Drawing.Bitmap 64, 48; $tg = [System.Drawing.Graphics]::FromImage($tile); $tg.Clear($grassC); $tg.InterpolationMode = 'NearestNeighbor'; $tg.PixelOffsetMode = 'Half'
    $src = New-Object System.Drawing.Rectangle ($c * 64), 0, 64, 48; $dst = New-Object System.Drawing.Rectangle 0, 0, 64, 48
    $tg.DrawImage($back, $dst, $src, [System.Drawing.GraphicsUnit]::Pixel)
    $tg.DrawImage($bodyBmp, (New-Object System.Drawing.Rectangle (32 - 24), (34 - 38), 48, 48), (New-Object System.Drawing.Rectangle 0, 0, 48, 48), [System.Drawing.GraphicsUnit]::Pixel)
    $tg.DrawImage($front, $dst, $src, [System.Drawing.GraphicsUnit]::Pixel)
    $g.DrawImage($tile, (New-Object System.Drawing.Rectangle (10 + $c * 270), $y, 256, 192)); $tile.Dispose()
}
$y += 210
# bulwark: 8 frames
$back = [System.Drawing.Bitmap]::FromFile((Join-Path $O "Aren_Bulwark_Back.png")); $front = [System.Drawing.Bitmap]::FromFile((Join-Path $O "Aren_Bulwark_Front.png"))
$g.DrawString("V Thanh Luy - Aren_Bulwark_Back.png + _Front.png (48 x 48, pivot 24,40): F0-1 xuat hien | F2-5 lap | F6-7 vo", $f, [System.Drawing.Brushes]::Yellow, 10, $y); $y += 22
$bodyBmp = Body "idle" "Down"
for ($c = 0; $c -lt 8; $c++) {
    $tile = New-Object System.Drawing.Bitmap 48, 48; $tg = [System.Drawing.Graphics]::FromImage($tile); $tg.Clear($grassC); $tg.InterpolationMode = 'NearestNeighbor'; $tg.PixelOffsetMode = 'Half'
    $src = New-Object System.Drawing.Rectangle ($c * 48), 0, 48, 48; $dst = New-Object System.Drawing.Rectangle 0, 0, 48, 48
    $tg.DrawImage($back, $dst, $src, [System.Drawing.GraphicsUnit]::Pixel)
    $tg.DrawImage($bodyBmp, (New-Object System.Drawing.Rectangle (24 - 24), (40 - 38), 48, 48), (New-Object System.Drawing.Rectangle 0, 0, 48, 48), [System.Drawing.GraphicsUnit]::Pixel)
    $tg.DrawImage($front, $dst, $src, [System.Drawing.GraphicsUnit]::Pixel)
    $g.DrawImage($tile, (New-Object System.Drawing.Rectangle (10 + $c * 200), $y, 192, 192)); $tile.Dispose()
}
$y += 210
# small sheets
$g.DrawString("Aren_ShieldGlint.png (2) | Aren_StunStars.png (4, lap) | Aren_ChargeDust.png (3) - o 16 x 16, x8", $f, [System.Drawing.Brushes]::Yellow, 10, $y); $y += 22
$xx = 10
foreach ($s in @(@("Aren_ShieldGlint.png", 2), @("Aren_StunStars.png", 4), @("Aren_ChargeDust.png", 3))) {
    $sheet = [System.Drawing.Bitmap]::FromFile((Join-Path $O $s[0]))
    for ($c = 0; $c -lt $s[1]; $c++) { $g.FillRectangle((New-Object System.Drawing.SolidBrush $grassC), $xx, $y, 128, 128); $g.DrawImage($sheet, (New-Object System.Drawing.Rectangle $xx, $y, 128, 128), (New-Object System.Drawing.Rectangle ($c * 16), 0, 16, 16), [System.Drawing.GraphicsUnit]::Pixel); $xx += 136 }
    $xx += 30
}
$y += 150
$b2 = $b.Clone((New-Object System.Drawing.Rectangle 0, 0, 1900, $y), $b.PixelFormat)
$b2.Save((Join-Path $D "aren_kit\AREN_KIT_VFX_SHEETS_REVIEW.png")); "board height $y"

# clipping check: any opaque pixel on a cell border = effect cut by the cell edge
$m = Import-Csv (Join-Path $O "AREN_KIT_VFX_MANIFEST.csv")
foreach ($r in $m) {
    $bmp = [System.Drawing.Bitmap]::FromFile((Join-Path $O $r.file)); $cw = [int]$r.cell_w; $ch = [int]$r.cell_h; $bad = 0
    for ($cy = 0; $cy -lt [int]$r.rows; $cy++) { for ($cx = 0; $cx -lt [int]$r.cols; $cx++) {
        for ($i = 0; $i -lt $cw; $i++) { if ($bmp.GetPixel($cx * $cw + $i, $cy * $ch).A -gt 0 -or $bmp.GetPixel($cx * $cw + $i, $cy * $ch + $ch - 1).A -gt 0) { $bad++ } }
        for ($j = 0; $j -lt $ch; $j++) { if ($bmp.GetPixel($cx * $cw, $cy * $ch + $j).A -gt 0 -or $bmp.GetPixel($cx * $cw + $cw - 1, $cy * $ch + $j).A -gt 0) { $bad++ } } } }
    # palette check
    $cols = @{}; for ($yy = 0; $yy -lt $bmp.Height; $yy++) { for ($xx = 0; $xx -lt $bmp.Width; $xx++) { $c = $bmp.GetPixel($xx, $yy); if ($c.A -gt 0) { $cols["{0:X2}{1:X2}{2:X2}" -f $c.R, $c.G, $c.B] = 1 } } }
    "{0,-26} {1}x{2} cells {3}x{4}  edgePixels={5}  colours={6} [{7}]" -f $r.file, $cw, $ch, $r.cols, $r.rows, $bad, $cols.Count, (($cols.Keys | Sort-Object) -join ' ')
    $bmp.Dispose()
}
