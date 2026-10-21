# Selene skill body frames candidate -> ..\selene\SELENE_SKILL_BODY_CANDIDATE_v1.png
$D = Split-Path $PSScriptRoot -Parent
$Out = Join-Path $D "selene"
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\FxGen.cs", "$D\tools\VfxConceptGen.cs", "$D\tools\ArenKitSheets.cs", "$D\tools\SeleneGen.cs", "$D\tools\SeleneKit.cs", "$D\tools\SeleneSkillGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$font = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fontS = New-Object System.Drawing.Font "Consolas", 8
$K = 3; $sz = 48 * $K; $cols = 6
$board = New-Object System.Drawing.Bitmap ($cols * ($sz + 6) + 10), (5 * ($sz + 32) + 60); $gr = [System.Drawing.Graphics]::FromImage($board); $gr.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'
$gr.DrawString("Selene - frame than rieng cho skill (CANDIDATE) | x3 | Side = quay phai", $font, [System.Drawing.Brushes]::Yellow, 10, 8)
function Tile($fr, $tx, $ty, $label) {
    $gr.FillRectangle($grass, $tx, $ty, $sz, $sz); $gr.FillEllipse($shadow, $tx + 16 * $K, $ty + 36 * $K, 16 * $K, 3 * $K)
    $gr.DrawImage([SeleneSkillGen]::Compose($fr), (New-Object System.Drawing.Rectangle $tx, $ty, $sz, $sz))
    $gr.DrawString($label, $fontS, [System.Drawing.Brushes]::White, $tx + 2, $ty + 2)
}
$defs = @(@("2 Cau Tinh Tu (2f)", "StarOrb"), @("3 Dich Chuyen (2f)", "Blink"), @("4 Vong Phu Chu (2f)", "RuneCircle"), @("V Khien Ma Luc (1f giu)", "ManaShield"), @("C Mua Sao Bang (2f)", "MeteorShower"))
for ($r = 0; $r -lt 5; $r++) {
    $ty = 50 + $r * ($sz + 32)
    $gr.DrawString($defs[$r][0], $font, [System.Drawing.Brushes]::Yellow, 10, $ty - 20)
    $tiles = New-Object System.Collections.ArrayList
    foreach ($v in @("Side", "Down", "Up")) {
        $fs = [SeleneSkillGen]::($defs[$r][1])($v)
        for ($i = 0; $i -lt $fs.Length; $i++) { [void]$tiles.Add(@($fs[$i], ("$v F$i " + $fs[$i].Note))) }
    }
    for ($i = 0; $i -lt [math]::Min($tiles.Count, $cols); $i++) { Tile $tiles[$i][0] (10 + $i * ($sz + 6)) $ty $tiles[$i][1] }
}
$board.Save((Join-Path $Out "SELENE_SKILL_BODY_CANDIDATE_v1.png")); "ok"
