# Pet board UI: art spec

Art only. No Unity code, scene, prefab or import setting was changed.
Style: `Docs/UI/Reference/GAME01_UI_VIDEO_REFERENCE.md` (flat slate, 1 px light borders, 1 px cut corners, no wood/parchment).
Layout and rules approved by the owner on 2026-11-03:
1. Opening the board shows the **active** pet: big picture in the middle with Rarity, Level, XP and ATK.
2. Quick switch: pick a pet on the left → **SUMMON** on the right. SUMMON is the most visible button.
3. Sacrifice is small and at the bottom, has a 2-step confirm (step 1 shows the pet lost and the shards gained, step 2 is hold-to-confirm), and is disabled for the active pet ("Recall first").
4. Egg shards have their own area (count, progress, COMBINE). COMBINE stays disabled until the combine rule is approved and 10 shards are owned.

Goes with `Docs/Pets/PET_SYSTEM_SPEC_CURSOR.md` §2 and §4.

## Mockups (`mockups/`, x2, review only, not for Unity)

| File | State |
|---|---|
| `PetBoard_1_Active_x2.png` | Board opened, active Baby Dragon selected. SUMMON and Sacrifice disabled |
| `PetBoard_2_Switch_x2.png` | Rabbit (Common, inactive) selected. SUMMON lit, Sacrifice enabled |
| `PetBoard_3_Sacrifice_Step1_x2.png` | Confirm 1/2: Rabbit → +1 Egg Shard (7 → 8 / 10) |
| `PetBoard_4_Sacrifice_Step2_x2.png` | Confirm 2/2: hold to sacrifice, fill shows 62 % |

Mockup text is VT323 for readability only. Use the game's UI font in Unity.

## Layout (board 440 x 300 canvas px; landscape reference 640 x 360)

| Area | Rect (x, y, w, h) | Contents |
|---|---|---|
| Header | 0, 0, 440, 20 | "PETS" gold, hint cyan, close ✕ at (426, 7) |
| Left | 6, 26, 134, 274 | "COLLECTION n", rows 134 x 28 every 30 px (portrait 24x24 at +2,+2, name, rarity, active dot, "Lv.n" right) |
| Shard area | 6, 246, 134, 50 | 1 px divider, "EGG SHARDS", shard icon in a 22x22 box, "7 / 10", 50x4 progress, COMBINE 48x18 |
| Middle | 146, 26, 176, 274 | Portrait frame 88x88 (portrait x3), rarity badge on its top edge, name gold, status, XP bar 130x5 with "Lv.n" and "x / y XP", ATK / SPEED / STYLE |
| Right | 328, 26, 106, 274 | "COMMAND", SUMMON 106x28, note line, 4 small commands 52x19 in 2x2, divider, Sacrifice 90x15 at the bottom, reason line |
| Confirm popup | centred, 200 x 118 | Board dimmed behind |

Text colours: gold `#F2D84A` headings and names, cyan `#8EC8F0` info, green `#7ED67E` active/SUMMON, red `#E04848` sacrifice, muted `#8A92A3` reasons, disabled `#353A45`.
Rarity colours: Common `#AEB6C8`, Rare `#5ED0E0`, Epic `#C99AF2`.

## Sprites (`export/`, 9-slice border 3 px on every side unless noted)

| File | Size | Use |
|---|---|---|
| `UI_PetBoard_Panel` | 12x12 | Board background |
| `UI_PetBoard_Modal` | 12x12 | Confirm popup |
| `UI_PetBoard_Row_Normal` / `_Selected` | 12x12 | Collection rows |
| `UI_PetBoard_Button_Summon_{Normal,Hover,Pressed,Disabled}` | 12x12 | SUMMON |
| `UI_PetBoard_Button_{Normal,Hover,Pressed,Disabled}` | 12x12 | RECALL / FOLLOW / STAY / CALL / CANCEL / BACK / COMBINE |
| `UI_PetBoard_Button_Danger_{Normal,Hover,Pressed,Disabled}` | 12x12 | Sacrifice, NEXT, HOLD TO SACRIFICE |
| `UI_PetBoard_HoldFill` | 4x4, no slice | Hold progress fill inside the danger button (scale X from 0 to 1 over 1 s) |
| `UI_PetBoard_Badge_{Common,Rare,Epic}` | 8x8 | Rarity badge on the portrait frame |
| `UI_PetBoard_PortraitFrame_{Common,Rare,Epic}` | 12x12 | Middle portrait frame and popup boxes |
| `UI_PetBoard_XP_Track` | 4x4, no slice | XP / shard progress track |
| `UI_PetBoard_XP_Fill` | 4x4, slice bottom 1 | XP / shard fill (flat + darker bottom row) |
| `UI_PetBoard_ActiveDot` | 5x5 | "ACTIVE" marker in a row |
| `UI_PetBoard_Close` | 7x7 | ✕ |

Shard icons come from `Docs/PetArt/Shard`. Pet portraits come from `Docs/PetArt/Species`.

## Import notes for Unity (Cursor side)

Sprite (single), Point, compression None, no mipmaps, same Pixels Per Unit as the UI kit sprites. Set the 9-slice borders above in the Sprite Editor; Image type Sliced.

## Rebuild

The mockups need `Assets/Resources/Fonts/VT323-Regular.ttf` (present in the main checkout; not tracked on every branch). Sprites do not need it.

```powershell
Add-Type -Path "Docs\UI\tools\UiCore.cs","Docs\PetArt\tools\PetBoardUi.cs" -ReferencedAssemblies System.Drawing,System.Core
[Img]::Root = (Get-Location).Path
[PetBoardUi]::Run((Join-Path (Get-Location).Path "Docs\PetArt\Board"), (Join-Path (Get-Location).Path "Assets\Resources\Fonts\VT323-Regular.ttf"))
```
