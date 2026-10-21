# R3D — Knight Reference Combat Matching

**Date:** 2026-10-20 · **Branch:** `reference_rebuild_r2c` · **Checkpoint parent:** `5ae8bdb` · **Backup:** `IncomingAssets/backup_before_R3D_ReferenceMatch/`  
**Hero:** Aren (R2C_OriginalHero) · **No Phase4 · No new Aren body sheets · No Phase 12E · No Claude**

---

## Verdict

Portrait **9:16** Play Mode confirmed (`Screen=576×1024`, `Camera.aspect=0.5625`, **PPC 144×256**, **PPU 16**). R combo three steps are measurable via **weapon Z / slash tip / flip / finisher scale** on one body Attack clip. Mob Name+Lv+HP **never culled**; overlap pairs cut sharply after screen-space resolve, with **1 residual pair** in a packed 6-goblin cluster. Difference matrix vs clips **C03 / C06 / GameReference 06** filed. MCP **cannot record video** — Play Mode left ready for manual capture.

---

## Checkpoint

| Item | Path / hash |
|---|---|
| Git parent | `5ae8bdb` |
| Script backup | `IncomingAssets/backup_before_R3D_ReferenceMatch/` (`EnemyWorldUI`, `DamagePopup`, `EquipmentVisual`, `PlayerCombat`) |

---

## 1. Baseline read

| Doc / system | Role |
|---|---|
| `R3_AB_COMBAT_REPORT.md` | Combo ownership, measured HitStop/KB |
| `R3C_COMBAT_IMPACT_REPORT.md` | Tip slash, spark, popup lanes |
| `PlayerCombat` / `EquipmentVisual` / `PlayerVisual` | R cast, weapon place, body Attack |
| `CombatVfx` / `DamagePopup` / `EnemyWorldUI` | Spark, popup, labels |
| `AspectProfileService` | Dual aspect PPC |

---

## 2. Portrait 9:16 — live confirmation

| Probe | Result |
|---|---|
| `Screen.width × height` | **576 × 1024** |
| `Camera.main.aspect` | **0.5625** (9:16) |
| Aspect profile | Portrait manual/auto |
| PPC ref | **144 × 256** |
| Assets PPU | **16** |
| World size | 9.00 × 16.00 |

**Play scenarios (Sandbox, evidence in console + shots):**

| Scenario | Result |
|---|---|
| Aren idle + R | PASS — cast + slash window ~0.15–0.25s |
| Aren move + R | PASS — sticky aim + same combo path (not separately filmed) |
| 1 Goblin | PASS — `combat_1mob.png` |
| 5–6 Goblin | PASS — `combat_6mob.png` |
| Hold R ≥20s | PASS — `SetSwordHeld(true)` ~38s unscaled, chain advanced, **0 console errors** |

Reference stills used: `Docs/ReferenceRebuild/R2/R2A_HeroMatch/ref_timecoded/strip_R03_*`, `strip_R06_*`, `GameReference/frames/06_sword_slash_40_0s.jpg`.  
**Video:** Unity MCP has no video recorder — **NOT CAPTURED**. Scene state: `ReferenceCombatSandbox` + Portrait Game view 576×1024 for manual record of clips 03/06/12.

---

## 3. Per-hit R measurement (Portrait)

Body animation: **one** `Aren_Attack_*` clip for all steps. Differentiation is equip/VFX only.

| Hit | Body clip | Weapon Z | Main local (x) | Slash tip (x/y) | Slash scale | Slash flip | Hand↔sword gap | Hit time | HitStop | Knock (wu) | ΔHP |
|---|---|---|---|---|---|---|---|---|---|---|---|
| **R1** L→R | `Aren_Attack_Side` | **+18 / −18** (by flip) | ~−0.19 / 0.06 | **±0.18 / 0.05** | 1.00 | normal | ~0.13–0.40 | 0.12s | **0.048** | ~0.70 | 18 |
| **R2** R→L | same | **−22 / +22** | pose swap | tip inverted + **flipX invert** | 1.00 | inverted | same | 0.12s | **0.048** | ~0.20–0.29 | 18 |
| **R3** Finisher | same | **±28** | wider dir+2 | tip ~±0.08 / 0.10 | **1.55** | normal | same | 0.12s | **0.058** | ~0.02–0.07* | 18 |

\*Hit3 knock low when target nearly dead / blocked — same note as R3C; **no damage/KB buff**.

**Why steps look similar by eye:** single body Attack sheet; slash ON only ~100ms; finisher scale 1.55 still much smaller than reference cyan crescents → weapon Z/flip is the main readable cue.

**Cursor-only tweaks this pass:** finisher slash scale 1.55 + tip; DamagePopup 4-lane lift; EnemyWorldUI smooth nudge + wider keep-out. **No fake body frames.**

---

## 4. Mob labels (Name / Lv / HP kept)

Rules held: shared canvas, follow knockback, recycle on death, **no per-mob Canvas**, **no cull/fade by distance**.

| Pack | Raw overlap pairs (anchors) | After layout | Notes |
|---|---|---|---|
| 1 mob | 0 | 0 | — |
| 3 mob packed | 1 | **0** | Resolved |
| 6 mob packed | 3–5 (force-zero nudge shot) / raw **4** | **1** | Residual when 6 share one screen pocket |

Shots: `R3D_shots/labels_before_6mob.png`, `labels_after_6mob.png`.  
Residual: dense ring around Aren — vertical stack hits `maxNudge` clamp; labels stay visible, may still partially overlap text glyphs.

---

## 5. Difference matrix (summary)

Full CSV: `Docs/ReferenceRebuild/R3/R3D_DIFFERENCE_MATRIX.csv`.

| ID | Topic | Priority | Unity vs ref | Fix |
|---|---|---|---|---|
| A | Hero size | P1 | Aren smaller in viewport | Canva Attack frames optional; no zoom cheat |
| B | Attack rhythm | P0 body | 3 code steps, 1 body clip | **Canva Attack2/3** |
| C | Slash arc | P1 | Thin/small vs cyan crescent | Canva slash sheets |
| D | Hit spark | P2 | Compact spark OK | Cursor OK |
| E | Knockback | P2 | Hit3 soft | No buff without proof |
| F | Combo counter | P1 | Missing `xN HITS` | Cursor HUD (deferred) |
| G | Damage popup | P2 | Lanes raised | Cursor done |
| H | Mob labels | P0 residual | 1 pair @6 | Cursor keep-out; never hide |
| I | Density | P1 | 6-mob playable | Labels + art |
| J | Camera | P2 | Framing OK | No zoom |
| K | HUD 9:16 | P1 | Icons small / sandbox class debug line | Cursor scaler + Canva icons |

No reference watermarks/editor chrome copied into game UI.

---

## 6. Cursor fixes applied

| File | Change |
|---|---|
| `EnemyWorldUI.cs` | Stable Y-sort, 5-pass resolve, smooth nudge, wider keep-out, `CountOverlappingPairs` |
| `DamagePopup.cs` | 4 lanes, lift ≥1.05wu clear of Name/HP |
| `EquipmentVisual.cs` | Finisher slash scale **1.55**, tip tweak |

Locked unchanged: Aren sheets, damage numbers, cooldowns, LevelFlow, GameProgress, skills 2/3/4/V/C, HitStop timings (48/58ms).

---

## 7. Regression

| Check | Result | Evidence |
|---|---|---|
| Combat 9:16 | **PASS** | Screen/PPC/aspect + shots |
| Combat 16:9 | **PASS** | Landscape apply, Screen 1280×720 / 1920×1080 aspect 1.7778, `hud_16x9.png` |
| R Hold | **PASS** | ≥20s hold, no errors |
| 3-hit combo | **PASS** | Measured rows |
| Multi-target | **PASS** | AoE path unchanged; 6-mob cast |
| HitStop | **PASS** | 0.048 / 0.048 / 0.058 |
| Knockback | **PASS** | Measured; Hit3 soft noted |
| SFX | **PASS** | Wired (R3C); not re-audited waveform |
| Label + HP | **PASS** | Always on; residual 1 pair @6 |
| Hero sorting | **NOT RE-TESTED** this pass (R2C lock) |
| Level 1–4 | **NOT TESTED** (Sandbox only) |
| Game Over / Victory | **NOT TESTED** |
| Save/Load | **NOT TESTED** |
| Console | **PASS** | 0 errors / 0 warnings in session GT |

---

## 8. Artifacts

| Artifact | Path |
|---|---|
| Report | `Docs/ReferenceRebuild/R3/R3D_REFERENCE_MATCH_REPORT.md` |
| Matrix | `Docs/ReferenceRebuild/R3/R3D_DIFFERENCE_MATRIX.csv` |
| Shots | `Docs/ReferenceRebuild/R3/R3D_shots/` |
| | `hit1.png` `hit2.png` `hit3.png` |
| | `combat_1mob.png` `combat_6mob.png` |
| | `labels_before_6mob.png` `labels_after_6mob.png` |
| | `hud_9x16.png` `hud_16x9.png` |

**Note:** Early `hit1–3` captures can include Game-view letterboxing when resolution flips mid-session; prefer freeze-on-slash method for manual retakes. MCP **video: not available**.

---

## ASSETS REQUIRED FROM CANVA

Only items that **cannot** be closed with Cursor timing/transform alone:

1. **Aren Attack2 / Attack3 body sheets (highest impact)**  
   - Keep base: `Docs/ReferenceRebuild/R2/R2C_OriginalHero/` approved Idle/Walk/Attack1/Hurt sheets — **do not replace**.  
   - Need: **8-dir** Attack2 (counter-swing / R→L body) and Attack3 (finisher plant).  
   - Suggested frames: **4–6** per direction (windup / strike / recover).  
   - Pose: same silhouette/palette as Aren Attack1; clearer torso twist + weight shift so R1/R2/R3 read without relying on weapon Z alone.

2. **Larger slash crescents (optional P1)**  
   - Wider cyan/white arc matching C03/C06 (~2–3× current `Aren_Slash_*` coverage) with tip pivot at hand.  
   - Source to align: existing `Aren_Slash_*` in R2C set for palette/pivot.

3. **HUD skill icons (optional P1 for 9:16)**  
   - Larger, higher-contrast R/2/3/4/V/C glyphs readable at 576×1024 — **no video watermark**.

4. **Not requested:** new classes, Hero Upgrade art, Phase 12E, Phase4 resurrection.

---

## STOP

**R3D complete.** Do not start R4 or Phase 12E.
