# R3C — Aren Knight Combat Impact & Visual Polish

**Date:** 2026-10-20 · **Branch:** `reference_rebuild_r2c` · **Checkpoint:** `2555a51` + `IncomingAssets/backup_before_R3C_Impact/`  
**Scope:** Combo readability · hitbox/VFX sync · impact polish · labels/popups · HitStop/KB re-measure · SFX audit · dual-aspect · Victory/GO · **STOP**

---

## Verdict

Three R hits are more readable via weapon Z-tilt + slash tip offset/flip/scale (still **one** body Attack clip). Hit sparks on enemy; popups lifted off labels; label spacing widened. HitStop stays **48–60ms**. Campaign GameOver + Victory UI verified. SFX already wired. **No damage buff, no Phase4, no new classes.**

---

## Scripts changed

| File | Change |
|---|---|
| `EquipmentVisual.cs` | R3C A1/A2/A3: weapon Z rotation + tip-offset slash + flip/scale |
| `PlayerCombat.cs` | `SpawnHitSpark` + higher popup offset (no second slash system) |
| `CombatVfx.cs` | `SpawnHitSpark(pos, finisher)` compact flash |
| `DamagePopup.cs` | 3-lane stagger + higher lift clear of HP bars |
| `EnemyWorldUI.cs` | Wider minX/minY keep-out (labels never culled) |

Prefab / Aren sheet / controller / damage numbers: **unchanged**.

---

## R Attack timeline (real)

| Phase | Time | Owner |
|---|---|---|
| Input R | frame 0 | PlayerCombat |
| Swing SFX | cast | `Sfx.SlashSwing` |
| Body Attack start | 0 | PlayerVisual lock 0.26s (windup 0.08s) |
| Equip slash visible | Attack frames | EquipmentVisual (blade tip) |
| Active hit / damage | **0.12s** (`slashImpactDelay`) | ApplyAreaDamage |
| Hit spark + popup | on land | CombatVfx / DamagePopup |
| HitStop | on land | 48ms (A1/A2) / 58–60ms (A3) |
| Impact SFX | on land | `SlashHit` / `SlashFinisher` |
| Recovery | ~0.26s | Attack lock → Idle/Walk |

Standing / moving / facing: PlaySlashSwing aims swing; sticky auto-target preserved. Multi-goblin AoE: one damage per enemy per cast; HitStop uses max-remaining (no stack).

---

## How three slashes differ (still 1 body clip)

| Step | Weapon | Slash VFX |
|---|---|---|
| **1** L→R | dir+1, Z ≈ −18° | tip +X, normal flip |
| **2** R→L | dir−1, Z ≈ +22° | tip −X, **flip inverted** |
| **3** Finisher | dir+2, Z ≈ −28° | scale **1.42**, tip center |

Measured equipLocal (Sandbox): hit1 `(-0.19)`, hit2 `(0.19)`, hit3 `(0.31)`.

**Asset limit:** no `Aren_Attack2/3_*` body frames — listed for Claude/Canva later; not filled from Phase4.

---

## HitStop & Knockback (post-polish)

| Scene | Hit1 | Hit2 | Hit3 |
|---|---|---|---|
| Campaign ΔHP | 18 | 18 | 18 |
| Campaign HitStop | 0.048 | 0.048 | 0.058/0.060 |
| Campaign Knock | 0.50 | 0.52 | 0.12* |
| Sandbox HitStop | 0.048–0.051 | 0.049 | 0.058–0.060 |

\*Hit3 knock low when target nearly dead / against collision — R3B band 0.30–0.60 still baseline; no knockback buff applied.

`Time.timeScale` restored after each freeze (`hsActive=False` in samples).

---

## VFX / SFX

**Improved:** enemy `SpawnHitSpark` (finisher slightly larger); popup lanes; label spacing; weapon/slash readability.

**SFX present & connected** (`Assets/Resources/Audio/SFX/`):  
Swing / Hit / Finisher / EnemyHurt / EnemyDeath / skill SFX — already played from PlayerCombat / Health. No new downloads.

---

## Tests T1–T19

| ID | Result | Evidence |
|---|---|---|
| T1 R tap | **PASS** | Δ18 |
| T2 Hold R ~3s+ | **PASS** | `Aren_Attack_Side` mid-hold; chain advances |
| T3 Three patterns | **PASS** | steps 0/1/2 + equipLocal / shots |
| T4 Move+attack | **PASS** | hold while reposition (prep teleport + attack) |
| T5 Four dirs | **PASS** | Idle/Attack Side/Up clips observed |
| T6 One goblin | **PASS** | tracked HP chain |
| T7 Multi goblin | **PASS** | Sandbox labels=6, AoE popups |
| T8 No dup damage | **PASS** | −18/step on one target |
| T9 Knock/Stagger | **PASS** | displacement measured; AI Stagger path unchanged |
| T10 HitStop vs R3B | **PASS** | 48 / 48 / 58–60ms |
| T11 Shake not spam | **PASS** | CameraFollow same-frame max amp |
| T12 Labels + popups | **PASS** | labels kept; popup lift |
| T13 Skills 2/3/4/V/C | **PASS** | all True |
| T14 Portrait 9:16 | **PASS** | Screen 576×1024 |
| T15 Landscape 16:9 | **PASS** | Screen 1920×1080 |
| T16 Retry | **PASS** | players 1→1 |
| T17 Victory / GameOver | **PASS** | GO `goUi=True`; Victory `winUi=True` (clear+exit / TriggerVictory) |
| T18 L1–4 + Save | **PASS** | Aren on L1–4; unlock=5 |
| T19 Console | **PASS** | 0 errors |

**Video 20–30s:** **NOT TESTED** — MCP stills only (`R3C_shots/`). Do not treat stills as motion sign-off.

---

## Screenshots

`Docs/ReferenceRebuild/R3/R3C_shots/`  
`sandbox_hit1..3.png` · `campaign_hit1..3.png` · `campaign_portrait.png` · `campaign_victory.png`

---

## Remaining

- Optional body Attack2/Attack3 sheets (Canva) for higher fidelity.  
- External video recorder for motion review.  
- Finisher knock varies with corpse/collision — acceptable without KB buff.

**STOP — no R3D / R4 / Phase 12E.**
