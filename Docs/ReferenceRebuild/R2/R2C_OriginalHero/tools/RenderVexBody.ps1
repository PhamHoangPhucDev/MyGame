# Vex body frames review board -> ..\vex\Vex_body_v1_review.png
$D = Split-Path $PSScriptRoot -Parent
$O = Join-Path $D "vex"; New-Item -ItemType Directory -Force $O | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\VexGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$pal = [VexGen]::Pal
foreach ($v in @("Down", "Up", "Side")) { $rows = [VexGen]::Base($v); if ($rows.Length -ne 21) { throw "$v rows $($rows.Length)" }; for ($y = 0; $y -lt 21; $y++) { if ($rows[$y].Length -ne 14) { throw "$v row $y len $($rows[$y].Length)" }; foreach ($ch in $rows[$y].ToCharArray()) { if ($ch -ne '.' -and -not $pal.ContainsKey($ch)) { throw "$v row $y char $ch" } } } }
function Paint($g, $grid, $x0, $y0, $k) {
    for ($y = 0; $y -lt 21; $y++) { for ($x = 0; $x -lt 14; $x++) { $ch = $grid[$x, $y]; if ($ch -ne '.') { $b = New-Object System.Drawing.SolidBrush ([HeroConceptGen]::Hex($pal[$ch])); $g.FillRectangle($b, $x0 + (9 + $x) * $k, $y0 + (9 + $y) * $k, $k, $k); $b.Dispose() } } }
}
$K = 5; $cell = 32 * $K
$f = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold)
$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$b = New-Object System.Drawing.Bitmap (11 * ($cell + 6) + 20), (3 * ($cell + 30) + 30); $g = [System.Drawing.Graphics]::FromImage($b); $g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32))
$views = @("Down", "Up", "Side")
for ($r = 0; $r -lt 3; $r++) {
    $v = $views[$r]; $y = 26 + $r * ($cell + 30)
    $g.DrawString("$v   Idle F0-F3 | Walk F0-F3 | Attack F0-F2", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 22)
    $col = 0
    for ($i = 0; $i -lt 4; $i++) { $x = 10 + $col * ($cell + 6); $g.FillRectangle($grass, $x, $y, $cell, $cell); Paint $g ([VexGen]::Idle($v, $i)) $x $y $K; $col++ }
    for ($i = 0; $i -lt 4; $i++) { $x = 10 + $col * ($cell + 6); $g.FillRectangle($grass, $x, $y, $cell, $cell); Paint $g ([VexGen]::Walk($v, $i)) $x $y $K; $col++ }
    for ($i = 0; $i -lt 3; $i++) { $x = 10 + $col * ($cell + 6); $g.FillRectangle($grass, $x, $y, $cell, $cell); Paint $g ([VexGen]::Attack($v, $i)) $x $y $K; $col++ }
}
$b.Save((Join-Path $O "Vex_body_v1_review.png")); "ok"
