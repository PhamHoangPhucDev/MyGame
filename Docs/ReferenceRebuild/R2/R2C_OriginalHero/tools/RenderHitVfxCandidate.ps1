# Projectile hit VFX candidate (Lyra arrow, Vex thrown knife) -> ..\roster\HIT_VFX_CANDIDATE_v1.png
$D = Split-Path $PSScriptRoot -Parent
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\FxGen.cs", "$D\tools\VfxConceptGen.cs", "$D\tools\VexGen.cs", "$D\tools\VexEquip.cs", "$D\tools\HitVfxGen.cs", "$D\tools\MonsterGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$font = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fs = New-Object System.Drawing.Font "Consolas", 9
$K = 6; $c = 24 * $K
$b = New-Object System.Drawing.Bitmap 1240, 900; $g = [System.Drawing.Graphics]::FromImage($b); $g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$g.DrawString("VFX trung dich v1 - o 24x24, 4 frame, pivot = diem trung (giua o) | x6", $font, [System.Drawing.Brushes]::Yellow, 10, 6)
$ah = [HitVfxGen]::ArrowHit(); $kh = [HitVfxGen]::KnifeHit()
$rows = @(@($ah, 0, "Lyra - mui ten thuong"), @($ah, 1, "Lyra - mui ten nap (phat 3 / xuyen)"), @($kh, 0, "Vex - dao nem (Phi Dao)"))
$y = 32
foreach ($r in $rows) {
    $g.DrawString($r[2], $font, [System.Drawing.Brushes]::Yellow, 10, $y); $y += 20
    for ($f = 0; $f -lt 4; $f++) {
        $x = 10 + $f * ($c + 8); $g.FillRectangle($grass, $x, $y, $c, $c)
        $g.DrawImage($r[0], (New-Object System.Drawing.Rectangle $x, $y, $c, $c), (New-Object System.Drawing.Rectangle ($f * 24), ($r[1] * 24), 24, 24), [System.Drawing.GraphicsUnit]::Pixel)
        $g.DrawString("f$f", $fs, [System.Drawing.Brushes]::White, $x + 3, $y + 3)
    }
    # in context x3: on a goblin's chest
    $x = 10 + 4 * ($c + 8) + 20; $g.FillRectangle($grass, $x, $y, 144, 144)
    $gob = [MonsterGen]::Goblin("Idle", "Down", 0, $false)
    $g.DrawImage($gob, (New-Object System.Drawing.Rectangle ($x - 0), ($y - 36), 144, 144))
    $g.DrawImage($r[0], (New-Object System.Drawing.Rectangle ($x + 36), ($y + 30), 72, 72), (New-Object System.Drawing.Rectangle 24, ($r[1] * 24), 24, 24), [System.Drawing.GraphicsUnit]::Pixel)
    $g.DrawString("tren quai (f1)", $fs, [System.Drawing.Brushes]::White, $x + 3, $y + 3)
    $y += $c + 14
}
$b.Save((Join-Path $D "roster\HIT_VFX_CANDIDATE_v1.png")); "ok"
