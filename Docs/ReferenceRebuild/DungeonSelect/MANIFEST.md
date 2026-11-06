# Dungeon stage select: Goblin Camp (approved 2026-11-06)

Spec: `Docs/superpowers/specs/2026-11-05-dungeon-stage-select-design.md`
Rebuild: `tools/run.ps1` (art), then `tools/preview.ps1` (1920x1080 preview, text is a stand-in for Unity text).
The board candidate was approved by the user on 2026-11-06. Cursor imports these into `Assets/Art/UI/Game01/Dungeon/` with Point filter, no compression, and 9-slice borders as listed.

| File | Size | Notes |
|---|---|---|
| `export/UI_Dungeon_Board_GoblinCamp.png` | 408x308 | Opaque, 145 colours, same size as `UI_WorldMap_Outdoor_Art`. Show at 3x, Point filter. |
| `export/UI_Dungeon_Fog_GoblinCamp_Stage2..5.png` | 408x308 | Purple mist, alpha steps 0/70/120/165/205. Show while that stage is locked; fade out on unlock. |
| `export/UI_Dungeon_Pin_Boss.png` | 13x13 | Stage 5 when open. Same shape as world-map pins, red border, skull. |
| `export/UI_Dungeon_Pin_Boss_Locked.png` | 13x13 | Stage 5 while locked. |
| `export/UI_Dungeon_MapType_Winding / Maze / Field / Ledge.png` | 11x11 | Map-type badge under each pin and in the side panel. |

| `export/UI_Dungeon_Tab.png` / `_Selected` / `_Locked` | 8x8 | 9-slice, border 2 px. Open / selected (gold) / locked dungeon tab. Put the lock pin from the world map at the tab's right end. |
| `export/UI_Dungeon_GuardStrip.png` | 8x8 | 9-slice, border 2 px. Guard dialogue box. |
| `export/UI_Dungeon_RewardSlot.png` | 10x10 | 9-slice, border 3 px. Reward slot; also frames the guard portrait. |
| `export/UI_Dungeon_Guard_Portrait.png` | 24x24 | Camp guard bust (steel helm, red plume, blue tabard). Show at 4x inside a reward slot. |

Side panel, its rows and the Travel button reuse `UI_WorldMap_SidePanel`, `UI_WorldMap_Row(_Selected)` and `UI_WorldMap_Button_Travel`.

Stages 1-4 reuse the world-map pins (`UI_WorldMap_Pin_Cleared / Selected / Unlocked / Locked`, `Pin_Check`, `Pin_SelectHalo`, `YouMarker`) so both maps read the same.

## Pin centres (board pixels, top-left origin)

| Stage | Centre | Goblin Camp map type |
|---|---|---|
| 1 | (54, 262) | Winding road |
| 2 | (146, 181) | Maze (palisade) |
| 3 | (228, 236) | Open field (campfire clearing) |
| 4 | (318, 171) | Ledge (rope bridge over the ravine) |
| 5 | (352, 120) | Boss tent (fixed) |

## Pixel checks (2026-11-06)
- Board fully opaque; pins and icons use alpha 0/255 only; no blur from scaling (nearest-neighbour).
- Art is original; the camp, tent and throne motifs only follow the mood of the user's reference videos.
