# Boss review boards (one per view, x2) -> ..\monsters\review\Cyclops_REVIEW_{View}.png
$D = Split-Path $PSScriptRoot -Parent
$Out = Join-Path $D "monsters\review"; New-Item -ItemType Directory -Force $Out | Out-Null
Add-Type -AssemblyName System.Drawing
$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(70, 0, 0, 0))
$font = New-Object System.Drawing.Font "Consolas", 12, ([System.Drawing.FontStyle]::Bold)
$anims = @("IdleCalm (hien)", "WalkCalm (hien)", "IdleAngry (hung du)", "WalkAngry (hung du)", "Attack (f2 = dap)", "Hurt", "Death"); $frames = @(4, 4, 4, 4, 4, 2, 4)
$views = @("Down", "Up", "Side")
$S = 96; $sole = 89; $K = 2; $c = $S * $K
$sheet = [System.Drawing.Bitmap]::FromFile((Join-Path $D "unity_export_monsters\Cyclops\Cyclops_Sheet.png"))
$tele = [System.Drawing.Bitmap]::FromFile((Join-Path $D "unity_export_monsters\Cyclops\Cyclops_Telegraph.png"))
$crack = [System.Drawing.Bitmap]::FromFile((Join-Path $D "unity_export_monsters\Cyclops\Cyclops_Crack.png"))
for ($vi = 0; $vi -lt 3; $vi++) {
    $lw = 190; $W = $lw + 4 * ($c + 4); $H = 40 + 7 * ($c + 4)
    $b = New-Object System.Drawing.Bitmap $W, $H; $g = [System.Drawing.Graphics]::FromImage($b)
    $g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
    $g.DrawString("Cyclops BOSS - $($views[$vi]) (x$K) | o 96x96, chan hang 89 | hang Side ve quay trai", $font, [System.Drawing.Brushes]::Yellow, 8, 8)
    for ($a = 0; $a -lt 7; $a++) {
        $y = 40 + $a * ($c + 4)
        $g.DrawString($anims[$a], $font, [System.Drawing.Brushes]::Yellow, 8, ($y + $c / 2 - 8))
        for ($f = 0; $f -lt $frames[$a]; $f++) {
            $x = $lw + $f * ($c + 4)
            $g.FillRectangle($grass, $x, $y, $c, $c); $g.SetClip((New-Object System.Drawing.Rectangle $x, $y, $c, $c))
            if ($a -eq 4) { $tf = [Math]::Min($f + 1, 4); if ($f -eq 3) { $tf = 5 }; $g.DrawImage($tele, (New-Object System.Drawing.Rectangle ($x + $c / 2 - 96), ($y + ($sole - 0) * $K - 96), 192, 192), (New-Object System.Drawing.Rectangle ($tf * 128), 0, 128, 128), [System.Drawing.GraphicsUnit]::Pixel) }
            if ($a -eq 4 -and $f -ge 2) { $g.DrawImage($crack, (New-Object System.Drawing.Rectangle ($x + $c / 2 - 72), ($y + $sole * $K - 36), 144, 72)) }
            $sw = [int]($S * 0.42 * $K); $g.FillEllipse($shadow, ($x + $c / 2 - $sw / 2), ($y + ($sole - 2) * $K), $sw, 5 * $K)
            $src = New-Object System.Drawing.Rectangle ($f * $S), (($a * 3 + $vi) * $S), $S, $S
            $g.DrawImage($sheet, (New-Object System.Drawing.Rectangle $x, $y, $c, $c), $src, [System.Drawing.GraphicsUnit]::Pixel); $g.ResetClip()
        }
    }
    $b.Save((Join-Path $Out "Cyclops_REVIEW_$($views[$vi]).png")); $g.Dispose(); $b.Dispose()
}
$sheet.Dispose(); $tele.Dispose(); $crack.Dispose(); "ok"
