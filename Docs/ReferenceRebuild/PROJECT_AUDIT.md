# PROJECT_AUDIT — R0 Dual-Aspect Architecture Review

**Date:** 2026-10-19 (refresh with `R0_R1_Dual_Aspect_Handoff.zip`)  
**Unity:** 6000.3.26f1 · URP 2D · MCP connected  
**Phase 12E:** **PAUSED**  
**R0 mode:** Audit / backup / screenshots / design review only — **no gameplay implementation in this pass**

### Branch / checkpoint

| Item | Value |
|---|---|
| R0 branch | `reference_rebuild_r0` @ `dca4cfc` |
| Working note | Machine may also have `reference_rebuild_r1` experiments; **this R0 package is the Claude handoff** |
| Save/Load | **Not modified** (no `GameProgress.ResetAllProgress` / no intentional unlock writes for audit) |

### Constraint compliance

| Forbidden | Status |
|---|---|
| Create Level 5 | **Not done** |
| Change Hero sprite | **Not done** |
| Change Ground Tilemap (campaign) | **Not done** |
| Change Combat | **Not done** |
| Remove Mob Labels | **Not done** |
| Switch game fully to Portrait | **Not done** |
| Edit Level 1–4 data | **Not done** |

---

## 1. Backup — PASS

| Item | Path / evidence |
|---|---|
| Git branch | `reference_rebuild_r0` |
| Scene backup | `IncomingAssets/backup_before_r0_dualaspect/Scenes/GameplayPrototype_Phase4Test.unity` (+ prior phase backups) |
| Level / Data backup | `IncomingAssets/backup_before_r0_dualaspect/Data/` |
| Prefab backup | `IncomingAssets/backup_before_r0_dualaspect/Prefabs/` (16 prefabs) |
| Scripts snapshot (earlier) | `IncomingAssets/backup_before_r0/Scripts/` |
| Handoff pack extracted | `IncomingAssets/R0_R1_Handoff/` (from Downloads zip) |

---

## 2. Unity project inventory — PASS

**CSV:** [`CURRENT_ASSET_INVENTORY.csv`](./CURRENT_ASSET_INVENTORY.csv) (118 Texture2D rows under `Assets/Art` + `Assets/Resources`, Rebuild art excluded from R0 baseline inventory)  
**Live snapshot:** [`UNITY_SNAPSHOT.txt`](./UNITY_SNAPSHOT.txt)

### 2.1 Systems verified (MCP)

| System | Location / notes |
|---|---|
| Hero | Scene object `Player` (no Hero Prefab) — Movement, Combat, Visual, Health, SpriteSorter, Sword, ContactShadow |
| Animator | `Assets/Art/GoblinCampPhase4/Animators/Hero.controller` · sheet-driven · PPU 16 · Point |
| PlayerMovement / PlayerCombat | Input System Keyboard; R chain + skills 2/3/4/V/C |
| EnemyController / EnemyVisual / EnemySpawner | Goblin, Gobrider, Elite, Warlord (L5 paused) |
| CameraFollow | Bounds + KeepTargetOnScreen |
| URP PixelPerfectCamera | **PPU 16**, ref **480×270**, crop None, ortho ≈ 8.4375 |
| SpriteSorter | `base + 4000 + round(-y*100)` |
| TreeCanopyOcclusion | Runtime trunk/canopy split; fades canopy |
| WorldLabelsCanvas + EnemyWorldUI | **Always-on** Name + Level + HP |
| SkillHotbarUI + TouchMovePad | Landscape anchors; **R bottom-right**; **2/3/4/V/C bottom-center** |
| DamagePopup / HitStop / Camera Shake | Present |
| LevelFlow / GameProgress / LevelConfig | L1–4 playable; L5 config may exist but Phase 12E paused |
| LevelMaps | Camp, Trail, AbandonedCamp, GoblinOutpost, GobriderStronghold |

### 2.2 Dominant art conventions

- **PPU 16**, **Filter Mode Point**, mostly **Uncompressed**, alpha transparency on sprites  
- Sorting Layer: **Default** (order via SpriteSorter / tilemap)  
- Hero Visual world height measured: **2.00 wu**

---

## 3. Baseline screenshots — PASS

See [`BASELINE_TESTS.md`](./BASELINE_TESTS.md).  
**Primary folder:** `Docs/ReferenceRebuild/SCREENSHOTS/`  
**Unity copies:** `Assets/Docs/ReferenceRebuild/SCREENSHOTS/`  
**Prior archive:** `Docs/ReferenceRebuild/SCREENSHOTS/archive_prior/` + `Assets/Screenshots/R0_Baseline/`

Camera for all fresh captures: **Landscape PPC 480×270 · PPU 16 · 1920×1080 · aspect 1.778**.

---

## 4. Dual-aspect architecture review (DESIGN ONLY — not implemented in R0)

### 4.1 Target profiles

| Profile | PPC ref | Game View test | PPU | Visible world (wu) |
|---|---|---|---|---|
| **Landscape** (current shipping) | 480×270 | 1920×1080 | 16 | **30.0 × 16.875** |
| **Portrait** (proposed for video parity) | 144×256 | 576×1024 | 16 | **9.0 × 16.0** |

### 4.2 Proposed `AspectVisualProfile` (ScriptableObject) — do not ship in R0

```
AspectVisualProfile
  id: Landscape | Portrait
  pixelPerfectRef: Vector2Int      // 480x270 | 144x256
  testGameView: Vector2Int         // 1920x1080 | 576x1024
  assetsPPU: 16
  cameraFollowOffset: Vector3
  keepOnScreenMargins: Vector3     // side, top, bottom (wu)
  hudScalerReference: Vector2
  hudMatchWidthOrHeight: float
  showMobileJoystick: bool
  defaultBoundsMin/Max: Vector2    // shared playable core + landscape side margins
```

**Runtime service (future R1+):** one active profile; applies PPC + CameraFollow margins + CanvasScaler + joystick visibility.  
**Must stay single:** LevelFlow, GameProgress, PlayerCombat, EnemySpawner/AI, Input subscriptions, Mob Labels, SpriteSorter, TreeCanopyOcclusion.

### 4.3 Hero size consistency (no sprite stretch)

| Metric | Landscape (live) | Portrait (math) |
|---|---|---|
| Ortho height | 16.875 wu | 16.000 wu |
| Hero height | 2.00 wu | 2.00 wu |
| Hero / viewport | **0.1185** | **0.1250** |
| Absolute Δ | — | **+0.0065** (~**+5.5%** relative) |

**Recommendation:** keep PPU 16 and hero sprite scale identical; rely on near-matching vertical world height. Optional later: tiny PPC crop tweak only if reference demands tighter framing — **never non-uniform sprite scale**.

### 4.4 Camera bounds

- Author maps with **shared playable core** (~±4.5 X usable in portrait width) and **landscape-only side margins**.  
- Keep `LevelConfig` bounds landscape-authored; portrait profile clamps/pads.  
- South spawn must stay centerable (already fixed for L4 landscape).

### 4.5 HUD anchors (both aspects)

- HP / Level / Wave: top-left + safe area  
- Skills **2,3,4,V,C**: bottom-center  
- **R**: bottom-right  
- Joystick: bottom-left (portrait/mobile; optional hide on PC landscape)  
- Mob Name + Level + HP: **always on**

### 4.6 Orientation / device

- Do **not** force `Screen.SetResolution` on phone.  
- Editor Game View sizes are for QA only.  
- Portrait autorotate remains off until an approved profile lands.

---

## 5. Top 10 differences vs reference pack

Sources: `IncomingAssets/R0_R1_Handoff/ReferencePack/` (clips index + sheet JPGs), existing `GameReference/R1_Rebuild/` if present.  
**Limit:** Clip **21 unreadable** (`moov atom not found`). Full frame-by-frame Claude shotlist still recommended.

| # | Difference | Current game | Reference signal | Severity |
|---|---|---|---|---|
| 1 | **Aspect** | Landscape 16:9 (480×270) | Portrait 9:16 combat videos | Critical for compare |
| 2 | **FOV width** | ~30 wu wide | ~9 wu wide in portrait | Layout / spawn density |
| 3 | **Ground transitions** | Hard dirt/grass tile edges | Softer path + greenery (clips 4,10,24) | High art |
| 4 | **Forest canopy / occlusion** | Functional fade; simpler trees | Richer canopy contrast (clips 8,19) | High |
| 5 | **Combat VFX readability** | Procedural + optional slash sheets | Strong arc/AoE readability (clips 3,12) | High |
| 6 | **Camp / outpost dressing** | Phase4 tents/props present | Denser camp storytelling (clip 22) | Medium–High |
| 7 | **Mob label presentation** | Always-on Name+Lv+HP (good) | Label/HP/combo readability (clip 20) — style may differ | Medium |
| 8 | **HUD chrome** | Landscape hotbar + large R | Portrait-first combat HUD (clips 3,6,17) | Medium |
| 9 | **Weather / atmosphere** | None | Rain overlays (clip 2) | Low–Medium (optional) |
| 10 | **Boss / telegraph polish** | L4 waves / L5 paused | Cyclops/boss telegraphs (clips 11,18,26) | Out of R0 scope |

---

## 6. PASS / FAIL / NOT TESTED summary

| Item | Result | Evidence |
|---|---|---|
| Backup + `reference_rebuild_r0` | **PASS** | Git + `IncomingAssets/backup_before_r0_dualaspect/` |
| Inventory CSV | **PASS** | 118 textures |
| L1–4 baseline shots | **PASS** | `SCREENSHOTS/01`–`04` |
| Hero idle / walk / R | **PASS** | `05`–`07` |
| Goblin combat / Gobrider | **PASS** | `08`–`09` |
| Mob Labels + HP always visible | **PASS** | `10_mob_labels_hp.png` — labels not auto-hidden |
| Tree occlusion | **PASS** | `11_tree_occlusion.png` |
| Hotbar R + skills row | **PASS** | `12_hotbar_skills.png` + L4 HUD |
| Landscape PPC 480×270 | **PASS** | Live ortho 8.4375, hero frac 0.1185 |
| Portrait profile implemented | **NOT TESTED** (deferred by R0) | Design §4 only |
| Portrait Game View 576×1024 live | **NOT TESTED** | By design in R0 |
| Device multi-touch | **NOT TESTED — DEVICE** | `TouchMovePad` exists |
| Clip 21 reference | **FAIL / BLOCKER** | Unreadable MP4 in pack |
| Level 1–4 data mutated | **PASS** (none) | No asset writes to level maps |
| Combat / Hero / Labels changed | **PASS** (none) | Audit-only |

---

## 7. Claude handoff package

| File | Purpose |
|---|---|
| `Docs/ReferenceRebuild/PROJECT_AUDIT.md` | This document |
| `Docs/ReferenceRebuild/CURRENT_ASSET_INVENTORY.csv` | Asset inventory |
| `Docs/ReferenceRebuild/BASELINE_TESTS.md` | Shot + test log |
| `Docs/ReferenceRebuild/SCREENSHOTS/` | Fresh 12 baseline PNGs |
| `Docs/ReferenceRebuild/UNITY_SNAPSHOT.txt` | Hierarchy/PPC snapshot |
| `Docs/ReferenceRebuild/reference_clips_index.csv` | From handoff zip |
| `IncomingAssets/R0_R1_Handoff/` | Full prompt + ReferencePack sheets |

**STOP.** Awaiting Claude R0 review / shotlist. Do not implement `AspectVisualProfile` until approved after Claude returns.
