# Weapons-on-back candidate (Aren, Brom, Selene) -> ..\roster\BACK_GEAR_CANDIDATE_v1.png
$D = Split-Path $PSScriptRoot -Parent
$Out = Join-Path $D "roster"
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\FxGen.cs", "$D\tools\VfxConceptGen.cs", "$D\tools\ArenKitSheets.cs", "$D\tools\BromGen.cs", "$D\tools\BromKit.cs", "$D\tools\SeleneGen.cs", "$D\tools\SeleneKit.cs", "$D\tools\BackGearGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$font = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fontS = New-Object System.Drawing.Font "Consolas", 8
$K = 4; $sz = 48 * $K; $cols = 6
$board = New-Object System.Drawing.Bitmap ($cols * ($sz + 6) + 10), (3 * ($sz + 32) + 50); $gr = [System.Drawing.Graphics]::FromImage($board); $gr.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'
$gr.DrawString("Idle/Walk: vu khi deo sau lung (CANDIDATE) | x4 | cot cuoi = Attack (vu khi rut ra, giu nguyen)", $font, [System.Drawing.Brushes]::Yellow, 10, 8)
function Tile($bmp, $tx, $ty, $label) {
    $gr.FillRectangle($grass, $tx, $ty, $sz, $sz); $gr.FillEllipse($shadow, $tx + 16 * $K, $ty + 36 * $K, 16 * $K, 3 * $K)
    $gr.DrawImage($bmp, (New-Object System.Drawing.Rectangle $tx, $ty, $sz, $sz))
    $gr.DrawString($label, $fontS, [System.Drawing.Brushes]::White, $tx + 2, $ty + 2)
}
$classes = @("Aren", "Brom", "Selene")
for ($r = 0; $r -lt 3; $r++) {
    $cls = $classes[$r]; $ty = 46 + $r * ($sz + 32)
    $gr.DrawString($cls, $font, [System.Drawing.Brushes]::Yellow, 10, $ty - 20)
    $tiles = @(
        @([BackGearGen]::Compose($cls, "Idle", "Down", 0, $false), "Idle Down (dau vu khi nho qua vai)"),
        @([BackGearGen]::Compose($cls, "Idle", "Side", 0, $false), "Idle Side 3/4 phai"),
        @([BackGearGen]::Compose($cls, "Idle", "Up", 0, $false), "Idle Up (sau lung)"),
        @([BackGearGen]::Compose($cls, "Idle", "Side", 0, $true), "Idle Side trai (flipX)"),
        @([BackGearGen]::Compose($cls, "Walk", "Side", 0, $false), "Walk Side F0")
    )
    for ($i = 0; $i -lt 5; $i++) { Tile $tiles[$i][0] (10 + $i * ($sz + 6)) $ty $tiles[$i][1] }
    if ($cls -eq "Aren") { $atk = [EquipGen]::ArenAttackComposite("Side", 1, [EquipGen]::SteelLongsword, [Func[string, EquipGen+Spr]] { param($v) [EquipGen]::HeaterShield($v) }, $null) }
    elseif ($cls -eq "Brom") { $atk = [BromKit]::Preview("Attack", "Side", 1, 0) }
    else { $atk = [SeleneKit]::Preview("Attack", "Side", 1, 0, $false) }
    Tile $atk (10 + 5 * ($sz + 6)) $ty "Attack Side (cam tren tay)"
}
$board.Save((Join-Path $Out "BACK_GEAR_CANDIDATE_v1.png")); "ok"
