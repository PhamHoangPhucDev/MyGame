# GAME01 — RPG UI Kit

UI art and design only. No Unity code, scene, or import setting was changed.

## Canva

| Design | Content | Link |
|---|---|---|
| GAME01 — RPG UI MASTER DESIGN SYSTEM | p1–12: design system (A–J + asset library). p13–20: early copy of L01–L08 | https://www.canva.com/d/M_X3Uer9CvpN3mq |
| GAME01 — UI Screens Portrait 9:16 | 30 portrait screens P01–P30 | https://www.canva.com/d/Ogt-wZaMjf0pyeH |
| GAME01 — UI Screens Landscape 16:9 (PC) | 30 landscape screens L01–L30 | https://www.canva.com/d/cNDsaLPiIUaJSLE |
| Folder | Both designs | https://www.canva.com/folder/FAHZGa9_2vI |

Canva exports: `canva_export/GAME01_RPG_UI_Master_Design_System.pdf` (20 pages) and `canva_export/GAME01_UI_Screens_Landscape.pdf` (30 pages).

Canva limits found:
- Canva MCP exports whole pages (PDF, PNG, JPG, PPTX). It cannot export a single element, so the Unity sprites are exported from the generator here, not from Canva.
- Master pages 13–20 were made while Canva AI credits were out. The landscape design now holds the full set; those 8 pages are duplicates (the Canva MCP has no delete-page action).
- Canva pages hold the pixel art as flat images. The portrait pages are 1080×1920, so the 576×1024 mockups are scaled ×1.875 and look slightly soft in Canva. The PNGs in `mockups/` are pixel-exact.

## Folders

| Path | What |
|---|---|
| `export/<Folder>/*.png` | **148 Unity-ready sprites at 1 UI px** (transparent). Panels, Buttons, Controls, HUD, Bars, Slots, Icons, Icons/Items, Icons/Equip, Icons/Minimap |
| `GAME01_UI_ASSET_MANIFEST.csv` | One row per sprite: ID, name, screen, type, Canva page, file, size, usage, 9-slice borders, state, status |
| `mockups/portrait/P01–P30` | 576×1024 screen mockups |
| `mockups/landscape/L01–L30` | 1920×1080 PC mockups (L09–L30 reuse the portrait popups or use two-column windows) |
| `mockups/SCREEN_LIST.csv` | Screen file → title |
| `boards/B01–B12` | Design-system pages (1920×1080) |
| `tools/` | Generator (C#, same pattern as the R2C art tools). Re-run to rebuild everything |

Rebuild (PowerShell, about 40 s):

```powershell
$T = "Docs\UI\tools"; Add-Type -Path "$T\UiCore.cs","$T\UiParts.cs","$T\UiIcons.cs","$T\UiScene.cs","$T\UiScreens.cs","$T\UiBoards.cs","$T\UiRun.cs" -ReferencedAssemblies System.Drawing,System.Core
[Img]::Root = (Get-Location).Path; [Txt]::FontPath = "Assets\Art\Fonts\PressStart2P-Regular.ttf"; [UiRun]::Run("Docs\UI")
```

## Layout notes

**Pixel grid.** All UI art is drawn at 1 UI px. The world behind it stays at PPU 16, ×4.

| Profile | UI canvas | Scale | Screen | World PPC |
|---|---|---|---|---|
| Portrait (primary) | 288×512 | ×2 | 576×1024 | 144×256 |
| Landscape (PC) | 640×360 | ×3 | 1920×1080 | 480×270 |

**Portrait 9:16 anchors.**
- Top-left: portrait 38, level badge, HP bar, thin EXP bar, class name.
- Top-centre: wave ribbon and objective, or boss name and boss bar.
- Top-right: Bag, Map, Settings/Pause (22 px buttons) and coins below them.
- Left, under the hero block: quest tracker in the village.
- Bottom-left: joystick, 64.
- Bottom-right: attack, 56 round, plus five 38 skill slots on an arc with radius 104 (180° → 90°). Mobile shows no key labels.
- Interact: 40 round button next to attack. The prompt plate sits above the NPC name.
- The centre stays clear. The combo counter sits on the right edge, at mid height.

**Landscape 16:9 (PC).** Keeps the approved layout: five skills bottom-centre with keys 1–5, and R/SPACE bottom-right. Top-right buttons show key caps (I, ESC). The village has a key hint bar at the bottom-left.

**Spacing.** Safe margin 8. Panel padding: 12 main, 9 popup, 5 tooltip. Gaps 6–8. Touch targets ≥ 22 UI px.

## Integration for Cursor

1. Import `export/` under e.g. `Assets/Art/UI/Kit/` with Sprite (2D and UI), Single, Filter Point, Compression None, no mipmaps, Mesh Type Full Rect. Keep the project's PPU 16 and set the Canvas Scaler **Reference Pixels Per Unit = 16** so 1 sprite px = 1 UI px.
2. For every row whose usage lists a `9-slice border L,T,R,B`, set the Sprite Editor border to those values and use Image Type **Sliced**. `Header 3-slice` rows stretch horizontally only, at a fixed height of 18.
3. Canvas Scaler: Scale With Screen Size. Use reference 288×512 in the portrait profile and 640×360 in the landscape profile (`AspectProfileService` already switches profiles).
4. Bars: put the frame (sliced) under a fill image set to Filled Horizontal. Fill columns are identical, so stretching is safe. For a boss bar, the fill area is x16…w-17, y4…11.
5. Skill slot: slot frame 38, class icon 32 at (3,3) (existing `ClassUiIcons`), then `UI_SkillSlot_CooldownFill` as Filled Radial360 clockwise, then the cooldown number as text. The key cap overlaps the slot's bottom edge. Mobile attack: `UI_AttackButton_*` with the class R icon masked by `UI_Mask_Circle32`.
6. Text: all of it is drawn in Unity with Press Start 2P at 8 × integer. Colours and treatments are on board B03. Never bake numbers into sprites.
7. Pressed buttons: shift the label 1 px down. Selected is the keyboard or gamepad focus ring.
8. Suggested targets in today's code: `SkillHotbarUI` (slots, key caps, attack), `LevelFlowUI` (menu, pause, victory, game over), `BossHudUI`, `EnemyWorldUI` (enemy bar), `TouchMovePad` (joystick), and the `WorldServices` and `WorldNavigator` panels (main panel, tabs, list rows, slots). The `GAME01_ART_SLOTS.csv` ids `ui_*` and `item_*` map to `UI_Icon_*` and `UI_Item_*`.

## Not done / concept only

- **Logo**: placeholder text "GAME01" with a LOGO SLOT tag.
- **NPC portraits**: a shared silhouette slot. The NPCs are tinted blocks in the game.
- **World map art**: the drawn map is a concept; the game uses a list today. Minimap art is also a concept (the game shows a zone label only).
- **Skill tree, gems, housing placement, Fiber crop**: UI marked CONCEPT and data-only. No new mechanics were designed.
- **Equipment**: only the weapon slot is live. The other five slots are drawn locked. The blade has no combat bonus in game.
- **Mana bar**: drawn, but no class uses mana today. It is kept for the shield/absorb overlay.
- **Warden dialogue line**: not in `WorldContent`.
- **Not verified in Unity**: nothing was imported or tested in Play Mode, and no device touch test was done.
