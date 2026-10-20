# Aren skill body frames candidate -> ..\aren_kit\AREN_SKILL_BODY_CANDIDATE_v2.png
$D = Split-Path $PSScriptRoot -Parent
$Out = Join-Path $D "aren_kit"
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\ArenSkillGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$font = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fontS = New-Object System.Drawing.Font "Consolas", 8
$K = 4; $sz = 48 * $K
$cols = 8; $rowsN = 5
$board = New-Object System.Drawing.Bitmap ($cols * ($sz + 6) + 10), ($rowsN * ($sz + 32) + 40); $gr = [System.Drawing.Graphics]::FromImage($board); $gr.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'
$gr.DrawString("Aren - frame than rieng cho skill (CANDIDATE v2) | x4 | Side = quay phai; xuat sheet se lat nhu bo Aren da duyet", $font, [System.Drawing.Brushes]::Yellow, 10, 8)
function Tile($fr, $tx, $ty, $label) {
    $gr.FillRectangle($grass, $tx, $ty, $sz, $sz); $gr.FillEllipse($shadow, $tx + 16 * $K, $ty + 36 * $K, 16 * $K, 3 * $K)
    $gr.DrawImage([ArenSkillGen]::Compose($fr), (New-Object System.Drawing.Rectangle $tx, $ty, $sz, $sz))
    $gr.DrawString($label, $fontS, [System.Drawing.Brushes]::White, $tx + 3, $ty + 3)
}
$rowDefs = @(
    @("2 Khien Dap (2f)", "ShieldBash"),
    @("3 Xung Phong (2f lap)", "Charge"),
    @("4 Loc Kiem (4f xoay, dung chung moi huong)", "Whirl"),
    @("V Thanh Luy (1f giu)", "Bulwark"),
    @("C Chem Binh Minh (3f)", "Dawnbreaker")
)
for ($r = 0; $r -lt 5; $r++) {
    $ty = 50 + $r * ($sz + 32)
    $gr.DrawString($rowDefs[$r][0], $font, [System.Drawing.Brushes]::Yellow, 10, $ty - 20)
    $tiles = New-Object System.Collections.ArrayList
    if ($rowDefs[$r][1] -eq "Whirl") {
        $fs = [ArenSkillGen]::Whirl(); for ($i = 0; $i -lt $fs.Length; $i++) { [void]$tiles.Add(@($fs[$i], ("F$i " + $fs[$i].Note))) }
    } else {
        foreach ($v in @("Side", "Down", "Up")) {
            $fs = [ArenSkillGen]::($rowDefs[$r][1])($v)
            for ($i = 0; $i -lt $fs.Length; $i++) { [void]$tiles.Add(@($fs[$i], ("$v F$i " + $fs[$i].Note))) }
        }
    }
    for ($i = 0; $i -lt [math]::Min($tiles.Count, $cols); $i++) { Tile $tiles[$i][0] (10 + $i * ($sz + 6)) $ty $tiles[$i][1] }
}
$board.Save((Join-Path $Out "AREN_SKILL_BODY_CANDIDATE_v2.png")); "ok"
