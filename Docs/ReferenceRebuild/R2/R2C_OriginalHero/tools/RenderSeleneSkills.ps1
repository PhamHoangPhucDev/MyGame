# Selene skill body frames: export (..\unity_export_selene\Skills\) + review board (..\selene\) + pixel checks
$D = Split-Path $PSScriptRoot -Parent
$X = Join-Path $D "unity_export_selene\Skills"; $Out = Join-Path $D "selene"
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\FxGen.cs", "$D\tools\VfxConceptGen.cs", "$D\tools\ArenKitSheets.cs", "$D\tools\SeleneGen.cs", "$D\tools\SeleneKit.cs", "$D\tools\SeleneSkillGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
[SeleneSkillGen]::Export($X)

$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$font = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold); $fontS = New-Object System.Drawing.Font "Consolas", 8
$rows = [SeleneSkillGen]::Rows()
$K = 3; $sz = 48 * $K
$board = New-Object System.Drawing.Bitmap (420 + 2 * ($sz + 6)), (40 + $rows.Count * ($sz + 18)); $gr = [System.Drawing.Graphics]::FromImage($board); $gr.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'
$gr.DrawString("Selene_SkillSheet.png (x3, trai) | ghep gay + tay theo Selene_SkillAnchors.csv (x3, phai) - Side quay trai", $font, [System.Drawing.Brushes]::Yellow, 10, 8)
$sheet = [System.Drawing.Bitmap]::FromFile((Join-Path $X "Selene_SkillSheet.png"))
$gr.FillRectangle($grass, 10, 40, 384, $sheet.Height * 3); $gr.DrawImage($sheet, (New-Object System.Drawing.Rectangle 10, 40, 384, ($sheet.Height * 3)))
for ($r = 0; $r -lt $rows.Count; $r++) {
    $row = $rows[$r]; $ty = 40 + $r * ($sz + 18)
    for ($f = 0; $f -lt $row.Frames.Length; $f++) {
        $tx = 420 + $f * ($sz + 6)
        $gr.FillRectangle($grass, $tx, $ty, $sz, $sz); $gr.FillEllipse($shadow, $tx + 16 * $K, $ty + 36 * $K, 16 * $K, 3 * $K)
        $gr.DrawImage([SeleneSkillGen]::Compose($row.Frames[$f]), (New-Object System.Drawing.Rectangle $tx, $ty, $sz, $sz))
        $gr.DrawString(($row.Skill + "_" + $row.View + " $f"), $fontS, [System.Drawing.Brushes]::White, $tx + 2, $ty + 2)
    }
}
$board.Save((Join-Path $Out "Selene_skill_body_review.png"))

$bad = 0; $semi = 0; $soleOff = 0
for ($cy = 0; $cy -lt ($sheet.Height / 32); $cy++) { for ($cx = 0; $cx -lt 4; $cx++) {
    $any = $false; $lowest = -1
    for ($y = 0; $y -lt 32; $y++) { for ($x = 0; $x -lt 32; $x++) { $a = $sheet.GetPixel($cx * 32 + $x, $cy * 32 + $y).A; if ($a -gt 0) { $any = $true; if ($y -gt $lowest) { $lowest = $y }; if ($x -eq 0 -or $y -eq 0 -or $x -eq 31 -or $y -eq 31) { $bad++ }; if ($a -lt 255) { $semi++ } } } }
    if ($any -and $lowest -ne 29) { $soleOff++; "cell r$cy c$cx lowest row $lowest" }
} }
"Selene_SkillSheet.png {0}x{1} edgePixels={2} semiAlpha={3} cellsWithSoleNot29={4}" -f $sheet.Width, $sheet.Height, $bad, $semi, $soleOff
$sheet.Dispose()
