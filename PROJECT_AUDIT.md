# R0 — Read-Only Project Audit

**Date:** 2026-10-19  
**Unity:** 6000.3.26f1  
**Unity MCP:** Connected (`editor_status` = ready)  
**Phase 12E:** PAUSED (no Level 5 continuation in this pass)  
**Scope:** Audit only — no sprite swaps, no gameplay code edits, no new levels.

---

## 0. Checkpoint

| Item | Value |
|---|---|
| Git | **Initialized this pass** (project previously had no `.git`) |
| Commit | `dca4cfc62af7ff42f4493b15e39c49218340d8ab` |
| Message | `R0 checkpoint: pre-reference rebuild audit baseline.` |
| Extra backup | `IncomingAssets/backup_before_r0/Scripts/` (script tree copy) |

---

## 1. Runtime / Display Snapshot

| Setting | Value | Notes |
|---|---|---|
| Default window | 1920×1080 | PlayerSettings |
| Orientation | LandscapeLeft | Autorotate L/R **ON**, Portrait **OFF** |
| Pixel Perfect Camera | Present | `assetsPPU=16`, ref **480×270**, `cropFrame=None` |
| Main Camera | Orthographic | Measured ortho size ≈ **8.44** @ 16:9 |
| Aspect in Play | 1.778 | Captured Game View **1920×1080** |
| Portrait 9:16 | **Not enabled** | `PortraitPixelSetup` only sets camera clear color; does not switch PPC/UI |

**Landscape 16:9:** Confirmed active and playable.  
**Portrait 9:16 add-on without breaking scenes:** Feasible as a *second presentation profile* (PPC ref e.g. 270×480, alternate CanvasScaler / hotbar anchors, map camera bounds per aspect). Current scene content is authored for landscape framing; dual-aspect needs UI + PPC + bounds profiles — **not implemented**. Risk: letterboxing or crushed HUD if forced without profiles.

---

## 2. Systems Inventory (code / scene)

### 2.1 Hero
- **No `Player*.prefab`** — Hero lives in the gameplay scene (tag `Player`).
- Components: `PlayerMovement`, `PlayerCombat`, `PlayerVisual`, `Health`, `SpriteSorter` (on Visual), sword child, ContactShadow.
- Animator controller: `Assets/Art/GoblinCampPhase4/Animators/Hero.controller` (Play Mode clip e.g. `Hero_Hero_Idle_Side`).
- Sheet: `Hero_Sheet.png` 128×288, **PPU 16**, Point, Multiple, **33 sprites**.
- Extra: Hurt / Windup / Death under `HeroExtra/`.
- Legacy singles still on disk: `Assets/Art/Characters/HeroWalkA/B`, `HeroStrike` (16×20) — not the Phase 4 primary path.

### 2.2 Combat skills (`PlayerCombat`)
| Key | SkillId | Behavior (code) |
|---|---|---|
| R / Space | Sword | 3-hit slash chain, hold-to-repeat, buffer |
| 2 | FireDash | Flame burst AoE |
| 3 | MultiSlash | Dash slash |
| 4 | MawBite | Whirlwind multi-wave |
| V | Bone | Guard damage reduction |
| C | Mace | Crescent strike |

VFX: mostly **procedural** `CombatVfx` (slash / burst / shockwave), not authored skill VFX sheets.

### 2.3 Enemies
- `EnemyController` + `EnemyVisual` + `EnemySpawner` + `EnemyWorldUI`.
- Kinds: Goblin, Gobrider; Elite Gobrider (stats/tint/scale); Boss `BossWarlordController` (L5 — paused).
- Goblin sheet 104×88 / 10 sprites; Gobrider 120×96 / 10 sprites; Idle/Walk/Attack/Hurt clips.
- Labels: always-on name + HP via `WorldLabelsCanvas` (sorting 55).

### 2.4 Camera / Sorting / Pixel
- `CameraFollow`: SmoothDamp + bounds clamp + `KeepTargetOnScreen` (HUD bottom margin).
- `SpriteSorter`: `baseOrder + 4000 + round(-y*100)` — keeps bodies above Ground at positive Y.
- `TreeCanopyOcclusion`: runtime trunk/canopy split, fade alpha when covered.
- URP Pixel Perfect as above.

### 2.5 World / Maps
`LevelMaps/` children: **Camp, Trail, AbandonedCamp, GoblinOutpost, GobriderStronghold**.  
Tile fill: `GrassSoft` + dirt modules; props from Phase 4 prefabs + Environment sprites.  
Missing fort art still listed as design needs (gate / fence / tower / barricade).

### 2.6 UI
- `SkillHotbarUI` — landscape HUD (HP top-left, skills bottom-center, R bottom-right, joystick pad bottom-left).
- `TouchMovePad` — code present; **device touch NOT TESTED**.
- `LevelFlowUI` — menu / select / pause / victory / chapter complete / game over.
- `BossHudUI` — top boss bar (L5).
- `EnemyWorldUI` + `DamagePopup` on shared world overlay canvas.
- Font: Press Start 2P (Resources / Art/Fonts).

### 2.7 LevelFlow / Progress
- Levels 1–5 configs (runtime factory + assets under `Assets/Data/Levels/`).
- Objectives: ClearWaves, ClearWavesThenReachExit, DefeatBossChapterComplete.
- `GameProgress` PlayerPrefs unlock / bests.
- Save/Load path exists; L5 chapter complete implemented but **Phase 12E paused for reference rebuild**.

### 2.8 FX / Feel
- `HitStop`, `CameraFollow.Shake`, `CombatVfx`, `DamagePopup`, `HitComboCounter`.

---

## 3. Asset inventory summary

Full table: **`IncomingAssets/R0_Audit/ASSET_INVENTORY.csv`** (also mirrored below as deliverable name).

| Category | Count (approx) | PPU | Filter | Notes |
|---|---|---|---|---|
| Art textures/sprites | 115 rows | mostly **16** | **Point** | Uncompressed common |
| AnimatorControllers | 3 | — | — | Hero, Goblin, Gobrider |
| AnimationClips | 24 | — | — | See frame estimates in CSV |
| Prefabs (Phase 4 env) | Tree/Rock/Crate/Flag/Brazier/Bone… | 16 | Point | In use on maps |
| LevelConfig assets | L1–L5 | — | — | Data |

**Key sheets**

| Asset | Size | Sprites | Use |
|---|---|---|---|
| Hero_Sheet.png | 128×288 | 33 | Primary hero |
| Goblin_Sheet.png | 104×88 | 10 | Goblin |
| Gobrider_Sheet.png | 120×96 | 10 | Gobrider / Elite / temp Boss |
| GrassSoft.png | 16×16 | 1 | Main grass fill |
| DirtM0–M15 + Dirt | 16×16 | 1 each | Paths / edges |

**Hero clip frame estimates (from clip length×fps in CSV):** Idle/Walk dirs ~4; Attack ~3; Attack Windup ~13; Hurt ~9; Death ~8.

---

## 4. Screenshot baseline

Folder: **`Assets/Screenshots/R0_Baseline/`**

| # | File | Subject | Status |
|---|---|---|---|
| 1 | `01_hero_idle.png` | Hero Idle (L1) | Captured |
| 2 | `02_hero_walk.png` | Hero Walk | Captured |
| 3 | `03_r_attack_combo.png` | R hold / attack | Captured |
| 4 | `04_combat_goblins.png` | L4 goblin combat | Captured |
| 5 | `05_gobrider_elite.png` | Elite Gobrider wave | Captured |
| 6 | `06_forest_ground.png` | Ground / dirt | Captured |
| 7 | `07_tree_occlusion.png` | Under tree canopy | Captured |
| 8 | `08_hotbar.png` | Hotbar / HUD | Captured |
| 9 | `09a`–`09e` + `09_skills_hotbar_row.png` | Skills 2/3/4/V/C | Captured |
| 10 | `10_level1.png` | Level 1 | Captured |
| 11 | `11_elite.png` | Elite frame | Captured |
| 12 | `12_level3.png` | Level 3 | Captured |
| 13 | `13_level4.png` | Level 4 | Captured |
| — | Boss Warlord exclusive frame | — | **NOT CAPTURED this pass** (Elite covered; L5 paused) |

Play Mode used only to pose for screenshots; **no persistent code/art changes**.

---

## 5. Have / Missing / Broken / Art-swap risk

| Area | Have | Missing / weak | Broken / fragile | Risk if art replaced |
|---|---|---|---|---|
| Hero body | Phase 4 sheet + Animator | Reference-fidelity sheet; dedicated Prefab | Legacy Character PNGs unused | `PlayerVisual` attack/sword sort sync |
| Skills | R/2/3/4/V/C logic | Pixel VFX sheets per skill | Icon art is generated UI | Timing keyed to attack frames |
| Enemies | Goblin/Gobrider sheets | Distinct Elite/Boss sprites | Boss = tinted Gobrider | Label offsets, collider sizes |
| Sorting | +4000 Y formula | — | Was under-ground before fix | Must keep PPU 16 + pivot rules |
| Camera | Follow + on-screen keep | Portrait profile | — | Bounds vs sprite size |
| Ground | GrassSoft + dirt | Richer forest floor from refs | Old Grass hatch still on disk | Tile PPU/pivot |
| Trees | Phase 4 trees + occlusion | — | Texture readable / split assumptions | Canopy pixel thresholds |
| Props | Crates, flags, tents… | Gate/fence/tower/barricade | Stakes as gate language | Colliders / sorter |
| UI | Landscape hotbar + labels | Portrait layout; polished icons | Touch **device untested** | Anchor / safe area |
| Levels | L1–L4 playable; L5 exists | Reference-quality stronghold art | — | Spawn zones / bounds |
| Multi-res | 1920×1080 verified | 1600×900, 1280×720 | — | PPC integer scale |

---

## 6. Top 10 differences to prioritize (pre-reference pack)

These are the highest-leverage gaps **before** Claude digests the 30-video pack. Re-rank after reference analysis.

1. **Hero presentation vs reference** — current Phase 4 sheet + scene Hero vs likely denser idle/walk/attack sets in videos.  
2. **Skill readability** — gameplay skills work; VFX/icons are procedural/UI-generated, not video-matched pixel FX.  
3. **Enemy silhouette ladder** — Goblin → Gobrider → Elite → Warlord need distinct art (Elite/Boss currently rescale/tint).  
4. **Fort / outpost language** — stakes+flags stand in for gate/fence/tower (listed in art requests).  
5. **Ground / path richness** — GrassSoft is intentionally flat; references may expect denser floor detail without checker hatch.  
6. **No Hero Prefab** — scene coupling complicates safe art swaps and A/B testing.  
7. **Dual orientation** — landscape locked; portrait 9:16 needs PPC+UI profiles without forking scenes.  
8. **UI chrome** — functional hotbar; may diverge from reference skill discs / frames / feedback.  
9. **Occlusion + sorting contracts** — any new tree/hero pivot or PPU breaks canopy fade and draw order.  
10. **Unused / dual art paths** — `Assets/Art/Characters/*` legacy vs `GoblinCampPhase4` primary increases swap mistakes.

---

## 7. Tests not completed / MCP notes

| Item | Result |
|---|---|
| Unity MCP connection | **OK** |
| Git checkpoint | **OK** (new repo) |
| Asset CSV export | **OK** |
| Baseline screenshots (list above) | **OK** @ 1920×1080 |
| 1600×900 / 1280×720 Game View | **NOT TESTED** (GameView size API not switched this pass) |
| Real mobile multi-touch | **NOT TESTED — DEVICE** |
| Profiler FPS/GC | **NOT TESTED** |
| Side-by-side vs 30 reference videos | **NOT TESTED** (ZIP / Rebuild Plan Pack not ingested in R0) |
| Boss-only screenshot | **NOT TESTED** this pass |

---

## 8. Deliverables

| Deliverable | Path |
|---|---|
| Audit report | `IncomingAssets/R0_Audit/PROJECT_AUDIT.md` |
| Inventory CSV | `IncomingAssets/R0_Audit/ASSET_INVENTORY.csv` |
| Unity snapshot | `IncomingAssets/R0_Audit/UNITY_SNAPSHOT.txt` |
| Screenshots | `Assets/Screenshots/R0_Baseline/` |
| Git tag/commit | `dca4cfc` on `master` |

---

## 9. Stop

R0 complete. **Stopped for handoff** — ready for reference ZIP + Rebuild Plan Pack analysis. No Phase 12E continuation until you approve.
