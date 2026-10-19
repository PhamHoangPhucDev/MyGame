# Pixel Art Requests — Phase 12D.2 / 12E

Use for Canva or Adobe Illustrator. Target: Unity 2D top-down, PPU 16, Point filter, palette consistent with Phase 4 Goblin Camp.

## Shared rules
- Orthographic top-down / 3/4-ish matching existing trees/flags (not side-view).
- Transparent PNG, no baked shadows (game has ContactShadow / SpriteSorter).
- Pivot: bottom-center for props that stand on dirt; center for floor decals.
- Max canvas per sprite: keep content inside power-of-two friendly boxes when practical.

## 1. Wooden Fort Gate
- Size: 64×48 px (wide gate) + optional open variant 64×48.
- Content: timber posts, crossbeam, torn purple cloth / bone charms.
- Two lane openings readable at pixel scale (left/right path through).
- Palette: wood browns, iron darks, Phase 4 purple flag accent.

## 2. Broken / Fortified Fence
- Size: 16×24 and 32×24 modular segments (straight, corner, broken gap).
- Content: stakes, rope, scrap wood — matches Gate stakes already in scene.
- Pivot: bottom-center.

## 3. Watchtower
- Size: 48×64 px.
- Content: raised platform + ladder hint, banner hook; not taller than Tree_Forest canopy.
- Must not fully hide combat when placed on map rim.

## 4. Defensive Barricade
- Size: 32×24 px.
- Content: crate+spike mash usable as soft cover on courtyard rim only.

## 5. Gobrider Warlord (Boss body)
- Current logic uses tinted Gobrider sprite (temporary).
- Ideal: 32×32 or 48×48 idle + walk + telegraph pose.
- Distinct silhouette from Elite Gobrider (heavier armor, crest, larger axe/banner).
- Level label text already: `Gobrider Warlord Lv.8`.

## 6. Optional VFX sheets
- Charge arrow / danger stripe 16×64.
- Area ring telegraph 48×48.
- Phase roar burst 32×32.
