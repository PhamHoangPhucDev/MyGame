# Buff style comparison -> ..\icons\STATUS_BUFF_STYLES.png (debuffs fixed, buffs in styles A / B / C)
$D = Split-Path $PSScriptRoot -Parent
$Out = Join-Path $D "icons"
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\ArenKitSheets.cs", "$D\tools\StatusIconGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$font = New-Object System.Drawing.Font "Consolas", 12, ([System.Drawing.FontStyle]::Bold); $fontS = New-Object System.Drawing.Font "Consolas", 8
$board = New-Object System.Drawing.Bitmap 1270, 640; $gr = [System.Drawing.Graphics]::FromImage($board); $gr.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'
$gr.DrawString("So sanh 3 phuong an icon BUFF (4 debuff ben trai giu nguyen da duyet) | x4, ben duoi x2", $font, [System.Drawing.Brushes]::Yellow, 10, 8)
$labels = @("A - hien tai v3: nhieu mau, nen xanh duong", "B - nen XANH LA, hinh 3 tong trang/vang (quy uoc buff pho bien)", "C - nen xanh duong, hinh 3 tong trang/xanh bang")
for ($s = 0; $s -lt 3; $s++) {
    $y = 40 + $s * 200
    $gr.DrawString($labels[$s], $font, [System.Drawing.Brushes]::White, 10, $y)
    for ($i = 0; $i -lt 11; $i++) {
        $ic = [StatusIconGen]::Icon($i, $s)
        $x = 10 + $i * 104 + $(if ($i -ge 4) { 30 } else { 0 })
        $gr.DrawImage($ic, (New-Object System.Drawing.Rectangle $x, ($y + 24), 96, 96))
        $gr.DrawString([StatusIconGen]::NamesVi[$i], $fontS, [System.Drawing.Brushes]::Gray, $x, ($y + 122))
        $gr.DrawImage($ic, (New-Object System.Drawing.Rectangle (10 + $i * 52 + $(if ($i -ge 4) { 16 } else { 0 })), ($y + 140), 48, 48))
    }
}
$board.Save((Join-Path $Out "STATUS_BUFF_STYLES.png")); "ok"
