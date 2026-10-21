# Lyra skill body frames candidate -> ..\lyra\LYRA_SKILL_BODY_CANDIDATE_v1.png
$D = Split-Path $PSScriptRoot -Parent
$Out = Join-Path $D "lyra"
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\FxGen.cs", "$D\tools\VfxConceptGen.cs", "$D\tools\ArenKitSheets.cs", "$D\tools\LyraGen.cs", "$D\tools\LyraKit.cs", "$D\tools\LyraSkillGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$font = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fontS = New-Object System.Drawing.Font "Consolas", 8
$K = 3; $sz = 48 * $K; $cols = 6
$board = New-Object System.Drawing.Bitmap ($cols * ($sz + 6) + 10), (4 * ($sz + 32) + 60); $gr = [System.Drawing.Graphics]::FromImage($board); $gr.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'
$gr.DrawString("Lyra - frame than rieng cho skill (CANDIDATE) | x3 | Side = quay phai; V Buoc Gio dung clip Walk", $font, [System.Drawing.Brushes]::Yellow, 10, 8)
function Tile($fr, $tx, $ty, $label) {
    $gr.FillRectangle($grass, $tx, $ty, $sz, $sz); $gr.FillEllipse($shadow, $tx + 16 * $K, $ty + 36 * $K, 16 * $K, 3 * $K)
    $gr.DrawImage([LyraSkillGen]::Compose($fr), (New-Object System.Drawing.Rectangle $tx, $ty, $sz, $sz))
    $gr.DrawString($label, $fontS, [System.Drawing.Brushes]::White, $tx + 2, $ty + 2)
}
$defs = @(@("2 Ten Quat (2f)", "FanShot"), @("3 Nhay Lui (2f)", "LeapBack"), @("4 Mua Ten (2f)", "ArrowRain"), @("C Ten Xuyen Rung (2f)", "ForestPierce"))
for ($r = 0; $r -lt 4; $r++) {
    $ty = 50 + $r * ($sz + 32)
    $gr.DrawString($defs[$r][0], $font, [System.Drawing.Brushes]::Yellow, 10, $ty - 20)
    $tiles = New-Object System.Collections.ArrayList
    foreach ($v in @("Side", "Down", "Up")) {
        $fs = [LyraSkillGen]::($defs[$r][1])($v)
        for ($i = 0; $i -lt $fs.Length; $i++) { [void]$tiles.Add(@($fs[$i], ("$v F$i " + $fs[$i].Note))) }
    }
    for ($i = 0; $i -lt [math]::Min($tiles.Count, $cols); $i++) { Tile $tiles[$i][0] (10 + $i * ($sz + 6)) $ty $tiles[$i][1] }
}
$board.Save((Join-Path $Out "LYRA_SKILL_BODY_CANDIDATE_v1.png")); "ok"
