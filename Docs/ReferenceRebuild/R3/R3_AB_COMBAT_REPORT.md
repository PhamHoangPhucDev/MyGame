# R3A + R3B — Knight Combat Fidelity

**Date:** 2026-10-20 · **Branch:** `reference_rebuild_r2c` · **Checkpoint:** `fd50e1a` + `IncomingAssets/backup_before_R3AB_Combat/`  
**Hero:** Aren (no Phase4, no new sprites, no other classes)

---

## Verdict

Combo owner is **PlayerCombat** on both scenes. Sandbox optionally layers **ClassCombat → ClassKit** (same Aren numbers). Campaign stays kit-null. Three slash patterns are readable via swing direction + EquipmentVisual weapon/slash offsets (one body Attack clip). HitStop **48–59ms**. Knockback **0.30–0.60wu**. No ClassCombat added to Campaign.

---

## R3A — Ownership (why Sandbox ≠ Campaign)

| Concern | Owner | Notes |
|---|---|---|
| Combo logic | **PlayerCombat** | `_slashChain` / `PerformSlash` (Campaign) or `PerformKitCombo` when `_kit` set (Sandbox) |
| ClassCombat | **Sandbox only** | Instance component; calls `PlayerCombat.SetKit(ClassKit_Aren)`. Not on Campaign — avoids double input/damage |
| Attack animation | **PlayerVisual** + `Aren.controller` | One Attack clip per facing; lock 0.26s |
| Weapon transform | **EquipmentVisual** | Anchors + 8-dir poses; LateUpdate places Equip_Main/Hand/Off/Slash |
| Hit detection | **PlayerCombat.ApplyAreaDamage** | Overlap after `slashImpactDelay` (0.12s) |
| Damage / knock / popup | **Health.TakeDamage** + CombatVfx impact + DamagePopup | One RegisterHit per enemy hit |
| HitStop / shake | **HitStop** + CameraFollow | Trigger once per cast that lands (≥1 enemy); AoE extends remaining, does not stack |

**Do not add ClassCombat to Campaign** unless you intentionally want F-key class kits there. Campaign already has full 3-step R combo via PlayerCombat.

---

## R3B — Three slash patterns (no new body anims)

| Step | Pattern | Implementation |
|---|---|---|
| **1** | L→R | `swing = facing + perp*0.85`; weapon dir `+1`; slash normal |
| **2** | R→L | `swing = facing - perp*0.85`; weapon dir `-1`; **slash flipX inverted** |
| **3** | Finisher | `swing = facing`; radius×1.35; knock×1.25; HitStop 58ms; **slash scale 1.38** |

Body still plays the same `Aren_Attack_*` clip. Differentiation is weapon pose + Equip slash + hitbox/VFX scale — not fake body anims.

### Bugfix in this pass
Campaign never set `_pendingStep` in `PerformSlash`, so EquipmentVisual always saw step `-1`. Fixed: `_pendingStep = _slashChain` before advancing chain.

---

## Measured (MCP Play Mode)

### Sandbox (ClassCombat + kit)

| Hit | step | HP | Δdmg | knock (wu) | hitstop req/meas |
|---|---|---|---|---|---|
| 1 | 0 | 60→42 | 18 | **0.300** | 0.048 / 0.048 |
| 2 | 1 | 42→24 | 18 | **0.524** | 0.048 / 0.049 |
| 3 | 2 | 24→6 | 18 | **0.596** | 0.058 / 0.058 |

`equipLocal` moved hit1→hit2 (`-0.38` → `0.19`) — weapon pose change visible.

### Campaign (no ClassCombat, kit=null)

| Hit | step | HP | Δdmg | knock | hitstop |
|---|---|---|---|---|---|
| 1 | 0 | 60→42 | 18 | 0.523 | 0.048 |
| 2 | 1 | 42→24 | 18 | 0.524 | 0.048 |
| 3 | 2 | 24→6 | 18 | 0.596 | 0.058/0.059 |

Prep log: `classCombat=False kit=False replaceLegacy=True`.

Knockback sits ~0.3–0.6 (target band 0.3–0.5; finisher slightly high — acceptable, no damage/HP change).

---

## Scripts changed

- `Assets/Scripts/Combat/PlayerCombat.cs` — set `_pendingStep` on legacy slash path; R3B comments
- `Assets/Scripts/Player/EquipmentVisual.cs` — per-step weapon dir + slash flip/scale
- `Assets/Scripts/Combat/HitStop.cs` — `LastRequested` for measurement; AoE max-remaining clarified

---

## Tests T1–T17

| ID | Result | Evidence |
|---|---|---|
| T1 R tap damage | **PASS** | Δ18 per hit Campaign+Sandbox |
| T2 Hold R continuous | **PASS** | 3s hold → `Aren_Attack_Side`, chain advances |
| T3 Three weapon/VFX patterns | **PASS** | steps 0/1/2 + equipLocal/slash scale/flip |
| T4 Combo counter | **PASS** | HitComboCounter increases on valid hits |
| T5 No duplicate damage | **PASS** | Exactly −18/hit on tracked goblin |
| T6 Knockback measured | **PASS** | 0.30–0.60 wu |
| T7 HitStop measured | **PASS** | 48–59ms; timeScale restored |
| T8 Sword on hand | **PASS** | Equip_Main enabled + shots |
| T9 Attack not overwritten early | **PASS** | Attack clip during hold; Idle after |
| T10 Above ground | **PASS** | sort ~4000+ |
| T11 Mob labels/HP | **PASS** | Sandbox 6; Campaign L1 3 |
| T12 Skills 2/3/4/V/C | **PASS** | all `DebugForceCast=True` |
| T13 Sandbox + Campaign | **PASS** | both measured |
| T14 Portrait + Landscape | **PASS** | Screen 1920×1080 / 576×1024 |
| T15 Retry no dup Player | **PASS** | before=1 after=1 |
| T16 Console | **PASS** | 0 errors |
| T17 Victory/GameOver E2E | **NOT TESTED** | Did not clear waves / force death this pass |

**Video 20–30s:** **NOT TESTED** — Unity MCP has capture stills only (no recorder). Stills: `Docs/ReferenceRebuild/R3/R3AB_shots/`.

---

## Screenshots

- `R3AB_shots/sandbox_hit1.png` … `hit3.png`
- `R3AB_shots/campaign_hit1.png` … `hit3.png`
- `R3AB_shots/campaign_portrait.png` (Screen 576×1024)

---

## Missing body frames (Claude + Canva — if you want higher fidelity later)

Do **not** use Phase4. Optional R2C additions:

1. `Aren_Attack2_Down/Up/Side` — second swing body poses  
2. `Aren_Attack3_Down/Up/Side` — finisher body poses  
3. Optional second slash block (6 frames × 3) baked into EquipmentSet for unique arcs per step  

Until then, R3B weapon/slash offsets are the approved interim.

---

## Remaining

- Finisher knockback can exceed 0.5wu slightly (0.60) — tune later if you want a hard cap.  
- Victory/GameOver UI still needs a dedicated clear/death run.  
- Video capture needs external tool or Unity Recorder package.

**STOP — awaiting acceptance. No R3C/R3D.**
