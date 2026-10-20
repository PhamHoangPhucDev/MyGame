# Equipment prototype: Aren attack (body + sword + shield by anchors), gear swap demo, other heroes' weapons.
$D = Split-Path $PSScriptRoot -Parent
$O = Join-Path $D "attack"; New-Item -ItemType Directory -Force $O | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
[EquipGen]::Run($O) | Out-Null

$f = New-Object System.Drawing.Font "Consolas", 11, ([System.Drawing.FontStyle]::Bold)
$fs = New-Object System.Drawing.Font "Consolas", 9
$grassC = [System.Drawing.Color]::FromArgb(255, 0x74, 0xA5, 0x87)
$grass = New-Object System.Drawing.SolidBrush $grassC
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(90, 0, 0, 0))
$bg = [System.Drawing.Color]::FromArgb(255, 28, 28, 32)
function NewBoard($w, $h) { $b = New-Object System.Drawing.Bitmap $w, $h; $g = [System.Drawing.Graphics]::FromImage($b); $g.Clear($bg); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'; return @($b, $g) }
function Tile($g, $bmp, $x, $y, $k, $label) {
    $sz = 48 * $k
    $g.FillRectangle($grass, $x, $y, $sz, $sz); $g.FillEllipse($shadow, $x + 18 * $k, $y + 36 * $k, 12 * $k, 3 * $k)
    $g.DrawImage($bmp, (New-Object System.Drawing.Rectangle $x, $y, $sz, $sz), (New-Object System.Drawing.Rectangle 0, 0, 48, 48), [System.Drawing.GraphicsUnit]::Pixel)
    if ($label) { $g.DrawString($label, $fs, [System.Drawing.Brushes]::White, $x + 3, $y + 3) }
}
$heater = [Func[string, EquipGen+Spr]] { param($v) [EquipGen]::HeaterShield($v) }
$round = [Func[string, EquipGen+Spr]] { param($v) [EquipGen]::RoundWoodShield($v) }
$fl = @("F0 windup 0.08s", "F1 strike 0.10s", "F2 recover 0.08s")

# A) Aren attack: 3 views x 3 frames (x5)
$K = 5; $bb = NewBoard 780 840; $b = $bb[0]; $g = $bb[1]
$views = @(@("Down", "Down"), @("Up", "Up (back)"), @("Side", "Side 3/4 right (left = flipX)"))
for ($r = 0; $r -lt 3; $r++) {
    $y = 30 + $r * 270
    $g.DrawString("Aren Attack $($views[$r][1]) - kiem dai thep + khien heater (sprite rieng, gan theo anchor)", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 24)
    for ($i = 0; $i -lt 3; $i++) { Tile $g ([EquipGen]::ArenAttackComposite($views[$r][0], $i, [EquipGen]::SteelLongsword, $heater, $null)) (10 + $i * 255) $y $K $fl[$i] }
}
$b.Save((Join-Path $O "Aren_Attack_v1_review.png"))

# B) swap demo: same body frames, different gear / skin
$K = 4; $bb = NewBoard 1000 560; $b = $bb[0]; $g = $bb[1]
$g.DrawString("Doi trang bi / skin: cung khung than, chi doi sprite vu khi, khien, palette (F1 strike)", $f, [System.Drawing.Brushes]::Yellow, 10, 6)
$combos = @(
    @("Kiem thep + Heater", [EquipGen]::SteelLongsword, $heater, $null),
    @("Kiem han + Heater", [EquipGen]::RuneBlade, $heater, $null),
    @("Kiem thep + Khien go", [EquipGen]::SteelLongsword, $round, $null),
    @("Skin Crimson + Kiem han + Go", [EquipGen]::RuneBlade, $round, [EquipGen]::CrimsonSkin)
)
for ($i = 0; $i -lt 4; $i++) {
    $c = $combos[$i]; $x = 10 + $i * 245
    $g.DrawString($c[0], $fs, [System.Drawing.Brushes]::White, $x, 32)
    Tile $g ([EquipGen]::ArenAttackComposite("Down", 1, $c[1], $c[2], $c[3])) $x 50 $K $null
    Tile $g ([EquipGen]::ArenAttackComposite("Side", 1, $c[1], $c[2], $c[3])) $x 250 $K $null
}
$g.DrawString("Hang tren: Down. Hang duoi: Side.", $fs, [System.Drawing.Brushes]::White, 10, 450)
$b.Save((Join-Path $O "Equip_swap_demo_v1.png"))

# C) weapon pose sheets (8 directions, never rotated at runtime) + shields
$K = 4; $bb = NewBoard 1060 560; $b = $bb[0]; $g = $bb[1]
$rows = @(@("Weapon_sword_steel_8dir.png", "Kiem dai thep - 8 tu the (R UR U UL L DL D DR), anchor = tam o"), @("Weapon_sword_rune_8dir.png", "Kiem han (skin vu khi)"), @("Weapon_dagger_steel_8dir.png", "Dao gam (Vex)"))
for ($r = 0; $r -lt 3; $r++) {
    $s = [System.Drawing.Bitmap]::FromFile((Join-Path $O $rows[$r][0])); $y = 30 + $r * 150
    $g.DrawString($rows[$r][1], $f, [System.Drawing.Brushes]::Yellow, 10, $y - 22)
    $g.FillRectangle($grass, 10, $y, 1032, 129)
    $g.DrawImage($s, (New-Object System.Drawing.Rectangle 10, $y, 1032, 129), (New-Object System.Drawing.Rectangle 0, 0, 384, 48), [System.Drawing.GraphicsUnit]::Pixel)
}
$y = 480; $g.DrawString("Khien heater / khien go: Down | Up | Side", $f, [System.Drawing.Brushes]::Yellow, 10, $y - 22)
$x = 10
foreach ($n in @("heater", "roundwood")) { foreach ($v in @("Down", "Up", "Side")) { $s = [System.Drawing.Bitmap]::FromFile((Join-Path $O "Shield_${n}_$v.png")); $g.FillRectangle($grass, $x, $y - 4, 72, 72); $g.DrawImage($s, (New-Object System.Drawing.Rectangle $x, ($y - 4), 64, 64), (New-Object System.Drawing.Rectangle 0, 0, 16, 16), [System.Drawing.GraphicsUnit]::Pixel); $x += 80 } ; $x += 40 }
$b.Save((Join-Path $O "Equip_weapon_sheets_v1.png"))

# D) other heroes: rest pose with their gear (Idle Down F0)
function HeroBody($id) { $c = [HeroConceptGen]::Roster | Where-Object { $_.Id -eq $id }; return ,([EquipGen]::Body([EquipGen]::Grid($c.Rows), $c.Pal)) }
function L($s, $x, $y) { $l = New-Object 'EquipGen+Layer'; $l.S = $s; $l.X = $x; $l.Y = $y; return $l }
function Lst() { return ,(New-Object 'System.Collections.Generic.List[EquipGen+Layer]') }
$K = 5; $bb = NewBoard 1000 360; $b = $bb[0]; $g = $bb[1]
$g.DrawString("Vu khi mac dinh cua 4 nhan vat con lai (tu the dung, Idle Down F0) - tat ca la sprite rieng, doi duoc", $f, [System.Drawing.Brushes]::Yellow, 10, 6)
# Brom: hammer on the viewer-left hand, big round shield in front on the right
$bh = Lst; $fr = Lst
$bh.Add((L ([EquipGen]::Hammer(2, 7, "7A4E2E", "A06A40", "8A94A4", "C8D0DC", "5A6070", "1C1A22")) 6 24))
$fr.Add((L ([EquipGen]::BromShield()) 19 23))
Tile $g ([EquipGen]::Compose((HeroBody "1"), $bh, $fr)) 10 40 $K "Brom: bua + khien tron lon"
# Lyra: bow in the viewer-right hand (quiver currently painted in the body: separate in next pass)
$bh = Lst; $fr = Lst; $fr.Add((L ([EquipGen]::Bow()) 21 22)); $fr.Add((L ([EquipGen]::Hand("FCDCC0", "E8B494", "2A2218")) 20 22))
Tile $g ([EquipGen]::Compose((HeroBody "2"), $bh, $fr)) 255 40 $K "Lyra: cung cam tay (ong ten sau lung)"
# Selene: staff upright in the viewer-left hand
$bh = Lst; $fr = Lst; $fr.Add((L ([EquipGen]::Staff(2, 14, "6A4428", "9A6A3C", "C04A8A", "FFD0F0", "7A2A5A", "1A1426")) 9 25))
Tile $g ([EquipGen]::Compose((HeroBody "3"), $bh, $fr)) 500 40 $K "Selene: gay phep"
# Vex: twin daggers, reverse grip pointing down-out
$bh = Lst; $fr = Lst
$fr.Add((L ([EquipGen]::Blade([EquipGen]::Dagger, 5)) 10 24)); $fr.Add((L ([EquipGen]::Blade([EquipGen]::Dagger, 7)) 21 24))
Tile $g ([EquipGen]::Compose((HeroBody "5"), $bh, $fr)) 745 40 $K "Vex: doi dao gam"
$b.Save((Join-Path $O "Equip_roster_weapons_v1.png"))
"done"
