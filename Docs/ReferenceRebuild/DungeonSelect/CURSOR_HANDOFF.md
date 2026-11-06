# Handoff to Cursor: build the dungeon stage-select screen

Owner: Cursor (Unity code, scene, tests). Art and spec are done by Claude and approved by the user.

Read first:
- Spec: `Docs/superpowers/specs/2026-11-05-dungeon-stage-select-design.md`
- Art list, 9-slice borders, pin centres: `Docs/ReferenceRebuild/DungeonSelect/MANIFEST.md`
- Target look: `Docs/ReferenceRebuild/DungeonSelect/preview/DungeonSelect_GoblinCamp_Preview.png`

## Current gap (checked 2026-11-06)
1. `UI_Dungeon_*` PNGs exist only in `Docs/ReferenceRebuild/DungeonSelect/export/`, not in `Assets/`.
2. `Assets/Resources/UI/UiStyleCatalog.asset` has no `UI_Dungeon_*` entries, so `UiKit.Get("UI_Dungeon_...")` returns null at runtime (and in the Editor, since the files are not in Assets).
3. There is no stage-select panel, no dungeon/stage data, and the guard NPC does not open anything.
4. This PR branch was cut before base commit `5426614` (which added `UiKit.cs`, `UiStyleCatalog`, `CampMap`). Build on top of current `reference_rebuild_r2c`.

## Steps
1. **Import**: copy `export/*.png` into `Assets/Art/UI/Game01/Dungeon/`. Settings like the WorldMap sprites: Sprite (Single), Point filter, no compression, no mipmaps, PPU as WorldMap. Set 9-slice borders: Tab, Tab_Selected, Tab_Locked, GuardStrip = 2 px; RewardSlot = 3 px.
2. **Catalog**: add every `UI_Dungeon_*` sprite to `UiStyleCatalog.asset` the same way `UI_WorldMap_*` entries were added.
3. **Data**: a dungeon has a name, 5 stages and its own saved progress (highest cleared stage), separate from the world-map level. Each stage has: map type (Winding / Maze / Field / Ledge, or Boss for stage 5), display name, recommended level, rewards, guard line, and a talk sequence (lines of speaker + text + optional action such as "spawn enemies" or "boss jumps down"). Goblin Camp order: Winding, Maze, Field, Ledge, Boss.
4. **Panel**: `DungeonSelectPanel`, built like `WorldMapPanel` (open / close / select / travel, pin states, fog fade, test hooks). Board art at 3x, pins at the MANIFEST centres, stage 1-4 pins reuse `UI_WorldMap_Pin_*`, stage 5 uses `UI_Dungeon_Pin_Boss(_Locked)`. Fog layer `UI_Dungeon_Fog_GoblinCamp_StageN` is shown while stage N is locked. Map-type badge under each pin. Side panel reuses `UI_WorldMap_SidePanel` and `UI_WorldMap_Button_Travel`; button reads "Dịch chuyển", "Vào lại" (cleared) or "Bị khoá" (locked, not interactable). Guard strip shows the selected stage's guard line. Open on the highest unlocked stage. Landscape 16:9 only.
5. **Guard NPC**: talking to the camp guard opens the panel. The panel cannot be opened from inside a stage.
6. **Travel**: load the stage, then play its talk sequence before combat starts. Clearing stage N unlocks N+1; clearing stage 5 unlocks the next dungeon tab.
7. **Tests** (like `WorldMapRulesTests`): pin sprite per state, fog on/off per state, travel enabled only for unlocked stages, unlock chain 1→5, catalog contains every `UI_Dungeon_*` name.

## Done when
- Panel matches the preview at 1920x1080 with real sprites, no missing-sprite fallbacks.
- Tests pass in Unity and the result is reported as VERIFIED with evidence.
