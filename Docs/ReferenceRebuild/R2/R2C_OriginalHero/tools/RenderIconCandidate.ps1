# Skill icon candidate (Aren x5) -> ..\icons\ICONS_CANDIDATE_v2.png
$D = Split-Path $PSScriptRoot -Parent
$Out = Join-Path $D "icons"; New-Item -ItemType Directory -Force $Out | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\ArenKitSheets.cs", "$D\tools\IconGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$font = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fontS = New-Object System.Drawing.Font "Consolas", 9
$board = New-Object System.Drawing.Bitmap 1000, 560; $gr = [System.Drawing.Graphics]::FromImage($board); $gr.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'
$gr.DrawString("Icon skill v2 - Aren 32x32: khung thep dinh vang, trang bi that cua Aren + VFX trang-bang-vang | x4 ; o hotbar 68 px (icon 64 px) ; x1", $font, [System.Drawing.Brushes]::Yellow, 10, 6)
$slot = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 20, 16, 18)); $slotRim = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 140, 108, 76)), 3
for ($i = 0; $i -lt 5; $i++) {
    $ic = [IconGen]::Aren($i)
    $x = 10 + $i * 190
    $gr.DrawImage($ic, (New-Object System.Drawing.Rectangle $x, 34, 128, 128))
    $gr.DrawString([IconGen]::ArenNames[$i], $fontS, [System.Drawing.Brushes]::White, $x, 166)
    # in-slot preview: 68 px frame, icon at 72% (48 px = 2x)
    $sx = 10 + $i * 90; $sy = 240
    $gr.FillRectangle($slot, $sx, $sy, 68, 68); $gr.DrawRectangle($slotRim, $sx, $sy, 68, 68)
    $gr.DrawImage($ic, (New-Object System.Drawing.Rectangle ($sx + 2), ($sy + 2), 64, 64))
    $gr.DrawString(@("2", "3", "4", "V", "C")[$i], $fontS, [System.Drawing.Brushes]::White, $sx + 54, $sy + 52)
    $gr.DrawImage($ic, (New-Object System.Drawing.Rectangle (10 + $i * 40), 340, 32, 32))
    $ic.Save((Join-Path $Out ("Aren_Skill_" + $i + "_candidate.png")))
}
$gr.DrawString("(chua xuat bo chinh thuc - chi la mau)", $fontS, [System.Drawing.Brushes]::Gray, 10, 380)
$board.Save((Join-Path $Out "ICONS_CANDIDATE_v2.png")); "ok"
