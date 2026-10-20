# R1.1 — Visual Quality Gate

**Date:** 2026-10-19  
**Scene:** `ReferenceCombatSandbox` only  
**Level 1–4 art / maps:** not rolled forward. Phase 12E still paused.  
**Video:** **NOT TESTED** — Unity Recorder is not in `Packages/manifest.json`.

---

## 1. Real portrait (no forced orthographic size)

`AspectGameViewHelper.SetSize` selects a **FixedResolution** slot and calls `GameView.SizeSelectionCallback`. Edit-mode target after the call: **576×1024**.

Play Mode, profile auto-selected from the screen (not a manual ortho write):

| | Portrait | Landscape |
|---|---|---|
| `Screen` | **576×1024** | **1920×1080** |
| `Camera.aspect` | **0.5625** | **1.778** |
| `PixelPerfectCamera` | **144×256**, PPU 16 | **480×270**, PPU 16 |
| `orthographicSize` | **8.000** (PPC) | **8.4375** (PPC) |
| Profile | Portrait | Landscape |

Shots: `Docs/ReferenceRebuild/R1/Screenshots/R1_1_portrait_576x1024.png`, `R1_1_landscape_1920x1080.png`.

| Check | Result |
|---|---|
| 576×1024 is the real Game View | **PASS** |
| 1920×1080 still works | **PASS** |
| HUD present both aspects | **PASS** (captures) |
| Mob labels both aspects | **PASS** — portrait 6/6 named; landscape after 20 s 5/5 named |

---

## 2. Forest ground (sandbox tilemaps only)

Repaint: `Tools/Gameplay Prototype/R1/Repaint Sandbox Ground`.

| Change | Detail |
|---|---|
| Dark grass | `GrassDark` only when hash `% 29 == 0` and not next to another dark cell. No `(x*17+y*31)&7` lattice |
| Path | Sine wobble, width 1–3, plus a shoulder and a short spur |
| Edges | `DirtM` bits match the generator: grass **N=1, E=2, S=4, W=8** (the old painter had S/E swapped) |
| Inner corners | Overlay `DirtCorners` using existing `DirtIC_*` tiles |
| Filter | Point. No bilinear |

| Check | Result |
|---|---|
| Uses existing Rebuild tiles | **PASS** |
| Level 1–4 tilemaps untouched | **PASS** |

---

## 3. Sword slash R

Spawned the three authored sheets in Play Mode (same `SpawnSwordSlashFrames` the R cast uses). Pivot distance is `chest - facing * pullback`.

| Variant | Sprite | Cell height | Pivot distance from chest |
|---|---|---|---|
| Hit 1 | `VFX_SwordSlash_A_0` | 6.00 wu (96 px) | **1.00** |
| Hit 2 | `VFX_SwordSlash_B_0` | 6.00 wu | **0.90** |
| Hit 3 | `VFX_SwordSlash_C_0` | 8.00 wu (128 px) | **1.80** |

Hero body cell is **2.00 wu**. Full slash cells are **3×** and **4×** that height (includes empty padding). Damage and cooldown were **not** changed (`slashImpactDelay` 0.12 s, `slashCooldown` 0.35 s).

| Check | Result |
|---|---|
| Three different VFX sheets | **PASS** |
| Three different **body** attack clips | **FAIL** — one `Hero_Attack_*` clip replays; chain index only swaps the VFX sheet |
| Hitbox time vs VFX | **PASS** — strike still waits 0.12 s, then damage once per cast (unchanged `PlayerCombat`) |
| Weapon child during the body swing | Hidden by `PlayerVisual` so the sheet pose is the blade (no orphan sword) |

---

## 4. Mob labels

Names, levels, and HP bars stay on. Alpha stays **1**. Off-screen culling is unchanged.

Sandbox-only spacing (campaign numbers stay 78×22 px, nudge 52): keep-out **92×30** px, nudge clamp **72** px. Labels still follow the enemy anchor.

| Check | Result |
|---|---|
| Names + HP kept | **PASS** |
| Not faded to hide overlap | **PASS** |

---

## 5. Skill mapping (not changed)

`PlayerCombat` keyboard path, matching the Phase 8 headers on that class:

| Key | `SkillId` enum | Header / behaviour |
|---|---|---|
| **2** | `FireDash` | Flame Burst |
| **3** | `MultiSlash` | Dash Slash |
| **4** | `MawBite` | Whirlwind |
| **V** | `Bone` | Guard |
| **C** | `Mace` | Crescent Strike |
| **R** | `Sword` | Basic slash chain |

The R1 `TryCast` names were the **enum** names, not a second combat system. No remap in this pass.

| Check | Result |
|---|---|
| Mapping identified | **PASS** |
| Functions left as designed | **PASS** |

---

## 6. Soak and regression

| Test | Result | Evidence |
|---|---|---|
| Hold R and move ~20 s | **PASS** | unscaled time 19.5 → 48.8 s; sprite stayed `Hero_Attack_*`, alpha 1, enabled |
| Hero above ground | **PASS** | SpriteSorter +4000; renderer never null |
| Canopy fade | **PASS** | under `Tree_A`, alpha **0.4** on real portrait |
| Labels not removed | **PASS** | counts above |
| Console | **PASS** | 0 errors after the soak |
| L1 Camp | **PASS** | `Playing/Camp`, sandbox flag false |
| L2 Trail + exit gate | **PASS** | `Playing/Trail`, gate present |
| L3 AbandonedCamp | **PASS** | |
| L4 GoblinOutpost | **PASS** | |
| 20 s combat **video** | **NOT TESTED** | no Recorder package |

---

## Files

- `Assets/Editor/AspectGameViewHelper.cs` (already selects FixedResolution via `SizeSelectionCallback`; verified this pass)
- `Assets/Editor/R1SandboxBuilder.cs` — sandbox repaint only
- `Assets/Scenes/ReferenceCombatSandbox.unity` — ground + `DirtCorners`
- `Assets/Scripts/Enemy/EnemyWorldUI.cs` — sandbox spacing only when `ReferenceSandboxController.IsActive`

**STOP.** Sandbox visuals only. No rollout to Level 1–4.
