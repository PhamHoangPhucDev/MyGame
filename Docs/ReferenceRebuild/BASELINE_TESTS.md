# BASELINE_TESTS — R0 Dual-Aspect Audit

**Date:** 2026-10-19  
**Branch checkpoint:** `reference_rebuild_r0` @ `dca4cfc`  
**Unity:** 6000.3.26f1 · MCP connected  
**Scene:** `Assets/Scenes/GameplayPrototype_Phase4Test.unity`  
**Camera profile (all fresh captures):** Landscape PPC **480×270**, PPU **16**, Game View **1920×1080**, aspect **1.778**, ortho height **16.875 wu**  
**Save/Load:** Not intentionally modified for corruption testing.

---

## Capture index (fresh R0 handoff set)

| # | Request | File | Level / Layout | Gameplay state | Result |
|---|---|---|---|---|---|
| 1 | Level 1 Forest | `SCREENSHOTS/01_level1_forest.png` | L1 · Camp | Playing · Wave 1 | **PASS** |
| 2 | Level 2 Trail | `SCREENSHOTS/02_level2_trail.png` | L2 · Trail | Playing | **PASS** |
| 3 | Level 3 Abandoned Camp | `SCREENSHOTS/03_level3_abandoned_camp.png` | L3 · AbandonedCamp | Playing | **PASS** |
| 4 | Level 4 Goblin Outpost | `SCREENSHOTS/04_level4_goblin_outpost.png` | L4 · GoblinOutpost | Playing · Wave 1 | **PASS** |
| 5 | Hero Idle | `SCREENSHOTS/05_hero_idle.png` | L1 | Movement locked | **PASS** |
| 6 | Hero Walk | `SCREENSHOTS/06_hero_walk.png` | L1 | External input +X | **PASS** |
| 7 | R Attack Combo | `SCREENSHOTS/07_r_attack_combo.png` | L1 under tree | `SetSwordHeld(true)` | **PASS** |
| 8 | 5 Goblin combat | `SCREENSHOTS/08_combat_5_goblins.png` | L4 | ≥5 Goblins + labels | **PASS** |
| 9 | Gobrider | `SCREENSHOTS/09_gobrider.png` | L4 | Gobrider Lv.5 on field | **PASS** |
| 10 | Mob Labels + HP | `SCREENSHOTS/10_mob_labels_hp.png` | L4 | Always-on Name+Lv+HP | **PASS** |
| 11 | Tree Occlusion | `SCREENSHOTS/11_tree_occlusion.png` | L1/L4 trees | Hero under canopy | **PASS** |
| 12 | Hotbar & Skills | `SCREENSHOTS/12_hotbar_skills.png` | L1 | R bottom-right; 2/3/4/V/C center | **PASS** |

Also mirrored under `Assets/Docs/ReferenceRebuild/SCREENSHOTS/`.  
Prior set retained: `Assets/Screenshots/R0_Baseline/` and `SCREENSHOTS/archive_prior/`.

---

## Measured hero size (Landscape live)

| Metric | Value |
|---|---|
| Screen | 1920×1080 |
| Ortho world height | 16.875 wu (= 270/16) |
| Hero Visual bounds.y | 2.00 wu |
| Hero height / viewport | **0.1185** (~11.85%) |

### Predicted Portrait 9:16 (math only — not switched)

| Metric | Value |
|---|---|
| Ref | 144×256 @ PPU 16 |
| Ortho world height | 16.00 wu |
| Same hero 2.00 wu | **frac = 0.1250** (~12.5%) |
| Delta vs landscape | **+0.0065** abs · **~+5.5%** relative — no sprite scale needed |

---

## Systems checks

| Test | Result | Evidence |
|---|---|---|
| Unity MCP connected | **PASS** | `editor_status` ready |
| Git `reference_rebuild_r0` | **PASS** | `dca4cfc` |
| Backup scene/data/prefabs | **PASS** | `IncomingAssets/backup_before_r0_dualaspect/` |
| Inventory CSV | **PASS** | 118 textures |
| Mob Labels never auto-hide | **PASS** | Shot 10 + EnemyWorldUI always-on |
| Skill hotbar anchors | **PASS** | Shot 12 / L4 HUD |
| Pixel Perfect 480×270 PPU16 | **PASS** | Live PPC + ortho |
| Portrait profile code (R0) | **NOT TESTED** | Deferred — design in PROJECT_AUDIT §4 |
| Portrait Game View 576×1024 | **NOT TESTED** | By R0 design |
| Device joystick | **NOT TESTED — DEVICE** | Code present |
| Reference clip 21 | **FAIL** | Unreadable MP4 in handoff pack |
| Level 1–4 data edited | **PASS** (none) | Audit-only |
| Combat / Hero / Ground swapped | **PASS** (none) | Audit-only |

---

## Notes for Claude

- Handoff zip unpacked: `IncomingAssets/R0_R1_Handoff/`  
- Clips index: `Docs/ReferenceRebuild/reference_clips_index.csv`  
- Reference sheet JPGs: `IncomingAssets/R0_R1_Handoff/ReferencePack/reference_sheet_{1,2,3}.jpg`  
- Phase 12E remains paused; do not extend Level 5 in R0 follow-ups without new approval.  
- **R0 STOP** after this document set.
