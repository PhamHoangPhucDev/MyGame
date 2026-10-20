# 30 hotbar icons (5 classes x 2/3/4/V/C skills + R basic combo): export (..\icons\export\) + review board (..\icons\) + pixel checks
$D = Split-Path $PSScriptRoot -Parent
$Out = Join-Path $D "icons"; $Exp = Join-Path $Out "export"
New-Item -ItemType Directory -Force $Out, $Exp | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\ArenKitSheets.cs", "$D\tools\LyraGen.cs", "$D\tools\SeleneGen.cs", "$D\tools\IconGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$classes = [IconGen]::Classes; $keys = [IconGen]::Keys

# export: single PNGs + 6x5 atlas (row = class, cols 0-4 = keys 2/3/4/V/C, col 5 = R) + manifest
$atlas = New-Object System.Drawing.Bitmap 192, 160
$csv = New-Object System.Text.StringBuilder; [void]$csv.AppendLine("file,class,key,skill_id,name_vi,atlas_col,atlas_row,size")
$icons = @{}
function Put($ic, $col, $row) { for ($y = 0; $y -lt 32; $y++) { for ($x = 0; $x -lt 32; $x++) { $px = $ic.GetPixel($x, $y); if ($px.A -gt 0) { $atlas.SetPixel($col * 32 + $x, $row * 32 + $y, $px) } } } }
for ($r = 0; $r -lt 5; $r++) {
    $cls = $classes[$r]
    for ($c = 0; $c -lt 5; $c++) {
        $ic = [IconGen]::Icon($cls, $c); $icons["$cls$c"] = $ic
        $id = [IconGen]::SkillIds[$cls][$c]; $name = [IconGen]::SkillNamesVi[$cls][$c]
        $file = "Icon_{0}_{1}_{2}.png" -f $cls, $keys[$c], $id
        $ic.Save((Join-Path $Exp $file)); Put $ic $c $r
        [void]$csv.AppendLine(("{0},{1},{2},{3},{4},{5},{6},32" -f $file, $cls, $keys[$c], $id, $name, $c, $r))
    }
    $ic = [IconGen]::Combo($cls); $icons["${cls}R"] = $ic
    $file = "Icon_{0}_R_Combo.png" -f $cls
    $ic.Save((Join-Path $Exp $file)); Put $ic 5 $r
    [void]$csv.AppendLine(("{0},{1},R,Combo,{2},5,{3},32" -f $file, $cls, [IconGen]::ComboNamesVi[$cls], $r))
}
$atlas.Save((Join-Path $Exp "Icons_Skills_Atlas.png"))
[System.IO.File]::WriteAllText((Join-Path $Exp "ICONS_MANIFEST.csv"), $csv.ToString(), (New-Object System.Text.UTF8Encoding $false))

# board: per class row - R + 5 skills x4 with names, x1 strip, and a hotbar mock (5 skill slots 68 px + big R 120 px)
$font = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fontS = New-Object System.Drawing.Font "Consolas", 9
$board = New-Object System.Drawing.Bitmap 1420, (40 + 5 * 210); $gr = [System.Drawing.Graphics]::FromImage($board); $gr.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'
$gr.DrawString("Icon hotbar 30 (32x32, khung rieng tung class): R don thuong + 5 skill | x4 ; x1 ; mo phong hotbar (o skill 68 px / icon 64, nut R 120 px / icon 96 = x3)", $font, [System.Drawing.Brushes]::Yellow, 10, 8)
$slot = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 20, 16, 18)); $slotRim = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 140, 108, 76)), 3
for ($r = 0; $r -lt 5; $r++) {
    $cls = $classes[$r]; $y0 = 40 + $r * 210
    $gr.DrawString($cls, $font, [System.Drawing.Brushes]::White, 10, $y0)
    $order = @("R", 0, 1, 2, 3, 4)
    for ($j = 0; $j -lt 6; $j++) {
        $k = $order[$j]; $ic = $icons["$cls$k"]; $x = 90 + $j * 140
        $label = if ($k -eq "R") { "R " + [IconGen]::ComboNamesVi[$cls] } else { $keys[$k] + " " + [IconGen]::SkillNamesVi[$cls][$k] }
        $gr.DrawImage($ic, (New-Object System.Drawing.Rectangle $x, $y0, 128, 128))
        $gr.DrawString($label, $fontS, [System.Drawing.Brushes]::White, $x, ($y0 + 132))
        $gr.DrawImage($ic, (New-Object System.Drawing.Rectangle (90 + $j * 36), ($y0 + 156), 32, 32))
    }
    for ($c = 0; $c -lt 5; $c++) {
        $sx = 960 + ($c % 3) * 76; $sy = $y0 + [math]::Floor($c / 3) * 76
        $gr.FillRectangle($slot, $sx, $sy, 68, 68); $gr.DrawRectangle($slotRim, $sx, $sy, 68, 68)
        $gr.DrawImage($icons["$cls$c"], (New-Object System.Drawing.Rectangle ($sx + 2), ($sy + 2), 64, 64))
        $gr.DrawString($keys[$c], $fontS, [System.Drawing.Brushes]::White, $sx + 56, $sy + 54)
    }
    $rx = 1200; $ry = $y0 + 10
    $gr.FillEllipse($slot, $rx, $ry, 120, 120); $gr.DrawEllipse($slotRim, $rx, $ry, 120, 120)
    $gr.DrawImage($icons["${cls}R"], (New-Object System.Drawing.Rectangle ($rx + 12), ($ry + 12), 96, 96))
    $gr.DrawString("R", $font, [System.Drawing.Brushes]::White, $rx + 100, $ry + 98)
}
$board.Save((Join-Path $Out "ICONS_30_review.png"))

# pixel checks: 1-px transparent margin, no semi-alpha
foreach ($k in $icons.Keys | Sort-Object) {
    $ic = $icons[$k]; $edge = 0; $semi = 0
    for ($i = 0; $i -lt 32; $i++) { foreach ($p in @(@($i, 0), @($i, 31), @(0, $i), @(31, $i))) { if ($ic.GetPixel($p[0], $p[1]).A -gt 0) { $edge++ } } }
    for ($y = 0; $y -lt 32; $y++) { for ($x = 0; $x -lt 32; $x++) { $a = $ic.GetPixel($x, $y).A; if ($a -gt 0 -and $a -lt 255) { $semi++ } } }
    if ($edge -gt 0 -or $semi -gt 0) { "{0}: edge={1} semi={2}" -f $k, $edge, $semi }
}
"checked $($icons.Count) icons"
