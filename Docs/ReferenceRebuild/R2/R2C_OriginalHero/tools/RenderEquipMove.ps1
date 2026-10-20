# Aren Idle + Walk with sword (lowered) and shield attached by anchors. Output in ..\attack\
$D = Split-Path $PSScriptRoot -Parent
$O = Join-Path $D "attack"; New-Item -ItemType Directory -Force $O | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
[EquipGen]::WriteMoveAnchors($O)

$f = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold)
$fs = New-Object System.Drawing.Font "Consolas", 9
$grassC = [System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87)
$grass = New-Object System.Drawing.SolidBrush $grassC
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(90, 0, 0, 0))
$heater = [Func[string, EquipGen+Spr]] { param($v) [EquipGen]::HeaterShield($v) }
$K = 4; $sz = 48 * $K

foreach ($anim in @("Idle", "Walk")) {
    $b = New-Object System.Drawing.Bitmap 820, 680; $g = [System.Drawing.Graphics]::FromImage($b)
    $g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
    $times = if ($anim -eq "Walk") { @("0.125", "0.125", "0.125", "0.125") } else { @("0.25", "0.15", "0.25", "0.15") }
    $views = @(@("Down", "Down"), @("Up", "Up (back)"), @("Side", "Side 3/4 right"))
    for ($r = 0; $r -lt 3; $r++) {
        $y = 30 + $r * 220
        $g.DrawString("Aren $anim $($views[$r][1]) - kiem ha + khien thu (anchor theo frame)", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
        for ($i = 0; $i -lt 4; $i++) {
            $x = 10 + $i * 200
            $bmp = [EquipGen]::ArenMoveComposite($anim, $views[$r][0], $i, [EquipGen]::SteelLongsword, $heater, $null)
            $g.FillRectangle($grass, $x, $y, $sz, $sz); $g.FillEllipse($shadow, $x + 18 * $K, $y + 36 * $K, 12 * $K, 3 * $K)
            $g.DrawImage($bmp, (New-Object System.Drawing.Rectangle $x, $y, $sz, $sz), (New-Object System.Drawing.Rectangle 0, 0, 48, 48), [System.Drawing.GraphicsUnit]::Pixel)
            $g.DrawString("F$i $($times[$i])s", $fs, [System.Drawing.Brushes]::White, $x + 3, $y + 3)
        }
    }
    $b.Save((Join-Path $O "Aren_${anim}_equip_v1_review.png"))
}
"done"
