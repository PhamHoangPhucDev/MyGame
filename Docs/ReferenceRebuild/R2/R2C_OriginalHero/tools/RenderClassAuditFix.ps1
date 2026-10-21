# Class art audit fixes (run AFTER the class exports and RenderBackGearExport.ps1):
#  1. Vex skill VFX: Gear/Vex_ShadowStep.png, Gear/Vex_BladeDance_Back.png, Gear/Vex_BladeDance_Front.png;
#     projectile hits: unity_export_lyra/Gear/Lyra_ArrowHit.png, unity_export_vex/Gear/Vex_KnifeHit.png
#  2. Death Up/Side frames + anchor rows for Aren, Vex, Brom, Lyra, Selene
#  3. AREN_MANIFEST.csv and VEX_MANIFEST.csv; Death rows added to the Brom/Lyra/Selene manifests
#  4. review board ..\roster\DEATH_VIEWS_REVIEW.png
$D = Split-Path $PSScriptRoot -Parent
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\FxGen.cs", "$D\tools\VfxConceptGen.cs", "$D\tools\VexGen.cs", "$D\tools\VexEquip.cs", "$D\tools\VexVfxGen.cs", "$D\tools\HitVfxGen.cs", "$D\tools\DeathViewsGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing

# 1. Vex VFX
$vg = Join-Path $D "unity_export_vex\Gear"
([VexVfxGen]::ShadowStep()).Save((Join-Path $vg "Vex_ShadowStep.png"))
([VexVfxGen]::BladeDance(0)).Save((Join-Path $vg "Vex_BladeDance_Back.png"))
([VexVfxGen]::BladeDance(1)).Save((Join-Path $vg "Vex_BladeDance_Front.png"))
([HitVfxGen]::KnifeHit()).Save((Join-Path $vg "Vex_KnifeHit.png"))
([HitVfxGen]::ArrowHit()).Save((Join-Path $D "unity_export_lyra\Gear\Lyra_ArrowHit.png"))

# 2. Death views
$classes = @(@("Aren", "unity_export"), @("Vex", "unity_export_vex"), @("Brom", "unity_export_brom"), @("Lyra", "unity_export_lyra"), @("Selene", "unity_export_selene"))
foreach ($c in $classes) { [DeathViewsGen]::Run($c[0], (Join-Path $D $c[1])) }

# 3. manifests
$H = "file,cell_w,cell_h,cols,rows,pivot_px_x,pivot_px_y_from_top,unity_pivot_x,unity_pivot_y,layout"
function ExtraRows($cls) {
    $r = @()
    foreach ($v in "Down", "Up", "Side") { foreach ($f in 0, 1) { $r += "Extra/${cls}_Hurt_${v}_$f.png,32,32,1,1,16,30,0.5,0.0625,""hurt: 0 white flash, 1 red tint""" } }
    foreach ($v in "Down", "Up", "Side") { foreach ($f in 0, 1, 2) { $r += "Extra/${cls}_Death_${v}_$f.png,32,32,1,1,16,30,0.5,0.0625,""death: 0 kneel, 1 fall, 2 lying (hold); Side faces left""" } }
    return $r
}
$aren = @($H,
    "Aren_Sheet.png,32,32,4,9,16,30,0.5,0.0625,""rows Idle D/U/S, Walk D/U/S, Attack D/U/S (Attack 3 cols); Side faces left; anchors Aren_Anchors.csv""") + (ExtraRows "Aren") + @(
    "Gear/Aren_Sword.png,48,48,9,1,24,24,0.5,0.5,""cells 0-7 poses R UR U UL L DL D DR, 8 = back hilt peek (Down idle/walk/hurt); pivot = hand""",
    "Gear/Aren_Shield.png,16,16,3,1,8,8,0.5,0.5,""Down (face) / Up (straps) / Side (edge, faces left)""",
    "Gear/Aren_Hand.png,4,4,1,1,1,1,0.25,0.75,""gauntlet over the sword grip""",
    "Gear/Aren_Slash.png,48,48,6,1,24,24,0.5,0.5,""combo crescent: cell = view*2 + frame (views Down/Up/Side; Side faces left); placed like the imported version""",
    "Skills/Aren_SkillSheet.png,32,32,4,13,16,30,0.5,0.0625,""skill body frames; rows in Skills/AREN_SKILL_LAYOUT.csv; anchors Skills/Aren_SkillAnchors.csv""",
    "../aren_kit/sheets/*,-,-,-,-,-,-,-,-,""skill VFX sheets live in aren_kit/sheets with their own AREN_KIT_VFX_MANIFEST.csv""")
[System.IO.File]::WriteAllLines((Join-Path $D "unity_export\AREN_MANIFEST.csv"), [string[]]$aren, (New-Object System.Text.UTF8Encoding $false))
$vex = @($H,
    "Vex_Sheet.png,32,32,4,9,16,30,0.5,0.0625,""rows Idle D/U/S, Walk D/U/S, Attack D/U/S (Attack 3 cols); Side faces left; anchors Vex_Anchors.csv (off_dir column for the second dagger)""") + (ExtraRows "Vex") + @(
    "Gear/Vex_Dagger.png,48,48,10,1,24,24,0.5,0.5,""cells 0-7 poses R UR U UL L DL D DR, 8-9 = back peeks of the crossed daggers; pivot = hand; main and off hand share it""",
    "Gear/Vex_Slash.png,48,48,24,1,24,24,0.5,0.5,""combo slashes: cell = step*6 + view*2 + frame (4 steps, views Down/Up/Side; Side faces left)""",
    "Gear/Vex_Knife.png,32,32,8,1,16,16,0.5,0.5,""3 Phi Dao: thrown knife + red streak, cols R UR U UL L DL D DR (drawn, never rotated)""",
    "Gear/Vex_Smoke.png,32,32,4,1,16,16,0.5,0.5,""V Bom Khoi: smoke cloud, frames 0-3""",
    "Gear/Vex_ExecuteX.png,48,48,2,1,24,24,0.5,0.5,""C Doat Menh: big crimson X, 0 strike, 1 afterglow""",
    "Gear/Vex_ShadowStep.png,32,32,4,1,16,30,0.5,0.0625,""2 Buoc Bong: ink flame column, play at the departure point AND at the arrival point; 0 burst+eyes, 1 peak, 2 break, 3 wisps; pivot = feet""",
    "Gear/Vex_BladeDance_Back.png,64,48,6,1,32,28,0.5,0.4167,""4 Vu Dieu Luoi Dao: frame = hit 1-6 (0.1 s each); BEHIND the body; pivot = body centre (feet + 0.125 unit up)""",
    "Gear/Vex_BladeDance_Front.png,64,48,6,1,32,28,0.5,0.4167,""4 Vu Dieu Luoi Dao: same frames; IN FRONT of the body""",
    "Gear/Vex_KnifeHit.png,24,24,4,1,12,12,0.5,0.5,""3 Phi Dao: knife impact on an enemy (silver X spark, crimson slash, blood drops); pivot = hit point; 0.05 s/frame, in front of the enemy""",
    "Skills/Vex_SkillSheet.png,32,32,4,13,16,30,0.5,0.0625,""skill body frames; rows in Skills/VEX_SKILL_LAYOUT.csv; anchors Skills/Vex_SkillAnchors.csv""")
[System.IO.File]::WriteAllLines((Join-Path $D "unity_export_vex\VEX_MANIFEST.csv"), [string[]]$vex, (New-Object System.Text.UTF8Encoding $false))
foreach ($c in @(@("Brom", "unity_export_brom\BROM_MANIFEST.csv"), @("Lyra", "unity_export_lyra\LYRA_MANIFEST.csv"), @("Selene", "unity_export_selene\SELENE_MANIFEST.csv"))) {
    $p = Join-Path $D $c[1]
    $lines = @(Get-Content -Encoding UTF8 $p | Where-Object { $_ -notmatch "_Death_(Up|Side)_" -and $_ -notmatch "Lyra_ArrowHit" })
    $new = @(); foreach ($v in "Up", "Side") { foreach ($f in 0, 1, 2) { $new += "Extra/$($c[0])_Death_${v}_$f.png,32,32,1,1,16,30,0.5,0.0625,""death: 0 kneel, 1 fall, 2 lying (hold); Side faces left""" } }
    if ($c[0] -eq "Lyra") { $new += "Gear/Lyra_ArrowHit.png,24,24,4,2,12,12,0.5,0.5,""arrow impact on an enemy: row 0 normal, row 1 charged (pierce shot, play at each enemy hit); pivot = hit point; 0.05 s/frame, in front of the enemy""" }
    [System.IO.File]::WriteAllLines($p, [string[]]($lines + $new), (New-Object System.Text.UTF8Encoding $false))
}

# 4. review board: rows = class, cols = Down 0-2 | Up 0-2 | Side 0-2 (body only; gear follows the anchors in Unity)
$grass = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87))
$font = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold)
$K = 4; $c = 32 * $K
$b = New-Object System.Drawing.Bitmap (90 + 9 * ($c + 4) + 2 * 12), (40 + 5 * ($c + 6)); $g = [System.Drawing.Graphics]::FromImage($b)
$g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 32)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$g.DrawString("Death 3 huong (x4): Down 0-2 (da co) | Up 0-2 (moi) | Side 0-2 (moi, quay trai) - than; vu khi gan theo anchor", $font, [System.Drawing.Brushes]::Yellow, 8, 8)
for ($i = 0; $i -lt 5; $i++) {
    $cls = $classes[$i][0]; $y = 36 + $i * ($c + 6)
    $g.DrawString($cls, $font, [System.Drawing.Brushes]::Yellow, 8, ($y + $c / 2 - 8))
    $col = 0
    foreach ($v in "Down", "Up", "Side") {
        foreach ($f in 0, 1, 2) {
            $x = 90 + $col * ($c + 4) + [math]::Floor($col / 3) * 12
            $g.FillRectangle($grass, $x, $y, $c, $c)
            $img = [System.Drawing.Bitmap]::FromFile((Join-Path $D "$($classes[$i][1])\Extra\${cls}_Death_${v}_$f.png"))
            $g.DrawImage($img, (New-Object System.Drawing.Rectangle $x, $y, $c, $c)); $img.Dispose(); $col++
        }
    }
}
$b.Save((Join-Path $D "roster\DEATH_VIEWS_REVIEW.png")); "ok"
