# R2C — Hero Visual Rebuild (Sandbox only)

**Date:** 2026-10-19  
**Branch:** `reference_rebuild_r2c` (from `reference_rebuild_r1` @ `abdeac8`)  
**Scope:** ReferenceCombatSandbox only. Level 1–4 Hero sheet **restored**. No Goblin/Gobrider. No Level 5.

---

## 1. Backup / audit

| Item | Finding |
|---|---|
| Checkpoint | Branch `reference_rebuild_r2c` created before the R2C import |
| Hero Prefab | **None.** Hero is scene object `Player` |
| Animator | Phase 4 `Assets/Art/GoblinCampPhase4/Animators/Hero.controller` left in place |
| Sandbox controller | `Assets/Art/Rebuild/R2/Hero/Animators/Hero_R2.controller` |
| PlayerCombat | Unchanged. R hold + `slashImpactDelay` 0.12 s still own damage |
| Sword | Child `Sword` on `Player`. `PlayerVisual.LateUpdate` sorts it from body order (rest = body−1). During body attack the child is hidden so the sheet pose is the weapon |
| SpriteSorter | Unchanged (`base + 4000 + round(-y*100)`) |
| Phase 4 pixels | Restored from `IncomingAssets/backup_before_R2B/Hero_Sheet.png` (hash `6E5A1F24…`). R2b (`6628752F…`) lives only under Rebuild |

Prior commit `abdeac8` had overwritten Phase 4 `Hero_Sheet.png` with R2b. This pass **reverted those pixels** and did not rewrite the Phase 4 `.meta` GUID.

---

## 2. Import

**Source:** `Docs/ReferenceRebuild/R2/R2B/Hero_Sheet_R2b.png` (approved sheet; no separate Canva sword PNGs were in the batch).

**Dest:** `Assets/Art/Rebuild/R2/Hero/Hero_Sheet.png`

| Check | Result |
|---|---|
| Size | **128×288** (grid OK). Reject path exists if not 32-aligned |
| Slices | **33 × 32×32**, names copied from Phase 4 (`Hero_Sheet_Hero_Idle_Down_0` …) |
| PPU | 16 |
| Filter | Point |
| Mipmaps | Off |
| Compression | Uncompressed |
| Alpha | Transparency on |
| Pivot | Same as Phase 4 slices (**0.5, 0.0625**) |

---

## 3. Animation states (`Hero_R2`)

| State | Clip | Notes |
|---|---|---|
| Hero_Idle_Down / Up / Side | remapped to R2 sheet | PASS |
| Hero_Walk_Down / Up / Side | remapped to R2 sheet | PASS |
| Hero_Attack_Down / Up / Side | **3-frame R2 clip**, not the old windup clip | Keys **0.00 / 0.12 / 0.19 s**, clip stop **0.26 s**. Impact sprite e.g. `Hero_Attack_Side_1` at 0.12 s |
| Hero_Hurt_Down / Up / Side | copied; sprites still **Phase 4 HeroExtra** | Clip plays; palette not rebuilt |
| Hero_Death | copied; sprites still **Phase 4 HeroExtra** | Same |

Windup clips were copied but **not** assigned to Attack states, so the strike reads the new sheet.

Damage, HitStop, and shake stay on `PlayerCombat` (one hit per cast). Hold-R was not retimed.

---

## 4. Dual aspect (no per-aspect sprite scale)

| Profile | PPC | Hero cell | Viewport fraction |
|---|---|---|---|
| Landscape live | 480×270, ortho 8.4375, screen 1920×1080 | 32 px → **2.00 wu** | **0.1185** |
| Portrait math | 144×256 → ortho 8.0 | same 2.00 wu | **0.1250** |
| Δ | | | **+0.0065** (~+5.5%) |

Live Game View stayed 1920×1080, so portrait ortho did not switch in this capture. PPC refs did switch to 144×256 when `Apply(Portrait)` ran.

---

## 5. Tests

| # | Test | Result |
|---|---|---|
| 1 | Idle | **PASS** — `Hero_Idle_*` from Rebuild sheet |
| 2 | Walk | **PASS** — captured |
| 3 | Continuous move | **PASS** — external input |
| 4 | R tap | **PASS** via combat (hold path) |
| 5 | R hold 10 s | **NOT TESTED** this pass (hold path confirmed, not a full 10 s soak) |
| 6 | 3-hit chain | **PASS** (combat chain unchanged; body clip replays per swing) |
| 7 | Hero hurt | **PARTIAL** — Hurt states exist; `TakeDamage` during attack lock stayed on Idle (hurt does not cancel swings) |
| 8 | Enemy knockback | **NOT TESTED** (not re-measured) |
| 9 | HitStop | **NOT TESTED** live this pass (system unchanged) |
| 10 | Tree occlusion | **NOT TESTED** this pass |
| 11 | Hero sorting | **PASS** — body order follows Y via SpriteSorter |
| 12 | Sword sorting | **PASS** — child hidden in attack (no orphan sword); rest order body−1 |
| 13 | Mob labels | **PASS** — labels == living enemies in sample |
| 14 | HP bars | **PASS** (same EnemyWorldUI) |
| 15 | Skills 2/3/4/V/C | **PASS** — FireDash, MultiSlash, MawBite, Bone, Mace. Sword false only on cooldown |
| 16 | Console | **PASS** — 0 errors |

---

## 6. Screenshots

`Docs/ReferenceRebuild/R2/R2C/`

| File | Content |
|---|---|
| `unity_idle_land.png` | Idle in sandbox |
| `unity_walk_land.png` | Walk |
| `unity_attack_land.png` | Attack (early) |
| `unity_attack_strike.png` | Strike frame `Attack_Side_1` |

Reference stills to compare (not composited into one image):

- `Docs/ReferenceRebuild/R2/ref_strips/strip_C05_hero_idle_1s.png`
- `Docs/ReferenceRebuild/R2/ref_strips/strip_C17_run_right.png`
- `Docs/ReferenceRebuild/R2/ref_strips/strip_C25_attack.png`

**Video 20–30 s:** NOT TESTED.

---

## 7. Files touched

- Restored pixels: `Assets/Art/GoblinCampPhase4/Hero_Sheet.png` (from backup)
- New: `Assets/Art/Rebuild/R2/Hero/**` (sheet, clips, `Hero_R2.controller`)
- Sandbox only: `Assets/Scenes/ReferenceCombatSandbox.unity` (`PlayerVisual.bodyController`)
- Editor: `Assets/Editor/R2CHeroSandboxSetup.cs`
- Report: this file

**Not changed:** Level 1–4 scene, GameProgress, LevelFlow, Enemy AI, Mob Labels, camera core, skill mapping, damage numbers, Save/Load, Phase 12E / Level 5.

---

## 8. Gaps before Goblin / Gobrider

1. Hurt/Death still use Phase 4 HeroExtra palette.
2. No `Hero_Sword_Idle` / `Hero_Sword_Swing` sheet in the delivered batch — rest sword is still the placeholder, hidden during the body attack.
3. Portrait 576×1024 was not the live Game View during capture.
4. No 60 s “body never vanishes” soak on this controller.

**STOP.** Sandbox hero only. Waiting for review before Goblin and Gobrider.
