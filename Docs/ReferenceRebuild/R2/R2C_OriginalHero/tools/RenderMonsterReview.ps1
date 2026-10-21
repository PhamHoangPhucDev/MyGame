# Review boards for the exported monster sheets -> ..\monsters\review\{Name}_REVIEW.png (x3, on grass, with labels)
$D = Split-Path $PSScriptRoot -Parent
$Out = Join-Path $D "monsters\review"; New-Item -ItemType Directory -Force $Out | Out-Null
Add-Type -AssemblyName System.Drawing
$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(70, 0, 0, 0))
$font = New-Object System.Drawing.Font "Consolas", 12, ([System.Drawing.FontStyle]::Bold)
$anims = @("Idle", "Walk", "Attack", "Hurt", "Death"); $frames = @(4, 4, 3, 2, 4); $views = @("Down", "Up", "Side (trai)")
$mons = @(@("Goblin", 48, 37), @("GoblinArcher", 48, 37), @("Gobrider", 48, 41), @("GobriderElite", 48, 41), @("GobriderWarlord", 64, 57))
$K = 3
foreach ($m in $mons) {
    $name = $m[0]; $S = $m[1]; $sole = $m[2]; $c = $S * $K
    $sheet = [System.Drawing.Bitmap]::FromFile((Join-Path $D "unity_export_monsters\$name\${name}_Sheet.png"))
    $lw = 80; $W = $lw + 3 * (4 * ($c + 2) + 16); $H = 56 + 5 * ($c + 4)
    $b = New-Object System.Drawing.Bitmap $W, $H; $g = [System.Drawing.Graphics]::FromImage($b)
    $g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
    $g.DrawString("$name - bo day du (x$K) | o ${S}x${S}, chan hang $sole | hang Side ve quay trai", $font, [System.Drawing.Brushes]::Yellow, 8, 6)
    for ($vi = 0; $vi -lt 3; $vi++) { $g.DrawString($views[$vi], $font, [System.Drawing.Brushes]::White, ($lw + $vi * (4 * ($c + 2) + 16)), 30) }
    for ($a = 0; $a -lt 5; $a++) {
        $y = 56 + $a * ($c + 4)
        $g.DrawString($anims[$a], $font, [System.Drawing.Brushes]::Yellow, 8, ($y + $c / 2 - 8))
        for ($vi = 0; $vi -lt 3; $vi++) {
            for ($f = 0; $f -lt $frames[$a]; $f++) {
                $x = $lw + $vi * (4 * ($c + 2) + 16) + $f * ($c + 2)
                $g.FillRectangle($grass, $x, $y, $c, $c)
                $sw = [int]($S * 0.42 * $K); $g.FillEllipse($shadow, ($x + $c / 2 - $sw / 2), ($y + ($sole - 1) * $K), $sw, 3 * $K)
                $src = New-Object System.Drawing.Rectangle ($f * $S), (($a * 3 + $vi) * $S), $S, $S
                $g.DrawImage($sheet, (New-Object System.Drawing.Rectangle $x, $y, $c, $c), $src, [System.Drawing.GraphicsUnit]::Pixel)
            }
        }
    }
    $sheet.Dispose(); $b.Save((Join-Path $Out "${name}_REVIEW.png")); $g.Dispose(); $b.Dispose()
}
"ok"
