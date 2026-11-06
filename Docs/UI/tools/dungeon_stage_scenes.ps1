# In-run mockups v3 for the 5 Goblin Camp stages.
# World: painted per pixel (organic grass/dirt edges from a smoothed tile field + value noise), game tiles as textures,
# game sprites for props/units, then a per-stage lighting pass (ambient, point lights, dapple, vignette).
# UI: kit sprites on the 640x360 canvas x3. Text: VT323 at 1920x1080. Minimap is painted from the same tile map.
# Note: PowerShell names are case-insensitive, so globals use long names (MapW, MapH, KitDir, ProjDir...).
param([string]$Game = "D:\My Doc You\Game01\My project", [string]$Out = "Docs/UI/mockups/landscape/dungeon_v2")
Add-Type -AssemblyName System.Drawing
$ProjDir = (Resolve-Path "$PSScriptRoot\..\..\..").Path
$KitDir = "$Game\Assets\Art\UI\Game01"; $ArtDir = "$Game\Assets\Art"; $StageDir = "$ProjDir\Docs\WorldArt\DungeonStages"
$R2Dir = "$Game\Docs\ReferenceRebuild\R2\R2C_OriginalHero"
New-Item -ItemType Directory -Force "$ProjDir\$Out" | Out-Null
$fontCol = New-Object System.Drawing.Text.PrivateFontCollection
$fontCol.AddFontFile("$Game\Assets\Resources\Fonts\VT323-Regular.ttf"); $fontFam = $fontCol.Families[0]

function C([string]$hex, [int]$a = 255) { [System.Drawing.Color]::FromArgb($a, [Convert]::ToInt32($hex.Substring(1,2),16), [Convert]::ToInt32($hex.Substring(3,2),16), [Convert]::ToInt32($hex.Substring(5,2),16)) }
function Brush([string]$hex, [int]$a = 255) { New-Object System.Drawing.SolidBrush (C $hex $a) }
$imgCache = @{}
function Img([string]$path) { if (-not $imgCache.ContainsKey($path)) { $imgCache[$path] = [System.Drawing.Image]::FromFile($path) }; $imgCache[$path] }
function Kit([string]$name) { $f = Get-ChildItem $KitDir -Recurse -Filter "$name.png" | Select-Object -First 1; Img $f.FullName }
function G($bmp) { $gr = [System.Drawing.Graphics]::FromImage($bmp); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'; $gr }
function Blit($gr, $img, [int]$x, [int]$y, [int]$dw = -1, [int]$dh = -1) { if ($dw -lt 0) { $dw = $img.Width; $dh = $img.Height }; $gr.DrawImage($img, (New-Object System.Drawing.Rectangle $x, $y, $dw, $dh), 0, 0, $img.Width, $img.Height, [System.Drawing.GraphicsUnit]::Pixel) }
function Part($gr, $img, [int]$x, [int]$y, [int]$sx, [int]$sy, [int]$sw, [int]$sh, [int]$dw = -1, [int]$dh = -1) { if ($dw -lt 0) { $dw = $sw; $dh = $sh }; $gr.DrawImage($img, (New-Object System.Drawing.Rectangle $x, $y, $dw, $dh), $sx, $sy, $sw, $sh, [System.Drawing.GraphicsUnit]::Pixel) }
function Slice($gr, $img, [int]$x, [int]$y, [int]$dw, [int]$dh, [int]$l, [int]$t, [int]$r, [int]$b) {
    $iw = $img.Width; $ih = $img.Height
    $sx = @(0, $l, ($iw - $r)); $sw = @($l, ($iw - $l - $r), $r); $sy = @(0, $t, ($ih - $b)); $sh = @($t, ($ih - $t - $b), $b)
    $dx = @($x, ($x + $l), ($x + $dw - $r)); $ddw = @($l, ($dw - $l - $r), $r); $dy = @($y, ($y + $t), ($y + $dh - $b)); $ddh = @($t, ($dh - $t - $b), $b)
    for ($j = 0; $j -lt 3; $j++) { for ($i = 0; $i -lt 3; $i++) {
        if ($sw[$i] -le 0 -or $sh[$j] -le 0 -or $ddw[$i] -le 0 -or $ddh[$j] -le 0) { continue }
        Part $gr $img $dx[$i] $dy[$j] $sx[$i] $sy[$j] $sw[$i] $sh[$j] $ddw[$i] $ddh[$j] } }
}
$inkCol = C '#141018'
function T($gr, [string]$s, [double]$x, [double]$y, [double]$size, $col, [string]$align = 'L', [double]$bw = 0) {
    $f = New-Object System.Drawing.Font $fontFam, ([float]($size * 3)), ([System.Drawing.GraphicsUnit]::Pixel)
    $fmt = New-Object System.Drawing.StringFormat
    $fmt.Alignment = switch ($align) { 'C' { 'Center' } 'R' { 'Far' } default { 'Near' } }
    $rw = if ($bw -le 0) { 3000 } else { [float]($bw * 3) }
    $rect = New-Object System.Drawing.RectangleF ([float]($x * 3)), ([float]($y * 3)), $rw, ([float]($size * 3 + 12))
    $ib = New-Object System.Drawing.SolidBrush $inkCol
    foreach ($o in @(@(-3,0),@(3,0),@(0,-3),@(0,3),@(-3,-3),@(3,-3),@(-3,3),@(3,3),@(0,6),@(3,6),@(-3,6))) {
        $gr.DrawString($s, $f, $ib, (New-Object System.Drawing.RectangleF ($rect.X + $o[0]), ($rect.Y + $o[1]), $rect.Width, $rect.Height), $fmt) }
    $gr.DrawString($s, $f, (New-Object System.Drawing.SolidBrush $col), $rect, $fmt)
}
function Hash([int]$x, [int]$y, [int]$seed) { $n = ($x * 374761393 + $y * 668265263 + $seed * 1442695041) -band 0x7fffffff; $n = (($n -bxor ($n -shr 13)) * 1274126177) -band 0x7fffffff; ($n % 1000) / 1000.0 }
function Rot($img, [string]$how) { $c = $img.Clone(); $c.RotateFlip([System.Drawing.RotateFlipType]$how); $c }
function Frame($sheet, [int]$col, [int]$row, [int]$fw, [int]$fh) { $b = New-Object System.Drawing.Bitmap -ArgumentList $fw, $fh; $gr = G $b; Part $gr $sheet 0 0 ($col * $fw) ($row * $fh) $fw $fh; $gr.Dispose(); $b }
function Tex($img, [int]$ox = 0, [int]$oy = 0) { $b = [System.Drawing.Bitmap]$img; $a = New-Object 'long[]' 256; for ($y = 0; $y -lt 16; $y++) { for ($x = 0; $x -lt 16; $x++) { $a[$y * 16 + $x] = [long]($b.GetPixel($ox + $x, $oy + $y).ToArgb()) -band 0xFFFFFFFFL } }; ,$a }
function LockPx($bmp) { $rc = New-Object System.Drawing.Rectangle 0, 0, $bmp.Width, $bmp.Height; $d = $bmp.LockBits($rc, 'ReadWrite', ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)); $arr = New-Object 'byte[]' ($d.Stride * $bmp.Height); [System.Runtime.InteropServices.Marshal]::Copy($d.Scan0, $arr, 0, $arr.Length); @{ data = $d; px = $arr } }
function UnlockPx($bmp, $lk) { [System.Runtime.InteropServices.Marshal]::Copy($lk.px, 0, $lk.data.Scan0, $lk.px.Length); $bmp.UnlockBits($lk.data) }
$Gold = C '#f2d84a'; $White = C '#f4f4f0'; $Cyan = C '#5ed0e0'; $Mute = C '#aeb6c8'; $Red = C '#e04848'; $Orange = C '#f08a3a'

# ---------- art ----------
$grassTex = @('GrassSoft','GrassSoft_N1','GrassSoft_N2','GrassSoft_N3') | % { ,(Tex (Img "$ArtDir\Tiles\$_.png")) }
$dirtTex = Tex (Img "$ArtDir\Tiles\Dirt.png")
$redTex = Tex (Img "$StageDir\Stage1\Stage1_RedDirtPath_9s.png") 16 16
$fenceImg = Img "$StageDir\Stage2\Stage2_Fence_Autotile_4x4.png"; $stoneImg = Img "$StageDir\Stage2\Stage2_StoneWall_Autotile_4x4.png"; $wallImgCur = $fenceImg; $miniWall = @('#5a3a20','#4e3220')
$gateImg = Img "$StageDir\Stage2\Stage2_FenceGate_2f.png"; $spikeImg = Img "$StageDir\Stage2\Stage2_Spikes.png"; $chestImg = Img "$StageDir\Stage2\Stage2_Chest_2f.png"
$bridgeImg = Img "$StageDir\Stage4\Stage4_Bridge_Tiles_4x16x32.png"
$ringImg = Img "$StageDir\Stage5\Stage5_RitualRing.png"; $teleImg = Img "$StageDir\Stage5\Stage5_RitualTelegraph_Sheet_4f.png"
$treeImgs = @('Tree_Forest_A','Tree_Forest_B','Tree_Forest_C') | % { Img "$ArtDir\GoblinCampPhase4\$_.png" }
$heroSheet = Img "$R2Dir\unity_export\Aren_Sheet.png"
$heroIdle = Frame $heroSheet 0 0 32 32; $heroAtk = Frame $heroSheet 1 6 32 32; $heroAtkSide = Rot (Frame $heroSheet 1 8 32 32) 'RotateNoneFlipX'
function Mon([string]$n, [int]$cell, [int]$row = 0, [int]$col = 0) { Frame (Img "$R2Dir\unity_export_monsters\$n\$($n)_Sheet.png") $col $row $cell $cell }
$gobImg = Mon 'Goblin' 48; $archImg = Mon 'GoblinArcher' 48; $riderImg = Mon 'Gobrider' 48; $eliteImg = Mon 'GobriderElite' 48; $warImg = Mon 'GobriderWarlord' 64
$gobAtk = Mon 'Goblin' 48 6 1; $archAtk = Mon 'GoblinArcher' 48 8 1
$footOf = @{}; foreach ($q in @(@($heroIdle,30),@($heroAtk,30),@($heroAtkSide,30),@($gobImg,38),@($archImg,38),@($gobAtk,38),@($archAtk,38),@($riderImg,42),@($eliteImg,42),@($warImg,58))) { $footOf[$q[0]] = $q[1] }
$slashImg = Frame (Img "$R2Dir\fx\Aren_SlashVFX_Side_v1.png") 0 0 48 48
$arrowSheet = Img "$R2Dir\unity_export_monsters\GoblinArcher\GoblinArcher_Arrow.png"
$shS = Img "$ArtDir\GoblinCampPhase4\Shadow_Small.png"; $shM = Img "$ArtDir\GoblinCampPhase4\Shadow_Medium.png"; $shL = Img "$ArtDir\GoblinCampPhase4\Shadow_Large.png"
$bushImgs = @('BushA','BushB','BushC','BushD','BushE') | % { Img "$ArtDir\Environment\$_.png" }
$rockImgs = @((Img "$ArtDir\Environment\Rock.png"), (Img "$ArtDir\GoblinCampPhase4\Rock_Small.png"), (Img "$ArtDir\GoblinCampPhase4\Rock_Large.png"))
$stumpImg = Img "$ArtDir\Environment\Stump.png"; $bonesImg = Img "$ArtDir\Environment\Bones.png"; $bonePileImg = Img "$ArtDir\GoblinCampPhase4\Bone_Pile.png"
$cratesImg = Img "$ArtDir\GoblinCampPhase4\Wooden_Crate_Stack.png"; $barrelImg = Img "$ArtDir\Environment\Barrel.png"; $crateImg = Img "$ArtDir\Environment\Crate.png"
$pebbleImg = Img "$ArtDir\Tiles\Pebble.png"; $flowerImg = Img "$ArtDir\Tiles\Flower.png"; $flagImg = Img "$ArtDir\GoblinCampPhase4\Goblin_Flag_Tattered.png"
$stakeImg = Img "$ArtDir\Environment\Stake.png"; $brazImg = Img "$ArtDir\GoblinCampPhase4\Brazier_Bowl.png"; $hornImg = Img "$ArtDir\Environment\Tent_GoblinHorn.png"
$tentAImg = Img "$ArtDir\Environment\TentA.png"; $tentBImg = Img "$ArtDir\Environment\TentB.png"; $fireImg = Img "$ArtDir\Environment\Campfire.png"

# ---------- map model ----------
# . grass  d dirt  r red dirt  # fence (dirt under)  g gate (dirt)  K chasm  = bridge row  s courtyard
$MapW = 44; $MapH = 36
function NewMap { $mm = New-Object 'char[,]' $MapW, $MapH; for ($y = 0; $y -lt $MapH; $y++) { for ($x = 0; $x -lt $MapW; $x++) { $mm[$x, $y] = '.' } }; ,$mm }
function At($mm, [int]$x, [int]$y, [char]$def = '.') { if ($x -lt 0 -or $y -lt 0 -or $x -ge $MapW -or $y -ge $MapH) { return $def }; $mm[$x, $y] }
function NearKind($mm, [int]$x, [int]$y, [int]$rad, [string]$kinds) { for ($dy = -$rad; $dy -le $rad; $dy++) { for ($dx = -$rad; $dx -le $rad; $dx++) { if ($kinds.Contains([string](At $mm ($x + $dx) ($y + $dy) ' '))) { return $true } } }; $false }

# ---------- terrain painter ----------
# earth palettes: 4 tones shade->light, rim/outline, pebble light, pebble body
$palDirt = @(0xFFA8662EL, 0xFFC27E3CL, 0xFFD8984CL, 0xFFE8B464L, 0xFF6A3A1EL, 0xFFF4D08AL, 0xFFA88A6AL)
$palRed  = @(0xFF9A4A26L, 0xFFB45E32L, 0xFFC8763EL, 0xFFDC9250L, 0xFF5A2A14L, 0xFFF2C88CL, 0xFFA87C5CL)
$palYard = @(0xFF7A5034L, 0xFF92623EL, 0xFFA8764CL, 0xFFBC8A5AL, 0xFF4E2E1AL, 0xFFE8D4B0L, 0xFFB8A890L)
$bayer = @(0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5) | % { $_ / 16.0 }
function Terrain($mm, [int]$cx, [int]$cy, [string]$floorKind) {
    $bmp = New-Object System.Drawing.Bitmap -ArgumentList 480, 272, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $lk = LockPx $bmp; $px = $lk.px; $stride = $lk.data.Stride
    $fd = New-Object 'double[,]' 34, 21; $fr = New-Object 'double[,]' 34, 21
    for ($j = 0; $j -lt 21; $j++) { for ($i = 0; $i -lt 34; $i++) {
        $c = [string](At $mm ($cx + $i - 2) ($cy + $j - 2))
        if ('ds#g'.Contains($c)) { $fd[$i, $j] = 1 }; if ($c -eq 'r') { $fr[$i, $j] = 1 } } }
    $n1 = New-Object 'double[,]' 52, 31; $n2 = New-Object 'double[,]' 124, 72
    $b1x = [Math]::Floor($cx * 16 / 10); $b1y = [Math]::Floor($cy * 16 / 10); $b2x = [Math]::Floor($cx * 16 / 4); $b2y = [Math]::Floor($cy * 16 / 4)
    for ($j = 0; $j -lt 31; $j++) { for ($i = 0; $i -lt 52; $i++) { $n1[$i, $j] = (Hash ($b1x + $i) ($b1y + $j) 71) * 2 - 1 } }
    for ($j = 0; $j -lt 72; $j++) { for ($i = 0; $i -lt 124; $i++) { $n2[$i, $j] = (Hash ($b2x + $i) ($b2y + $j) 72) * 2 - 1 } }
    $n3 = New-Object 'double[,]' 15, 10; $b3x = [Math]::Floor($cx * 16 / 40); $b3y = [Math]::Floor($cy * 16 / 40)
    for ($j = 0; $j -lt 10; $j++) { for ($i = 0; $i -lt 15; $i++) { $n3[$i, $j] = (Hash ($b3x + $i) ($b3y + $j) 73) * 2 - 1 } }
    for ($py = 0; $py -lt 272; $py++) {
        $wy = $cy * 16 + $py; $tv = $py / 16.0 - 0.5 + 2; $j0 = [int][Math]::Floor($tv); $fv = $tv - $j0; $sv = $fv * $fv * (3 - 2 * $fv)
        for ($pxx = 0; $pxx -lt 480; $pxx++) {
            $wx = $cx * 16 + $pxx; $tu = $pxx / 16.0 - 0.5 + 2; $i0 = [int][Math]::Floor($tu); $fu = $tu - $i0; $su = $fu * $fu * (3 - 2 * $fu)
            $D = ($fd[$i0, $j0] * (1 - $su) + $fd[($i0 + 1), $j0] * $su) * (1 - $sv) + ($fd[$i0, ($j0 + 1)] * (1 - $su) + $fd[($i0 + 1), ($j0 + 1)] * $su) * $sv
            $Rr = ($fr[$i0, $j0] * (1 - $su) + $fr[($i0 + 1), $j0] * $su) * (1 - $sv) + ($fr[$i0, ($j0 + 1)] * (1 - $su) + $fr[($i0 + 1), ($j0 + 1)] * $su) * $sv
            $ax = ($wx / 10.0) - $b1x; $ay = ($wy / 10.0) - $b1y; $ix = [int][Math]::Floor($ax); $iy = [int][Math]::Floor($ay); $gx = $ax - $ix; $gy = $ay - $iy
            $v1 = ($n1[$ix, $iy] * (1 - $gx) + $n1[($ix + 1), $iy] * $gx) * (1 - $gy) + ($n1[$ix, ($iy + 1)] * (1 - $gx) + $n1[($ix + 1), ($iy + 1)] * $gx) * $gy
            $ax = ($wx / 4.0) - $b2x; $ay = ($wy / 4.0) - $b2y; $ix = [int][Math]::Floor($ax); $iy = [int][Math]::Floor($ay); $gx = $ax - $ix; $gy = $ay - $iy
            $v2 = ($n2[$ix, $iy] * (1 - $gx) + $n2[($ix + 1), $iy] * $gx) * (1 - $gy) + ($n2[$ix, ($iy + 1)] * (1 - $gx) + $n2[($ix + 1), ($iy + 1)] * $gx) * $gy
            $nz = $v1 * 0.7 + $v2 * 0.3
            $ti = (($wy -band 15) * 16) + ($wx -band 15)
            $gt = $grassTex[[int][Math]::Floor((Hash ([Math]::Floor($wx / 16)) ([Math]::Floor($wy / 16)) 1) * 3.99)]
            $argb = $gt[$ti]; $mul = 0.95 + 0.09 * $v1
            $hh = Hash $wx $wy 77
            $isRed = $Rr -gt $D; $F = [Math]::Max($D, $Rr)
            $e = $F + 0.27 * $nz - 0.5
            if ($e -gt 0) {
              if (-not $isRed) {
                # plain dirt (stages 2-5, approved look): game Dirt tile + rim shading
                $argb = $dirtTex[$ti]
                if ($e -lt 0.025) { $argb = 0xFF5A4430L; $mul = 1 }
                elseif ($e -lt 0.07) { $mul = 0.8 }
                elseif ($e -lt 0.14) { $mul = 0.92 }
                else { $mul = 1.0 + 0.06 * $v2 }
                if ($floorKind -eq 's') { $mul *= 0.88; if ($hh -lt 0.02) { $argb = 0xFF6E5A48L; $mul = 1 } }
                if ($hh -gt 0.985) { $argb = 0xFF5A4636L; $mul = 1 } elseif ($hh -gt 0.965) { $mul *= 1.12 }
              } else {
                # stage 1 red earth, painted: broad soft light/shade patches (40 px noise) with Bayer dither, shade band under the grass lip
                $pal = $palRed
                $ax = ($wx / 40.0) - $b3x; $ay = ($wy / 40.0) - $b3y; $ix = [int][Math]::Floor($ax); $iy = [int][Math]::Floor($ay); $gx = $ax - $ix; $gy = $ay - $iy
                $gx = $gx * $gx * (3 - 2 * $gx); $gy = $gy * $gy * (3 - 2 * $gy)
                $v3 = ($n3[$ix, $iy] * (1 - $gx) + $n3[($ix + 1), $iy] * $gx) * (1 - $gy) + ($n3[$ix, ($iy + 1)] * (1 - $gx) + $n3[($ix + 1), ($iy + 1)] * $gx) * $gy
                $bay = $bayer[(($wy -band 3) * 4) + ($wx -band 3)]
                $tone = 2.1 + $v3 * 1.25 + $v1 * 0.25 + ($bay - 0.5) * 0.7
                if ($e -lt 0.16) { $tone -= (0.16 - $e) * 14 }
                $argb = $pal[[int][Math]::Max(0, [Math]::Min(3, [Math]::Floor($tone)))]; $mul = 1
                # pebbles on a 12 px grid: rounded, dark outline, lit top-left
                $gxp = [Math]::Floor($wx / 12); $gyp = [Math]::Floor($wy / 12)
                if ($e -gt 0.22 -and (Hash $gxp $gyp 79) -lt 0.16) {
                    $pcx = $gxp * 12 + 3 + [int]((Hash $gxp $gyp 80) * 6); $pcy = $gyp * 12 + 3 + [int]((Hash $gxp $gyp 81) * 6); $prr = 1.2 + (Hash $gxp $gyp 82) * 1.8
                    $ddx = ($wx - $pcx) / $prr; $ddy = ($wy - $pcy) / ($prr * 0.8); $dd = $ddx * $ddx + $ddy * $ddy
                    if ($dd -le 1) { $argb = if ($ddy -lt -0.3 -and $ddx -lt 0.3) { $pal[5] } else { $pal[6] } }
                    elseif ($dd -le 1.9 -and $ddy -gt -0.5) { $argb = $pal[4] } }
              }
            } elseif ($isRed -and $e -gt -0.02) { $argb = 0xFF3C6E2CL; $mul = 1 }
            elseif ($isRed -and $e -gt -0.075) {
                if ($hh -lt 0.5) { $argb = 0xFF9CD460L; $mul = 1 } else { $mul = 0.86 }
            } elseif (-not $isRed -and $e -gt -0.065) {
                if ($hh -lt 0.45) { $argb = 0xFF8CC85AL; $mul = 1 } else { $mul = 0.8 }
            } elseif ($e -gt -0.13 -and $hh -lt $(if ($isRed) { 0.08 } else { 0.1 })) { $argb = $(if ($isRed) { 0xFF9CD460L } else { 0xFF8CC85AL }); $mul = 1 }
            elseif ($hh -gt 0.993) { $argb = 0xFFF2E88AL; $mul = 1 }
            $o = $py * $stride + $pxx * 4
            $px[$o] = [byte][Math]::Min(255, ($argb -band 0xFF) * $mul); $px[$o + 1] = [byte][Math]::Min(255, (($argb -shr 8) -band 0xFF) * $mul); $px[$o + 2] = [byte][Math]::Min(255, (($argb -shr 16) -band 0xFF) * $mul); $px[$o + 3] = 255
        } }
    UnlockPx $bmp $lk
    Chasm $bmp $mm $cx $cy
    $bmp
}
function Chasm($bmp, $mm, [int]$cx, [int]$cy) {
    $any = $false; for ($ty = 0; $ty -lt 17; $ty++) { for ($tx = 0; $tx -lt 30; $tx++) { if ('K='.Contains([string](At $mm ($cx + $tx) ($cy + $ty)))) { $any = $true } } }
    if (-not $any) { return }
    $lk = LockPx $bmp; $px = $lk.px; $stride = $lk.data.Stride
    $pal = @(0xFFA89CB0L, 0xFF847A8EL, 0xFF5E5668L, 0xFF4C4456L, 0xFF3E3848L, 0xFF2A2432L, 0xFF1E1A26L, 0xFF16121CL)
    for ($ty = 0; $ty -lt 17; $ty++) { for ($tx = 0; $tx -lt 30; $tx++) {
        $x = $cx + $tx; $y = $cy + $ty; if (-not 'K='.Contains([string](At $mm $x $y))) { continue }
        $lft = 99; for ($q = 1; $q -le 4; $q++) { if (-not 'K='.Contains([string](At $mm ($x - $q) $y))) { $lft = ($q - 1) * 16; break } }
        $rgt = 99; for ($q = 1; $q -le 4; $q++) { if (-not 'K='.Contains([string](At $mm ($x + $q) $y))) { $rgt = ($q - 1) * 16; break } }
        for ($py = 0; $py -lt 16; $py++) { for ($pxx = 0; $pxx -lt 16; $pxx++) {
            $wx = $x * 16 + $pxx; $wy = $y * 16 + $py
            $jag = 3 * [Math]::Sin($wy / 5.0) + 2 * [Math]::Sin($wy / 2.3 + 1.7) + (Hash 0 ([int][Math]::Floor($wy / 2)) 31) * 2
            $e = [Math]::Min($lft + $pxx, $rgt + (15 - $pxx)) - $jag
            $hh = Hash $wx $wy 33
            if ($e -lt -1) { continue }
            elseif ($e -lt 2) { $ci = if ($hh -lt 0.6) { 0 } else { 1 } }
            elseif ($e -lt 12) { $ci = if ((Hash ([int][Math]::Floor($wx / 2)) ([int][Math]::Floor($wy / 7)) 35) -lt 0.35) { 4 } elseif ($hh -lt 0.5) { 2 } else { 3 } }
            elseif ($e -lt 30) { $ci = if ($hh -lt 0.7) { 5 } else { 6 } }
            else { $ci = if ($hh -lt 0.85) { 7 } else { 6 } }
            $argb = $pal[$ci]
            $mist = [Math]::Sin($wy / 13.0 + $wx / 41.0) + [Math]::Sin($wy / 6.0 - $wx / 23.0) * 0.6
            if ($e -ge 12 -and $mist -gt 0.9) { $argb = if ((($wx + $wy) % 2) -eq 0) { 0xFF4A4060L } else { 0xFF332C44L } }
            $o = ($ty * 16 + $py) * $stride + ($tx * 16 + $pxx) * 4
            $px[$o] = [byte]($argb -band 0xFF); $px[$o + 1] = [byte](($argb -shr 8) -band 0xFF); $px[$o + 2] = [byte](($argb -shr 16) -band 0xFF); $px[$o + 3] = 255 } }
    } }
    UnlockPx $bmp $lk
}
function Fences($gr, $mm, [int]$cx, [int]$cy) {
    for ($ty = -1; $ty -lt 18; $ty++) { for ($tx = -1; $tx -lt 31; $tx++) {
        $x = $cx + $tx; $y = $cy + $ty; if ((At $mm $x $y) -ne '#') { continue }
        $mask = 0; if ((At $mm $x ($y-1)) -eq '#') { $mask += 1 }; if ((At $mm ($x+1) $y) -eq '#') { $mask += 2 }; if ((At $mm $x ($y+1)) -eq '#') { $mask += 4 }; if ((At $mm ($x-1) $y) -eq '#') { $mask += 8 }
        Part $gr $wallImgCur ($tx * 16) ($ty * 16) (($mask % 4) * 16) ([Math]::Floor($mask / 4) * 16) 16 16 } }
}

# ---------- lighting ----------
# amb = @(r,g,b) multipliers; lights = @(@(wx, wy, radius, r, g, b, strength)) in world px; dapple 0..1; vign 0..1
function Light($bmp, [int]$cx, [int]$cy, $amb, $lights, [double]$dapple, [double]$vign) {
    $lk = LockPx $bmp; $px = $lk.px; $stride = $lk.data.Stride
    $ls = @(); foreach ($l in $lights) { $ls += ,@(($l[0] - $cx * 16), ($l[1] - $cy * 16), $l[2], $l[3], $l[4], $l[5], $l[6]) }
    for ($py = 0; $py -lt 272; $py++) { for ($pxx = 0; $pxx -lt 480; $pxx++) {
        $o = $py * $stride + $pxx * 4
        $lr = $amb[0]; $lg = $amb[1]; $lb = $amb[2]
        if ($dapple -gt 0) {
            $wx = $cx * 16 + $pxx; $wy = $cy * 16 + $py
            $dp = [Math]::Sin($wx / 23.0 + $wy / 31.0) * [Math]::Sin($wy / 17.0 - $wx / 29.0) + [Math]::Sin(($wx + $wy) / 13.0) * 0.35
            if ($dp -gt 0.55) { $lr += 0.22 * $dapple; $lg += 0.2 * $dapple; $lb += 0.08 * $dapple } elseif ($dp -lt -0.25) { $lr -= 0.12 * $dapple; $lg -= 0.1 * $dapple; $lb -= 0.04 * $dapple }
        }
        foreach ($l in $ls) {
            $dx = $pxx - $l[0]; $dy = ($py - $l[1]) * 1.35; $d2 = $dx * $dx + $dy * $dy
            if ($d2 -lt $l[2] * $l[2]) { $f = 1 - [Math]::Sqrt($d2) / $l[2]; $f = [Math]::Floor($f * $f * $l[6] * 5) / 5
                $lr += $l[3] * $f; $lg += $l[4] * $f; $lb += $l[5] * $f } }
        if ($vign -ne 0) { $vx = ($pxx - 240) / 240.0; $vy = ($py - 136) / 136.0; $rq = $vx * $vx + $vy * $vy; if ($vign -lt 0) { $sm = [Math]::Min(1, [Math]::Max(0, ($rq - 0.45) / 1.4)); $sm = $sm * $sm * (3 - 2 * $sm); $v = 1 + $vign * 0.7 * $sm } else { $v = 1 - $vign * [Math]::Max(0, $rq - 0.35) }; $lr *= $v; $lg *= $v; $lb *= $v }
        $px[$o] = [byte][Math]::Min(255, [Math]::Max(0, $px[$o] * $lb)); $px[$o + 1] = [byte][Math]::Min(255, [Math]::Max(0, $px[$o + 1] * $lg)); $px[$o + 2] = [byte][Math]::Min(255, [Math]::Max(0, $px[$o + 2] * $lr))
    } }
    UnlockPx $bmp $lk
}
function Motes($gr, [int]$cx, [int]$cy, $list, [string]$hex) {
    foreach ($m0 in $list) { $x = [int]($m0[0] - $cx * 16); $y = [int]($m0[1] - $cy * 16)
        $gr.FillRectangle((Brush $hex 70), $x - 2, $y - 1, 5, 3); $gr.FillRectangle((Brush $hex 70), $x - 1, $y - 2, 3, 5); $gr.FillRectangle((Brush '#fffbe0'), $x, $y, 1, 1) }
}
function MoteField([double]$x0, [double]$y0, [double]$x1, [double]$y1, [int]$n, [int]$seed) { $l = @(); for ($i = 0; $i -lt $n; $i++) { $l += ,@((($x0 + ($x1 - $x0) * (Hash $i 1 $seed)) * 16), (($y0 + ($y1 - $y0) * (Hash $i 2 $seed)) * 16)) }; ,$l }

# ---------- objects ----------
function Obj($img, [double]$tx, [double]$ty, [int]$sc = 1, $sh = $null) { @{ img = $img; x = $tx * 16; y = $ty * 16; sc = $sc; sh = $sh } }
function Objects($gr, $list, [int]$cx, [int]$cy) {
    foreach ($o in ($list | Sort-Object { $_.y })) {
        $ow = $o.img.Width * $o.sc; $oh = $o.img.Height * $o.sc
        $fy = if ($footOf.ContainsKey($o.img)) { $footOf[$o.img] * $o.sc } else { $oh }
        $x = [int]($o.x - $cx * 16 - $ow / 2); $y = [int]($o.y - $cy * 16 - $fy)
        if ($o.sh) { Blit $gr $o.sh ([int]($o.x - $cx * 16 - $o.sh.Width / 2)) ([int]($o.y - $cy * 16 - $o.sh.Height / 2 - 1)) }
        Blit $gr $o.img $x $y $ow $oh }
}
function TreesWhere($mm, [scriptblock]$ok, [int]$seed) {
    $l = @(); for ($y = 1; $y -lt $MapH + 2; $y += 3) { for ($x = -1; $x -lt $MapW + 1; $x += 3) {
        $jx = $x + [int][Math]::Floor((Hash $x $y $seed) * 2); $jy = $y + [int][Math]::Floor((Hash $y $x $seed) * 2)
        if (& $ok $jx $jy) { $l += Obj $treeImgs[[int][Math]::Floor((Hash $jx $jy 3) * 2.99)] ($jx + 0.5) ($jy + 1) 1 $shL } } }
    ,$l
}
function Scatter($mm, [string]$on, [string]$nearKinds, [int]$near, $imgs, [double]$prob, [int]$seed, [double]$hx, [double]$hy) {
    $l = @(); for ($y = 0; $y -lt $MapH; $y++) { for ($x = 0; $x -lt $MapW; $x++) {
        if (-not $on.Contains([string](At $mm $x $y))) { continue }
        if ($nearKinds -ne '' -and -not (NearKind $mm $x $y $near $nearKinds)) { continue }
        if ([Math]::Abs($x - $hx) -lt 2 -and [Math]::Abs($y - $hy) -lt 2) { continue }
        if ((Hash $x $y $seed) -ge $prob) { continue }
        $img = $imgs[[int][Math]::Floor((Hash $y $x ($seed + 1)) * ($imgs.Count - 0.001))]
        $l += Obj $img ($x + 0.2 + (Hash $x $y ($seed + 2)) * 0.6) ($y + 0.6 + (Hash $x $y ($seed + 3)) * 0.4) } }
    ,$l
}
$torchImg = New-Object System.Drawing.Bitmap -ArgumentList 6, 19; $tg = G $torchImg; Blit $tg $stakeImg 0 5
$tg.FillRectangle((Brush '#f08a3a'), 1, 1, 4, 4); $tg.FillRectangle((Brush '#f2d84a'), 2, 2, 2, 3); $tg.FillRectangle((Brush '#fff6c0'), 2, 3, 1, 1); $tg.Dispose()

# ---------- minimap ----------
function Minimap($mm, $treeList, [double]$hx, [double]$hy, $fog = $null) {
    $bm = New-Object System.Drawing.Bitmap -ArgumentList 68, 68
    $ox = [int]$hx - 17; $oy = [int]$hy - 17
    $cols = @{ '.' = @('#5f9e3e','#548f36'); 'd' = @('#b89466','#a8845a'); 'r' = @('#a8704a','#8a5636'); '#' = $miniWall; 'K' = @('#1c1820','#26202a'); '=' = @('#b07840','#8a5a30'); 's' = @('#9a7a58','#8a6c4c') }
    for ($y = 0; $y -lt 68; $y++) { for ($x = 0; $x -lt 68; $x++) {
        $c = [string](At $mm ($ox + [Math]::Floor($x / 2)) ($oy + [Math]::Floor($y / 2)) '.'); if ($c -eq 'g') { $c = 'd' }
        $pp = $cols[$c]; $bm.SetPixel($x, $y, (C ($(if ((Hash $x $y 11) -lt 0.7) { $pp[0] } else { $pp[1] })))) } }
    $gr = G $bm
    foreach ($t in $treeList) { $tx = ($t.x / 16 - $ox) * 2; $ty = ($t.y / 16 - 1 - $oy) * 2
        if ($tx -lt -6 -or $ty -lt -6 -or $tx -gt 74 -or $ty -gt 74) { continue }
        $gr.FillEllipse((Brush '#3e625e'), [int]$tx - 4, [int]$ty - 4, 9, 8); $gr.FillEllipse((Brush '#6a9e98'), [int]$tx - 3, [int]$ty - 3, 7, 6) }
    if ($fog) { for ($y = 0; $y -lt 68; $y++) { for ($x = 0; $x -lt 68; $x++) {
        $a = & $fog ($ox + [Math]::Floor($x / 2)) ($oy + [Math]::Floor($y / 2)) $x $y
        if ($a -gt 0) { $gr.FillRectangle((Brush '#0e1014' $a), $x, $y, 1, 1) } } } }
    $gr.Dispose(); $bm
}

# ---------- HUD ----------
$hudShot = Img "$Game\Docs\UI\screens_runtime\FONT_boss_16x9.png"; $pauseShot = Img "$Game\Docs\UI\screens_runtime\FONT_L2_16x9.png"
$keyAttr = New-Object System.Drawing.Imaging.ImageAttributes; $keyAttr.SetColorKey((C '#36502c'), (C '#3a532f'))
function CopyHud($gr) {
    foreach ($r in @(@(0,0,425,185),@(1600,12,210,100),@(1490,640,430,420))) {
        $gr.DrawImage($hudShot, (New-Object System.Drawing.Rectangle $r[0], $r[1], $r[2], $r[3]), $r[0], $r[1], $r[2], $r[3], [System.Drawing.GraphicsUnit]::Pixel, $keyAttr) }
    Part $gr $pauseShot 1828 24 1828 24 62 70
    $cm = New-Object System.Drawing.Imaging.ColorMatrix; $cm.Matrix33 = 0.55
    $ia = New-Object System.Drawing.Imaging.ImageAttributes; $ia.SetColorMatrix($cm)
    $gr.DrawImage((Kit 'UI_Joystick_Base'), (New-Object System.Drawing.Rectangle 70, 754, 256, 256), 0, 0, 64, 64, [System.Drawing.GraphicsUnit]::Pixel, $ia)
    Blit $gr (Kit 'UI_Joystick_Knob') 142 826 112 112
}
$miniX = 553; $miniY = 38
function DrawMini($u, $mini) {
    Blit $u (Kit 'UI_Minimap_Frame') $miniX $miniY; Blit $u $mini ($miniX + 6) ($miniY + 6)
    Slice $u (Kit 'UI_Header_Ribbon') ($miniX - 6) ($miniY + 80) 92 18 12 0 12 0
    Slice $u (Kit 'UI_NamePlate') ($miniX + 2) ($miniY + 99) 76 15 4 4 4 4
    Blit $u (Kit 'UI_Star_Large_Filled') ($miniX + 36) ($miniY + 101) 12 12
}
function MiniText($gr, [string]$zone, [string]$clock, [string]$star, $clockCol) {
    T $gr $zone ($miniX - 6) ($miniY + 81) 12 $Cyan 'C' 92
    T $gr $clock ($miniX + 6) ($miniY + 100) 12 $clockCol 'L'
    T $gr $star ($miniX + 2) ($miniY + 100) 12 $Gold 'R' 72
}
function TopStrip($u) { Slice $u (Kit 'UI_Header_Ribbon') 266 6 108 18 12 0 12 0; Slice $u (Kit 'UI_Panel_Inset') 226 27 188 14 2 2 2 2 }
function MiniAt($u, [string]$name, [double]$wx, [double]$wy, [double]$hx, [double]$hy) {
    $mx = 34 + ($wx - [int]$hx) * 2; $my = 34 + ($wy - [int]$hy) * 2
    if ($mx -lt 3 -or $my -lt 3 -or $mx -gt 65 -or $my -gt 65) { return }
    Blit $u (Kit $name) ([int](559 + $mx - 3)) ([int](44 + $my - 3)) }
function ScrX([double]$tx, [int]$cx) { ($tx - $cx) * 64 }
function ScrY([double]$ty, [int]$cy) { ($ty - $cy) * 64 }
function Label($gr, [string]$name, [double]$tx, [double]$ty, [int]$cx, [int]$cy, [double]$hp, [int]$headPx) {
    if ($name -eq '') { return }; $x = ScrX $tx $cx; $y = (ScrY $ty $cy) - $headPx * 4 - 10; if ($y -lt 230 -or $x -gt 1600) { return }; if ($y -lt 230 -or $x -gt 1600) { return }
    $f = New-Object System.Drawing.Font $fontFam, 26, ([System.Drawing.GraphicsUnit]::Pixel)
    $fmt = New-Object System.Drawing.StringFormat; $fmt.Alignment = 'Center'
    $col = if ($name.StartsWith('ELITE')) { $Red } else { $White }
    foreach ($o in @(@(-2,0),@(2,0),@(0,-2),@(0,2))) { $gr.DrawString($name, $f, (New-Object System.Drawing.SolidBrush $inkCol), (New-Object System.Drawing.RectangleF ($x - 150 + $o[0]), ($y - 34 + $o[1]), 300, 30), $fmt) }
    $gr.DrawString($name, $f, (New-Object System.Drawing.SolidBrush $col), (New-Object System.Drawing.RectangleF ($x - 150), ($y - 34), 300, 30), $fmt)
    if ($hp -lt 1) { $gr.FillRectangle((Brush '#141018'), [int]($x - 32), [int]$y, 64, 9); $gr.FillRectangle((Brush '#3a1a1e'), [int]($x - 30), [int]($y + 2), 60, 5); $gr.FillRectangle((Brush '#e04848'), [int]($x - 30), [int]($y + 2), [int](60 * $hp), 5) }
}
function Dmg($gr, [string]$s, [double]$tx, [double]$ty, [int]$cx, [int]$cy, $col, [int]$size = 54) {
    $f = New-Object System.Drawing.Font $fontFam, $size, ([System.Drawing.GraphicsUnit]::Pixel)
    $x = ScrX $tx $cx; $y = ScrY $ty $cy
    foreach ($o in @(@(-4,0),@(4,0),@(0,-4),@(0,4),@(-4,-4),@(4,4),@(-4,4),@(4,-4),@(0,7),@(4,7),@(-4,7))) { $gr.DrawString($s, $f, (New-Object System.Drawing.SolidBrush $inkCol), [float]($x + $o[0]), [float]($y + $o[1])) }
    $gr.DrawString($s, $f, (New-Object System.Drawing.SolidBrush $col), [float]$x, [float]$y)
}
function W2([double]$t, [int]$c) { [int](($t - $c) * 16) }

function Render([string]$file, $mm, [string]$floorKind, $objs, [int]$cx, [int]$cy, [scriptblock]$groundFx, [scriptblock]$light, [scriptblock]$airFx, [scriptblock]$uiFx, [scriptblock]$textFx) {
    $world = Terrain $mm $cx $cy $floorKind; $wg = G $world
    Fences $wg $mm $cx $cy
    if ($groundFx) { & $groundFx $wg }
    Objects $wg $objs $cx $cy
    $wg.Dispose()
    & $light $world
    $wg = G $world; if ($airFx) { & $airFx $wg }; $wg.Dispose()
    $big = New-Object System.Drawing.Bitmap -ArgumentList 1920, 1080; $bg = G $big
    $bg.DrawImage($world, (New-Object System.Drawing.Rectangle 0, 0, 1920, 1088), 0, 0, 480, 272, [System.Drawing.GraphicsUnit]::Pixel)
    $bg.TextRenderingHint = 'SingleBitPerPixelGridFit'
    CopyHud $bg
    $ui = New-Object System.Drawing.Bitmap -ArgumentList 640, 360; $u = G $ui
    & $uiFx $u
    $bg.DrawImage($ui, 0, 0, 1920, 1080)
    & $textFx $bg
    $big.Save("$ProjDir\$Out\$file"); $world.Dispose(); $big.Dispose(); $ui.Dispose()
    "saved $file"
}
$only = $env:SCENE_ONLY

# ================= Ải 1 · Đường đất đỏ (forest, dappled sun, fireflies) =================
if (-not $only -or $only -eq '1') {
$m1 = NewMap
function PathX([double]$y) { 21 + 7 * [Math]::Sin(($y - 4) / 4.5) }
for ($y = 0; $y -lt $MapH; $y++) { $c0 = PathX $y; for ($x = 0; $x -lt $MapW; $x++) { if ([Math]::Abs($x - $c0) -le 1.6) { $m1[$x, $y] = 'r' } } }
$clear1 = @(@((PathX 27),27,3.2),@((PathX 13),13,3.0))
foreach ($cl in $clear1) { for ($y = 0; $y -lt $MapH; $y++) { for ($x = 0; $x -lt $MapW; $x++) { if ([Math]::Pow($x - $cl[0], 2) + [Math]::Pow(($y - $cl[1]) * 1.3, 2) -le $cl[2] * $cl[2]) { $m1[$x, $y] = 'r' } } } }
$hy1 = 26.5; $hx1 = (PathX $hy1) + 0.3
$t1 = TreesWhere $m1 { param($x, $y) -not (NearKind $m1 $x $y 2 'r') } 21
$foes1 = @(@($gobAtk,($hx1 + 1.6),($hy1 - 0.2),0.55,'Goblin Lv.3'),@($archAtk,((PathX 20) + 1.2),20,1,'Gob Archer Lv.3'),@($gobImg,((PathX 18) - 0.6),18,1,'Goblin Lv.3'),@($archImg,((PathX 15) + 1.4),15,1,'Gob Archer Lv.3'),@($gobImg,((PathX 13) - 0.4),12.6,1,'Goblin Lv.4'))
$o1 = $t1 + @((Obj $heroAtkSide $hx1 ($hy1 + 0.4) 1 $shM))
foreach ($e in $foes1) { $o1 += Obj $e[0] $e[1] $e[2] 1 $shS }
$o1 += Scatter $m1 '.' 'r' 1 ($bushImgs + $rockImgs + @($stumpImg, $flowerImg, $flowerImg)) 0.2 51 $hx1 $hy1
$o1 += Scatter $m1 'r' '.' 1 @($pebbleImg, $rockImgs[1]) 0.05 59 $hx1 $hy1
$cx1 = [int][Math]::Floor($hx1 - 13); $cy1 = [int][Math]::Floor($hy1 - 11)
$mini1 = Minimap $m1 $t1 $hx1 $hy1
$guide = Rot (Frame (Img "$StageDir\Stage1\Stage1_GuideArrow_Sheet_4f.png") 1 0 16 16) 'Rotate270FlipNone'
Render 'S1_DuongDatDo_run.png' $m1 'd' $o1 $cx1 $cy1 {
    param($wg); foreach ($ay in @(23.2, 21.8)) { Blit $wg $guide ((W2 ((PathX $ay) + 0.5) $cx1) - 8) ((W2 $ay $cy1) - 8) }
} {
    param($bm); Light $bm $cx1 $cy1 @(0.95, 0.95, 0.9) @() 0.8 -0.6
} {
    param($wg)
    Motes $wg $cx1 $cy1 (MoteField ($cx1) ($cy1) ($cx1 + 30) ($cy1 + 17) 26 81) '#d8f070'
    Part $wg $arrowSheet ((W2 ((PathX 20) + 0.6) $cx1) - 14) ((W2 21 $cy1) + 20) 112 0 16 16
    Blit $wg $slashImg ((W2 $hx1 $cx1) + 2) ((W2 $hy1 $cy1) - 30)
} {
    param($u)
    TopStrip $u; DrawMini $u $mini1
    MiniAt $u 'UI_Minimap_Marker_Player' $hx1 $hy1 $hx1 $hy1; foreach ($e in $foes1) { MiniAt $u 'UI_Minimap_Marker_Enemy' $e[1] $e[2] $hx1 $hy1 }
    $gx = [Math]::Max(4, [Math]::Min(64, 34 + ((PathX 1) - $hx1) * 2))
    Blit $u (Kit 'UI_Map_EdgePlate') ([int](559 + $gx - 4)) 41; Blit $u (Kit 'UI_Minimap_Marker_Gate') ([int](559 + $gx - 3)) 42
    Blit $u (Img "$StageDir\Stage1\Stage1_GateEdgeIcon.png") 452 8 24 24
} {
    param($gr)
    T $gr 'ĐỢT 3/4' 266 7 12 $Gold 'C' 108
    T $gr 'ẢI 1 · DỌN ĐỢT RỒI TỚI CỔNG' 226 28 12 $White 'C' 188
    MiniText $gr 'ĐƯỜNG ĐẤT ĐỎ' '01:12' '02:20' $White
    T $gr 'CỔNG' 448 33 10 $Gold 'C' 32
    foreach ($e in $foes1) { Label $gr $e[4] $e[1] $e[2] $cx1 $cy1 $e[3] 16 }
    Dmg $gr '37' ($hx1 + 2.4) ($hy1 - 2.6) $cx1 $cy1 $White
    Dmg $gr '12' ($hx1 - 1.6) ($hy1 - 2.2) $cx1 $cy1 $White 40
}
}

# ================= Ải 2 · Hàng rào nhọn (dusk maze, torches, glowing exit) =================
if (-not $only -or $only -eq '2') {
$wallImgCur = $stoneImg; $miniWall = @('#8e8c9a','#6e6c78')
$m2 = NewMap
$mw = 14; $mh = 10; $ox2 = 7; $oy2 = 7
$rnd = New-Object System.Random 7
for ($y = 0; $y -le 2 * $mh; $y++) { for ($x = 0; $x -le 2 * $mw; $x++) { $m2[($ox2 + $x), ($oy2 + $y)] = '#' } }
$seen = New-Object 'bool[,]' $mw, $mh; $stack = New-Object System.Collections.Stack
$stack.Push(@(0, ($mh - 1))); $seen[0, ($mh - 1)] = $true; $m2[($ox2 + 1), ($oy2 + 2 * ($mh - 1) + 1)] = 'd'
while ($stack.Count -gt 0) {
    $cur = $stack.Peek(); $nb = @()
    foreach ($d in @(@(1,0),@(-1,0),@(0,1),@(0,-1))) { $nx = $cur[0] + $d[0]; $ny = $cur[1] + $d[1]; if ($nx -ge 0 -and $ny -ge 0 -and $nx -lt $mw -and $ny -lt $mh -and -not $seen[$nx, $ny]) { $nb += ,@($nx, $ny, $d[0], $d[1]) } }
    if ($nb.Count -eq 0) { [void]$stack.Pop(); continue }
    $n = $nb[$rnd.Next($nb.Count)]; $seen[$n[0], $n[1]] = $true
    $m2[($ox2 + 2 * $cur[0] + 1 + $n[2]), ($oy2 + 2 * $cur[1] + 1 + $n[3])] = 'd'
    $m2[($ox2 + 2 * $n[0] + 1), ($oy2 + 2 * $n[1] + 1)] = 'd'
    $stack.Push(@($n[0], $n[1]))
}
$m2[($ox2 + 1), ($oy2 + 2 * $mh)] = 'd'
$gateX = $ox2 + 2 * $mw - 1; $m2[$gateX, $oy2] = 'g'
for ($y = $oy2 + 2 * $mh + 1; $y -lt $MapH; $y++) { $m2[($ox2 + 1), $y] = 'd' }
for ($y = 0; $y -lt $oy2; $y++) { $m2[$gateX, $y] = 'd' }
$dead = @(); for ($cy2 = 0; $cy2 -lt $mh; $cy2++) { for ($cx2 = 0; $cx2 -lt $mw; $cx2++) { $x = $ox2 + 2 * $cx2 + 1; $y = $oy2 + 2 * $cy2 + 1; $wc = 0
    foreach ($d in @(@(1,0),@(-1,0),@(0,1),@(0,-1))) { if ((At $m2 ($x + $d[0]) ($y + $d[1])) -eq '#') { $wc++ } }; if ($wc -eq 3) { $dead += ,@($x, $y) } } }
function OpenCell([int]$tx, [int]$ty) {
    for ($rad = 0; $rad -lt 8; $rad++) { for ($dy = -$rad; $dy -le $rad; $dy++) { for ($dx = -$rad; $dx -le $rad; $dx++) {
        $x = $tx + $dx; $y = $ty + $dy; if ((At $m2 $x $y) -ne 'd' -or $y -le $oy2) { continue }
        $isDead = $false; foreach ($d in $dead) { if ($d[0] -eq $x -and $d[1] -eq $y) { $isDead = $true } }
        if (-not $isDead) { return @($x, $y) } } } }
    @($tx, $ty)
}
$hc = OpenCell ($gateX - 7) ($oy2 + 6); $hx2 = $hc[0] + 0.5; $hy2 = $hc[1] + 0.5
$foes2 = @(); foreach ($q in @(@($gobAtk,1,0),@($gobImg,3,-2),@($archImg,6,-4),@($archAtk,-3,2),@($archImg,5,3))) { $cc = OpenCell ($hc[0] + $q[1]) ($hc[1] + $q[2]); $foes2 += ,@($q[0], ($cc[0] + 0.5), ($cc[1] + 0.9)) }
$foes2[0][1] = $hx2 + 1.3; $foes2[0][2] = $hy2 + 0.4
$t2 = TreesWhere $m2 { param($x, $y) -not (NearKind $m2 $x $y 2 '#dg') } 22
$o2 = $t2 + @((Obj $heroAtkSide $hx2 ($hy2 + 0.4) 1 $shM))
foreach ($e in $foes2) { $o2 += Obj $e[0] $e[1] $e[2] 1 $shS }
$o2 += Scatter $m2 'd' '#' 1 @($bonesImg, $pebbleImg, $rockImgs[1]) 0.07 52 $hx2 $hy2
$o2 += Scatter $m2 '.' '#' 2 ($bushImgs + $rockImgs) 0.2 53 $hx2 $hy2
$torch2 = @(); for ($y = $oy2; $y -le $oy2 + 2 * $mh; $y += 2) { for ($x = $ox2; $x -le $ox2 + 2 * $mw; $x += 2) {
    if ((At $m2 $x $y) -eq '#' -and (Hash $x $y 91) -lt 0.24 -and [Math]::Abs($x - $hx2) -lt 16 -and [Math]::Abs($y - $hy2) -lt 10) { $torch2 += ,@(($x + 0.5), ($y + 0.95)) } } }
foreach ($tq in $torch2) { $o2 += Obj $torchImg $tq[0] $tq[1] }
$cx2 = [int][Math]::Floor($hx2 - 15); $cy2 = [int][Math]::Max(0, [Math]::Floor($hy2 - 10))
$explored = { param($tx, $ty) ($tx -le [int]$hx2 + 1) -or ($ty -ge [int]$hy2 + 1) -or $tx -lt $ox2 -or $tx -gt $ox2 + 2 * $mw -or $ty -gt $oy2 + 2 * $mh }
$fog2 = { param($tx, $ty, $mx, $my) if (& $explored $tx $ty) { 0 } elseif ((& $explored ($tx - 1) $ty) -or (& $explored $tx ($ty + 1))) { if ((($mx + $my) % 2) -eq 0) { 200 } else { 110 } } else { 225 } }
$mini2 = Minimap $m2 $t2 $hx2 $hy2 $fog2
$deadIcon = Img "$StageDir\Stage2\Stage2_MiniMap_DeadEnd.png"
Render 'S2_HangRaoNhon_run.png' $m2 'd' $o2 $cx2 $cy2 {
    param($wg)
    $i = 0; foreach ($d in $dead) { $x = ($d[0] - $cx2) * 16; $y = ($d[1] - $cy2) * 16
        if ($i % 3 -eq 0) { Part $wg $chestImg $x $y 0 0 16 16 } else { Blit $wg $spikeImg $x $y }; $i++ }
    Part $wg $gateImg (($gateX - $cx2) * 16) (($oy2 - $cy2) * 16) 0 0 16 16
} {
    param($bm)
    $ls = @(); foreach ($tq in $torch2) { $ls += ,@(($tq[0] * 16), (($tq[1] - 1) * 16), 46, 0.75, 0.5, 0.22, 1.0) }
    $ls += ,@((($gateX + 0.5) * 16), (($oy2 + 0.5) * 16), 70, 0.55, 0.6, 0.2, 1.3)
    $ls += ,@(($hx2 * 16), (($hy2 - 0.5) * 16), 55, 0.25, 0.3, 0.35, 0.8)
    Light $bm $cx2 $cy2 @(0.58, 0.62, 0.86) $ls 0 0.75
} {
    param($wg)
    Motes $wg $cx2 $cy2 (MoteField ($gateX - 2) ($oy2 - 1) ($gateX + 3) ($oy2 + 3) 10 82) '#f2d84a'
    Motes $wg $cx2 $cy2 (MoteField ($cx2) ($cy2) ($cx2 + 30) ($cy2 + 17) 12 83) '#9ad8f0'
    Part $wg $arrowSheet (W2 ($foes2[3][1] + 0.8) $cx2) (W2 ($foes2[3][2] - 1.2) $cy2) 0 0 16 16
    Blit $wg $slashImg ((W2 $hx2 $cx2) + 2) ((W2 $hy2 $cy2) - 30)
} {
    param($u)
    TopStrip $u; DrawMini $u $mini2
    MiniAt $u 'UI_Minimap_Marker_Player' $hx2 $hy2 $hx2 $hy2; foreach ($e in $foes2) { MiniAt $u 'UI_Minimap_Marker_Enemy' $e[1] $e[2] $hx2 $hy2 }
    $ox = [int]$hx2 - 17; $oy = [int]$hy2 - 17
    foreach ($d in $dead) { $mx = ($d[0] - $ox) * 2; $my = ($d[1] - $oy) * 2; if ($mx -ge 2 -and $my -ge 2 -and $mx -lt 66 -and $my -lt 66 -and (& $explored $d[0] $d[1])) { Blit $u $deadIcon (559 + $mx - 3) (44 + $my - 3) 7 7 } }
    Blit $u (Img "$StageDir\Stage2\Stage2_ExitCompass.png") 544 104
} {
    param($gr)
    T $gr 'ĐỢT 2/4' 266 7 12 $Gold 'C' 108
    T $gr 'ẢI 2 · TÌM CỔNG RA MÊ CUNG' 226 28 12 $White 'C' 188
    MiniText $gr 'HÀNG RÀO NHỌN' '01:48' '02:40' $White
    $names = @('Goblin Lv.5','Goblin Lv.5','Gob Archer Lv.5','Gob Archer Lv.5','Gob Archer Lv.6'); $hps = @(0.4, 1, 1, 0.7, 1)
    for ($i = 0; $i -lt 5; $i++) { Label $gr $names[$i] $foes2[$i][1] $foes2[$i][2] $cx2 $cy2 $hps[$i] 16 }
    Dmg $gr '64' ($hx2 + 2.2) ($hy2 - 2.6) $cx2 $cy2 $Gold 64
}
}

# ================= Ải 3 · Bãi lửa trại (evening, big campfire light, embers) =================
$wallImgCur = $fenceImg; $miniWall = @('#5a3a20','#4e3220')
if (-not $only -or $only -eq '3') {
$m3 = NewMap
for ($y = 0; $y -lt $MapH; $y++) { for ($x = 0; $x -lt $MapW; $x++) {
    if ([Math]::Pow(($x - 22) / 10.5, 2) + [Math]::Pow(($y - 18) / 7.5, 2) -le 1) { $m3[$x, $y] = 'd' }
    if (($x -ge 21 -and $x -le 23 -and ($y -gt 24 -or $y -lt 11)) -or ($y -ge 17 -and $y -le 19 -and ($x -lt 12 -or $x -gt 32))) { $m3[$x, $y] = 'd' } } }
$t3 = TreesWhere $m3 { param($x, $y) -not (NearKind $m3 $x $y 2 'd') } 23
$hx3 = 20.6; $hy3 = 19.6
$foes3 = @(@($gobAtk,22.1,19.9,0.5),@($gobImg,15,19.2,1),@($archImg,16,23,1),@($riderImg,28,18,1),@($gobImg,21,14.2,1),@($eliteImg,31,20.5,1),@($archAtk,13.4,16,1),@($riderImg,25,24.5,1))
$o3 = $t3 + @((Obj $tentAImg 15 13.5), (Obj $tentBImg 29 13.5), (Obj $tentAImg 29 25.6), (Obj $fireImg 22.5 18.0 2), (Obj $heroAtkSide $hx3 ($hy3 + 0.4) 1 $shM))
foreach ($e in $foes3) { $o3 += Obj $e[0] $e[1] $e[2] 1 $shS }
$o3 += @((Obj $cratesImg 17.5 13.8), (Obj $barrelImg 31.5 14), (Obj $crateImg 30.8 26.2), (Obj $bonesImg 24.6 21.6), (Obj $bonePileImg 19.2 16.8), (Obj $flagImg 12.6 21.2), (Obj $flagImg 32.4 21.2))
$o3 += Scatter $m3 '.' 'd' 1 ($bushImgs + $rockImgs + @($stumpImg)) 0.18 54 $hx3 $hy3
$cx3 = 7; $cy3 = 9
$mini3 = Minimap $m3 $t3 $hx3 $hy3
$warnImg = Frame (Img "$StageDir\Stage3\Stage3_SurroundWarn_Sheet_3f.png") 1 0 16 16
$encImg = Frame (Img "$StageDir\Stage3\Stage3_EncircleRing_Sheet_2f.png") 0 0 48 24
Render 'S3_BaiLuaTrai_run.png' $m3 'd' $o3 $cx3 $cy3 {
    param($wg); $wg.FillEllipse((Brush '#2a1a10' 90), (W2 21.2 $cx3), (W2 17.4 $cy3), 36, 18); Blit $wg $encImg ((W2 $hx3 $cx3) - 24) ((W2 ($hy3 + 0.4) $cy3) - 12)
} {
    param($bm)
    Light $bm $cx3 $cy3 @(0.42, 0.44, 0.66) @(@((22.5 * 16), (17.4 * 16), 175, 1.05, 0.62, 0.24, 1.25), @((22.5 * 16), (17.4 * 16), 60, 0.5, 0.3, 0.1, 1.0)) 0 0.85
} {
    param($wg)
    $emb = @(); for ($i = 0; $i -lt 16; $i++) { $emb += ,@(((22.5 + ((Hash $i 1 84) - 0.5) * 2.4) * 16), ((17.6 - (Hash $i 2 84) * 4.5) * 16)) }
    Motes $wg $cx3 $cy3 $emb '#f08a3a'
    Motes $wg $cx3 $cy3 (MoteField ($cx3) ($cy3) ($cx3 + 30) ($cy3 + 5) 10 85) '#d8f070'
    Blit $wg $slashImg ((W2 $hx3 $cx3) + 2) ((W2 $hy3 $cy3) - 30)
} {
    param($u)
    TopStrip $u; DrawMini $u $mini3
    MiniAt $u 'UI_Minimap_Marker_Player' $hx3 $hy3 $hx3 $hy3; foreach ($e in $foes3) { MiniAt $u 'UI_Minimap_Marker_Enemy' $e[1] $e[2] $hx3 $hy3 }
    Blit $u $warnImg 10 172 24 24; Blit $u (Rot $warnImg 'RotateNoneFlipX') 606 160 24 24; Blit $u (Rot $warnImg 'Rotate90FlipNone') 308 50 24 24
} {
    param($gr)
    T $gr 'ĐỢT 4/4' 266 7 12 $Gold 'C' 108
    T $gr 'ẢI 3 · DIỆT HẾT QUÁI' 226 28 12 $White 'C' 188
    T $gr 'ĐỢT 4 — VÒNG VÂY' 170 72 20 $Orange 'C' 300
    MiniText $gr 'BÃI LỬA TRẠI' '02:31' '02:50' $Gold
    $names = @('Goblin Lv.8','Goblin Lv.8','Gob Archer Lv.8','Gobrider Lv.9','Goblin Lv.8','ELITE Gobrider Lv.10','Gob Archer Lv.8','Gobrider Lv.9')
    for ($i = 0; $i -lt $foes3.Count; $i++) { Label $gr $names[$i] $foes3[$i][1] $foes3[$i][2] $cx3 $cy3 $foes3[$i][3] $(if ($i -eq 3 -or $i -eq 5 -or $i -eq 7) { 24 } else { 16 }) }
    Dmg $gr '128' ($hx3 + 2.3) ($hy3 - 2.7) $cx3 $cy3 $Gold 66
    T $gr 'x6 HITS' 360 196 14 $White
}
}

# ================= Ải 4 · Cầu vực (cold mist, outpost torches) =================
if (-not $only -or $only -eq '4') {
$m4 = NewMap
for ($y = 0; $y -lt $MapH; $y++) { for ($x = 15; $x -le 26; $x++) { $m4[$x, $y] = 'K' } }
for ($x = 0; $x -lt 15; $x++) { foreach ($y in 17..19) { $m4[$x, $y] = 'd' } }
for ($y = 10; $y -le 26; $y++) { for ($x = 27; $x -le 40; $x++) { $m4[$x, $y] = 'd' } }
for ($x = 15; $x -le 26; $x++) { $m4[$x, 18] = '=' }
$seq = @(0,1,1,1,3,1,1,1,3,1,1,2)
$t4 = TreesWhere $m4 { param($x, $y) -not (NearKind $m4 $x $y 2 'dK=') } 24
$hx4 = 20.4; $hy4 = 18.3
$foes4 = @(@($eliteImg,22.2,18.9,0.6,'ELITE Gobrider Lv.11'),@($eliteImg,24.8,18.9,1,''),@($archAtk,28.6,15.6,1,'Gob Archer Lv.11'),@($archImg,28.6,22.4,1,'Gob Archer Lv.11'),@($warImg,30.6,18.6,1,'Gobrider Warlord Lv.12'))
$o4 = $t4 + @((Obj $heroAtkSide $hx4 ($hy4 + 0.6) 1 $null), (Obj $flagImg 29 13), (Obj $flagImg 29 25.5), (Obj $torchImg 27.6 16.4), (Obj $torchImg 27.6 21.4), (Obj $cratesImg 33 13), (Obj $barrelImg 31.5 23.8), (Obj $crateImg 34 24.2), (Obj $bonePileImg 32 20.6))
foreach ($e in $foes4) { $o4 += Obj $e[0] $e[1] $e[2] 1 $(if ($e[1] -gt 27) { $shS } else { $null }) }
$o4 += Scatter $m4 '.d' 'K' 1 ($rockImgs + @($pebbleImg)) 0.22 55 $hx4 $hy4
$o4 += Scatter $m4 '.' 'd' 1 ($bushImgs + @($stumpImg)) 0.14 56 $hx4 $hy4
$cx4 = 6; $cy4 = 8
$mini4 = Minimap $m4 $t4 $hx4 $hy4
Render 'S4_CauVuc_run.png' $m4 'd' $o4 $cx4 $cy4 {
    param($wg)
    $wg.FillRectangle((Brush '#0a080c' 150), ((15 - $cx4) * 16 + 4), [int]((19.6 - $cy4) * 16), 184, 9)
    for ($i = 0; $i -lt 12; $i++) { Part $wg $bridgeImg ((15 + $i - $cx4) * 16) ([int]((17.5 - $cy4) * 16)) ($seq[$i] * 16) 0 16 32 }
} {
    param($bm)
    Light $bm $cx4 $cy4 @(0.8, 0.86, 1.0) @(@((27.6 * 16), (15.6 * 16), 50, 0.7, 0.45, 0.2, 1.0), @((27.6 * 16), (20.6 * 16), 50, 0.7, 0.45, 0.2, 1.0)) 0 0.6
} {
    param($wg)
    for ($i = 0; $i -lt 7; $i++) { $y = [int]((Hash $i 1 86) * 260); $x = (15 - $cx4) * 16 + [int]((Hash $i 2 86) * 150); $wg.FillRectangle((Brush '#c8c8e0' 38), $x, $y, 30 + [int]((Hash $i 3 86) * 40), 2) }
    Part $wg $arrowSheet (W2 26.4 $cx4) (W2 17.1 $cy4) 64 0 16 16
    Blit $wg $slashImg ((W2 $hx4 $cx4) + 2) ((W2 $hy4 $cy4) - 30)
} {
    param($u)
    Slice $u (Img "$StageDir\Stage4\Stage4_EdgeDanger_9s.png") 0 0 640 360 16 16 16 16
    Slice $u (Kit 'UI_Header_Ribbon') 266 6 108 18 12 0 12 0
    $bar = Img "$StageDir\Stage4\Stage4_BridgeBar_Frame.png"; $pips = Img "$StageDir\Stage4\Stage4_BridgeBar_Pips_3x18x8.png"
    Blit $u $bar 240 27 160 28
    foreach ($q in @(@(18,0),@(38,1),@(58,2))) { Part $u $pips (240 + $q[0] * 2) 33 ($q[1] * 18) 0 18 8 36 16 }
    DrawMini $u $mini4
    MiniAt $u 'UI_Minimap_Marker_Player' $hx4 $hy4 $hx4 $hy4; foreach ($e in $foes4) { MiniAt $u 'UI_Minimap_Marker_Enemy' $e[1] $e[2] $hx4 $hy4 }
} {
    param($gr)
    T $gr 'ĐỢT 2/3' 266 7 12 $Gold 'C' 108
    T $gr 'ĐOẠN 2/3' 404 32 12 $White
    MiniText $gr 'CẦU VỰC' '02:05' '03:30' $White
    foreach ($e in $foes4) { Label $gr $e[4] $e[1] $e[2] $cx4 $cy4 $e[3] $(if ($e[0] -eq $warImg) { 42 } else { 24 }) }
    Dmg $gr '211' ($hx4 + 0.4) ($hy4 - 3.9) $cx4 $cy4 $Gold 70
}
}

# ================= Ải 5 · Lều xương (violet dusk, braziers, glowing ritual ring) =================
if (-not $only -or $only -eq '5') {
$m5 = NewMap
for ($y = 4; $y -le 32; $y++) { for ($x = 6; $x -le 37; $x++) { $m5[$x, $y] = 's' } }
for ($x = 6; $x -le 37; $x++) { $m5[$x, 4] = '#'; $m5[$x, 32] = '#' }; for ($y = 4; $y -le 32; $y++) { $m5[6, $y] = '#'; $m5[37, $y] = '#' }
foreach ($x in 20..23) { $m5[$x, 32] = 's' }; for ($y = 33; $y -lt $MapH; $y++) { foreach ($x in 20..23) { $m5[$x, $y] = 'd' } }
$t5 = TreesWhere $m5 { param($x, $y) -not (NearKind $m5 $x $y 2 's#d') } 25
$hx5 = 21.6; $hy5 = 22.6; $ringX = 22; $ringY = 16.6
$cx5 = 8; $cy5 = 10
$foes5 = @(@($gobAtk,19.6,19.6,0.35,'Goblin Lv.14'),@($gobImg,25.6,19.8,1,'Goblin Lv.14'))
$o5 = $t5 + @((Obj $hornImg 22 12.2), (Obj $hornImg 12.2 19.2), (Obj $hornImg 29.4 21.8), (Obj $bonesImg 16.8 15), (Obj $bonesImg 26.6 14.6), (Obj $brazImg 15.6 22.4), (Obj $brazImg 27.8 17.6),
    (Obj $warImg $ringX ($ringY + 0.4) 1 $shM), (Obj $heroAtk $hx5 ($hy5 + 0.4) 1 $shM))
foreach ($e in $foes5) { $o5 += Obj $e[0] $e[1] $e[2] 1 $shS }
foreach ($fx5 in @(10.5, 16, 28, 33.5)) { $o5 += Obj $flagImg $fx5 11.4 }
$o5 += Scatter $m5 's' '' 0 @($bonesImg, $pebbleImg, $pebbleImg, $rockImgs[1], $bonePileImg) 0.05 57 $hx5 $hy5
$o5 += Scatter $m5 '.' 's#' 1 ($bushImgs + $rockImgs) 0.25 58 $hx5 $hy5
$mini5 = Minimap $m5 $t5 $hx5 $hy5
$tele = Frame $teleImg 2 0 64 32
Render 'S5_LeuXuong_run.png' $m5 's' $o5 $cx5 $cy5 {
    param($wg)
    for ($i = 0; $i -lt 16; $i++) { $x = [int]((Hash $i 1 61) * 470); $y = [int]((Hash $i 2 61) * 262); $wg.FillEllipse((Brush '#4a3226' 70), $x, $y, 18 + [int]((Hash $i 3 61) * 30), 8 + [int]((Hash $i 4 61) * 10)) }
    $rx = (W2 $ringX $cx5) - 64; $ry = (W2 $ringY $cy5) - 32
    Blit $wg $ringImg $rx $ry 128 64; Blit $wg $tele $rx $ry 128 64
} {
    param($bm)
    Light $bm $cx5 $cy5 @(0.5, 0.4, 0.72) @(@(($ringX * 16), ($ringY * 16), 95, 0.95, 0.2, 0.22, 1.1), @((15.6 * 16), (21.4 * 16), 60, 0.8, 0.5, 0.2, 1.0), @((27.8 * 16), (16.6 * 16), 60, 0.8, 0.5, 0.2, 1.0), @(($hx5 * 16), (($hy5 - 0.5) * 16), 45, 0.2, 0.2, 0.3, 0.8)) 0 0.9
} {
    param($wg)
    Motes $wg $cx5 $cy5 (MoteField ($ringX - 4) ($ringY - 3) ($ringX + 4) ($ringY + 2) 14 87) '#c070f0'
    Motes $wg $cx5 $cy5 (MoteField ($cx5) ($cy5) ($cx5 + 30) ($cy5 + 17) 10 88) '#9a80ff'
    Blit $wg (Rot $slashImg 'Rotate270FlipNone') ((W2 $hx5 $cx5) - 24) ((W2 $hy5 $cy5) - 60)
} {
    param($u)
    Slice $u (Kit 'UI_Header_Ribbon') 266 6 108 18 12 0 12 0
    Blit $u (Kit 'UI_BossBar_Frame') 215 28 210 22
    $u.FillRectangle((Brush '#1a1a22'), 225, 34, 190, 10)
    $fill = Kit 'UI_Bar_Fill_Boss'; $u.DrawImage($fill, (New-Object System.Drawing.Rectangle 225, 34, 108, 10), 0, 0, $fill.Width, $fill.Height, [System.Drawing.GraphicsUnit]::Pixel)
    $u.FillRectangle((Brush '#f0eee6'), 333, 34, 9, 10)
    $notch = Img "$StageDir\Stage5\Stage5_BossPhaseNotch_2f.png"
    Part $u $notch ([int](225 + 190 * 0.66 - 2)) 33 4 0 4 12; Part $u $notch ([int](225 + 190 * 0.33 - 2)) 33 0 0 4 12
    DrawMini $u $mini5
    MiniAt $u 'UI_Minimap_Marker_Player' $hx5 $hy5 $hx5 $hy5; MiniAt $u 'UI_Minimap_Marker_Boss' $ringX $ringY $hx5 $hy5
    foreach ($e in $foes5) { MiniAt $u 'UI_Minimap_Marker_Enemy' $e[1] $e[2] $hx5 $hy5 }
} {
    param($gr)
    T $gr 'ĐỢT 3/3' 266 7 12 $Gold 'C' 108
    T $gr 'TƯỚNG GOBRIDER' 215 50 12 $White 'C' 210
    T $gr 'PHA 2' 404 50 10 $Orange
    MiniText $gr 'LỀU XƯƠNG' '03:58' '04:40' $Gold
    foreach ($e in $foes5) { Label $gr $e[4] $e[1] $e[2] $cx5 $cy5 $e[3] 16 }
    Dmg $gr '96' ($hx5 + 1.0) ($hy5 - 3.1) $cx5 $cy5 $White 56
    T $gr 'ĐÒN NGHI LỄ!' 220 230 14 $Red 'C' 200
}
}
"ok"
