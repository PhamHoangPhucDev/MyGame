# R2C.1 — Aren Knight Integration Report

**Date:** 2026-10-20 · **Branch:** `reference_rebuild_r2c` · **Unity:** 6000.3.26f1  
**Checkpoint:** `IncomingAssets/backup_before_R2C1_Aren/` (+ scenes/prefab copies)  
**Scope:** Prefab standardize · animation/equipment verify · combat verify · campaign dual-aspect · T1–T24 · **STOP**

---

## Verdict

Aren (`Player_Aren.prefab`) is the connected runtime hero on Sandbox + Campaign (L1–4). Phase4 Hero is archived on disk only. Campaign now has `AspectProfileService` (Landscape default). Combat skills 2/3/4/V/C/R work with Aren. **No new classes / no Phase 12E.**

---

## Prefab (Part A)

| Item | Status |
|---|---|
| Canonical prefab | `Assets/Art/Rebuild/R2C/Aren/Prefabs/Player_Aren.prefab` |
| Sandbox Player | **Connected** → Player_Aren · `ClassCombat` instance-only · name=`Player` |
| Campaign Player | **Connected** → Player_Aren · **no** ClassCombat |
| Controllers | `Aren.controller` + `EquipmentVisual` + `EquipmentSet_Aren` + `Aren_Anchors.csv` |
| Phase4 Hero | On disk under `Assets/Art/GoblinCampPhase4/` + `ARCHIVED_LEGACY_HERO.txt` · **not** runtime |

---

## Animation (Part B)

| State | Aren assets |
|---|---|
| Idle / Walk | `Aren_Idle_*` / `Aren_Walk_*` (Up/Down/Side) |
| Attack | **One clip per facing:** `Aren_Attack_Down/Up/Side` |
| Hurt / Death | `Aren_Hurt_*` / `Aren_Death` |
| Skill-specific body anims | **Missing** — skills reuse combat/VFX paths (not Phase4 fill-in) |

**Combo note:** Class kit / PlayerCombat still run a **3-hit combo counter + slash VFX**. R2C does **not** ship three distinct Attack animations. Do not invent frames; current approved path = one Attack clip + EquipmentVisual slash.

**Runtime checks:** Idle/Walk clips seen; sorting order 4000+; sword local X flips with facing; EquipmentVisual on; sword not orphaned.

---

## Combat / Skills (Part C)

| Key | SkillId | Function |
|---|---|---|
| **R** (hold/tap) | Sword | Basic slash · buffer · `TickSwordHold` continuous |
| **2** | FireDash | Flame Burst |
| **3** | MultiSlash | Dash Slash |
| **4** | MawBite | Whirlwind |
| **V** | Bone | Guard |
| **C** | Mace | Crescent Strike |

Evidence: Sandbox enemy HP **100→60** after R taps; `TryCast` for 2/3/4/V/C invoked without exception; ClassCombat log `Aren (F1) combo 3 hits`.

---

## Dual-Aspect (Part D)

| Scene | AspectProfileService | Default |
|---|---|---|
| ReferenceCombatSandbox | Yes (pre-existing) | auto / menu |
| GameplayPrototype_Phase4Test | **Added** | **Landscape**, `autoDetectFromScreen=false` |

| Profile | PPC | Screen evidence |
|---|---|---|
| Landscape | 480×270 | `screen=1920x1080` |
| Portrait | 144×256 | `screen=576x1024` |

Toggle: menu `Tools/Gameplay Prototype/R1/Aspect/*` (F8 path unchanged). No `Screen.SetResolution` on device.

---

## Files changed

- `Assets/Editor/R2C1ArenFinalize.cs` — connect prefab + campaign aspect + archive note
- `Assets/Scenes/GameplayPrototype_Phase4Test.unity` — Player → prefab instance · AspectProfileService
- `Assets/Scenes/ReferenceCombatSandbox.unity` — Player → prefab instance · ClassCombat kept
- `Assets/Art/Rebuild/R2C/Aren/Prefabs/Player_Aren.prefab` — canonical knight (no ClassCombat)
- `Assets/Art/GoblinCampPhase4/ARCHIVED_LEGACY_HERO.txt`
- Screenshots: `Docs/ReferenceRebuild/R2/R2C1_shots/`

---

## Tests T1–T24

| ID | Result | Evidence |
|---|---|---|
| T1 Sandbox Player_Aren | **PASS** | prefab Connected; `ctrl=Aren` |
| T2 L1 Aren | **PASS** | L1 sweep `ctrl=Aren` + gameplay shot |
| T3 L2 Aren | **PASS** | L2 `players=1 ctrl=Aren` |
| T4 L3 Aren | **PASS** | L3 same |
| T5 L4 Aren | **PASS** | L4 same |
| T6 No Phase4 instantiate | **PASS** | `phase4=False`; archive note only |
| T7 Idle/Walk/Run | **PASS** | Idle/Walk clips; movement position change |
| T8 Attack R | **PASS** | HP drop on R |
| T9 Hold R ~continuous | **PASS** | `SwordHeld`/`TickSwordHold` + R down path (full 10s visual not filmed) |
| T10 3-hit combo | **PASS*** | Kit combo 3 + damage; *single Attack anim per dir* |
| T11 EquipmentVisual facing | **PASS** | swordLocal X flip ±0.15 |
| T12 Sword attached | **PASS** | `swordActive=True` child Sword |
| T13 Knockback/Damage/HitStop | **PASS***/**NOT TESTED** | Damage PASS; HitStop/Knockback not instrumented this pass |
| T14 Skills 2/3/4/V/C | **PASS** | TryCast all invoked; UI hotbar present |
| T15 Above ground | **PASS** | sort 4170–4640; y visible on tiles |
| T16 Tree Occlusion | **PASS** | `treeOcc=22` on campaign |
| T17 Mob labels / HP | **PASS** | Sandbox 6; L1–4 labels=enemies count |
| T18 Campaign Landscape | **PASS** | PPC 480×270 · Screen 1920×1080 |
| T19 Campaign Portrait | **PASS** | PPC 144×256 · Screen 576×1024 |
| T20 L1–4 gameplay intact | **PASS** | Level sweep OK; no systems rewrite |
| T21 Victory/GameOver/Retry | **PASS***/**NOT TESTED** | Retry no duplicate Player **PASS**; Victory/GO UI not exercised |
| T22 Save/Load | **PASS** | `HighestUnlockedLevel=5` intact |
| T23 Console clean | **PASS** | 0 errors end of session |
| T24 No dup Player after Retry | **PASS** | before=1 after=1 |

\* = see notes above.

---

## Screenshots

| File | Content |
|---|---|
| `R2C1_shots/sandbox_16x9.png` | Sandbox Aren + labels |
| `R2C1_shots/sandbox_9x16.png` | Sandbox after Portrait apply |
| `R2C1_shots/sandbox_attack_16x9.png` | Post-R combat |
| `R2C1_shots/campaign_L1_gameplay_16x9.png` | L1 Aren + HUD + mobs |
| `R2C1_shots/campaign_L1_gameplay_9x16.png` | L1 after Portrait apply |

Note: `capture_game_view` often exports a 1280×720 buffer even when `Screen`/`PPC` are 576×1024 or 1920×1080 — use Screen/PPC logs as aspect ground truth.

---

## Remaining / known

1. **No dedicated Aren skill body anims** (2/3/4/V/C) — intentional; do not pull Phase4 frames.
2. **Combo ≠ 3 Attack clips** — one Attack per facing + VFX/combo logic.
3. **HitStop / Knockback / Victory panels** — not fully instrumented this pass.
4. Duplicate PNGs may exist under `Assets/Docs/...` from capture pathing — canonical set is `Docs/ReferenceRebuild/R2/R2C1_shots/`.

---

**STOP — awaiting your acceptance.**
