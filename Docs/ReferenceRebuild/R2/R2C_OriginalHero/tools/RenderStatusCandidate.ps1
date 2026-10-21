# Status icons candidate -> ..\icons\STATUS_CANDIDATE_v6.png
$D = Split-Path $PSScriptRoot -Parent
$Out = Join-Path $D "icons"; New-Item -ItemType Directory -Force $Out | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\ArenKitSheets.cs", "$D\tools\StatusIconGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$font = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fontS = New-Object System.Drawing.Font "Consolas", 8
$n = [StatusIconGen]::Ids.Length
$board = New-Object System.Drawing.Bitmap 1240, 470; $gr = [System.Drawing.Graphics]::FromImage($board); $gr.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'
$gr.DrawString("Icon trang thai v6 24x24: buff khung xanh duong, ve sat nghia | x4 ; x2 (UI) ; x1 ; tren dau quai (game x3) - vien do = debuff, vien xanh = buff", $font, [System.Drawing.Brushes]::Yellow, 10, 8)
$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$icons = @()
for ($i = 0; $i -lt $n; $i++) {
    $ic = [StatusIconGen]::Icon($i); $icons += $ic
    $x = 10 + $i * 110
    $gr.DrawImage($ic, (New-Object System.Drawing.Rectangle $x, 36, 96, 96))
    $gr.DrawString([StatusIconGen]::NamesVi[$i], $fontS, [System.Drawing.Brushes]::White, $x, 136)
    $gr.DrawString(([StatusIconGen]::Source[$i] -replace ', ', "`n"), $fontS, [System.Drawing.Brushes]::Gray, $x, 150)
    $gr.DrawImage($ic, (New-Object System.Drawing.Rectangle (10 + $i * 52), 230, 48, 48))
    $gr.DrawImage($ic, (New-Object System.Drawing.Rectangle (10 + $i * 28), 290, 24, 24))
    $ic.Save((Join-Path $Out ("Status_" + [StatusIconGen]::Ids[$i] + "_candidate.png")))
}
# in-world mock: grass strip, goblin-ish placeholder bars with 2-3 icons above a fake HP bar, scaled x3 like the game camera
$gr.FillRectangle($grass, 10, 320, 600, 130)
$hp = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 200, 30, 30)); $hpBg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 40, 10, 10))
$sets = @(@(0, 1), @(2, 3), @(7, 9, 4))
for ($s = 0; $s -lt 3; $s++) {
    $bx = 40 + $s * 190
    $gr.FillRectangle($hpBg, $bx, 400, 96, 9); $gr.FillRectangle($hp, $bx, 400, 64, 9)
    for ($k = 0; $k -lt $sets[$s].Length; $k++) { $gr.DrawImage($icons[$sets[$s][$k]], (New-Object System.Drawing.Rectangle ($bx + $k * 52), 330, 48, 48)) }
}
$gr.DrawString("mo phong tren dau quai (x3)", $fontS, [System.Drawing.Brushes]::White, 14, 324)
$board.Save((Join-Path $Out "STATUS_CANDIDATE_v6.png")); "ok"
