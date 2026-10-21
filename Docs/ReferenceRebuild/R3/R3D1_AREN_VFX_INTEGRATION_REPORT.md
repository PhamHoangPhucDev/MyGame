# R3D.1 — Aren VFX Integration + Portrait QA

**Date:** 2026-10-20 · **Branch:** `reference_rebuild_r2c`  
**Hero:** Aren (R2C) · **No Phase4 · No other classes · No Phase 12E · No full-repo commit**

---

## 1. Repo status / task scope

### Checkpoint (task-only)
| Item | Path |
|---|---|
| Script backup | `IncomingAssets/backup_before_R3D1_ArenVfx/` |
| Parent work | Continues from R3D (`5ae8bdb` lineage; dirty tree not committed) |

### In scope (this task)
- `Assets/Art/Rebuild/R2C/Aren/VFX/*` (+ Resources mirror)
- `Assets/Editor/ArenKitVfxImporter.cs`
- `Assets/Scripts/FX/ArenKitVfx.cs`, `KitVfx.cs`
- `Assets/Scripts/Combat/StatusStun.cs`, `ClassKit.cs`, `ClassCombat.cs`, `PlayerCombat.cs`
- `Assets/Scripts/Player/EquipmentVisual.cs`
- `Assets/Scripts/Level/ReferenceSandboxController.cs`
- `Assets/Scripts/Camera/AspectProfileService.cs`
- `Assets/Scenes/ReferenceCombatSandbox.unity` (Ground tile expand)
- `Docs/ReferenceRebuild/R3/R3D1_*`

### Out of scope / left dirty (not committed)
Unrelated `.claude/`, `.cursor/`, R2C1 shots, other Docs, Phase4 archive notes, prior R3 scripts already modified in tree.

---

## 2. Files changed (summary)

| File | Change |
|---|---|
| `ArenKitVfxImporter.cs` | Slice 10 sheets per manifest; mirror to Resources |
| `ArenKitVfx.cs` | Load sheets; Thrust/Bash/Dawn/Whirl/Bulwark/Dust/StunStars |
| `KitVfx.cs` | `PlayAttached` + loop for stun/bulwark |
| `StatusStun.cs` | Goblin/Gobrider stun + stars; no stack; restores AI |
| `ClassKit.cs` | `ShieldBash` / `Charge` behaviours + `FillDesignedAren()` |
| `ClassCombat.cs` | Aren F1 → `FillDesignedAren` + `PreferDesignedSkills` |
| `PlayerCombat.cs` | Shield Bash cone+stun; Charge dust; VFX hooks; unscaled waits |
| `EquipmentVisual.cs` | Hide equip slash on R3 when Thrust active |
| `ReferenceSandboxController.cs` | Camera bounds from Ground tiles |
| `AspectProfileService.cs` | Don’t overwrite sandbox Ground bounds |
| Sandbox scene | Ground filled (+315 `GrassSoftTile`) |

---

## 3. VFX imported

**Source:** `Docs/ReferenceRebuild/R2/R2C_OriginalHero/aren_kit/sheets/`  
**Unity:** `Assets/Art/Rebuild/R2C/Aren/VFX/`  
**Runtime mirror:** `Assets/Resources/VFX/ArenKit/`

| Sheet | Slice | Pivot | Notes |
|---|---|---|---|
| Aren_Thrust | 80×80 · 3×3 | (0.5, 0.375) | R3 finisher |
| Aren_ShieldBash | 64×64 · 3×3 | (0.5, 0.375) | Skill 2 |
| Aren_Dawn | 96×96 · 3×3 | (0.5, 0.3333) | Skill C |
| Aren_WhirlRing_Back/Front | 64×48 · 4×1 | (0.5, 0.2917) | Skill 4 layers |
| Aren_Bulwark_Back/Front | 48×48 · 8×1 | (0.5, 0.1667) | Skill V layers |
| Aren_ShieldGlint | 16×16 · 2×1 | center | Bash glint |
| Aren_StunStars | 16×16 · 4×1 | center | Stun loop |
| Aren_ChargeDust | 16×16 · 3×1 | (0.5, 0.0625) | Dash/Charge |

Import: PPU **16**, **Point**, no mipmap, uncompressed, alpha transparency, Multiple mode. Menu: `Game01/R3D1/Import Aren Kit VFX Sheets` → **10/10**.

---

## 4. Key mapping (code-verified)

| Key | `SkillId` | Legacy (no kit / campaign) | Sandbox Aren designed kit |
|---|---|---|---|
| **R** | `Sword` | 3-hit slash | Same + **Thrust** on hit 3 |
| **2** | `FireDash` | Flame Burst | **Shield Bash** (+ stun) |
| **3** | `MultiSlash` | Dash Slash | **Charge** (same CD/dmg band) + ChargeDust |
| **4** | `MawBite` | Whirlwind crescents | Whirlwind + **WhirlRing Back/Front** |
| **V** | `Bone` | Guard shockwave | Guard + **Bulwark Back/Front** |
| **C** | `Mace` | Crescent | Crescent + **Aren_Dawn** |

Skill **3** has no unique new sheet beyond **ChargeDust** (per pack) — no invented art.

---

## 5. Skills with VFX attached

| Skill | VFX | Status |
|---|---|---|
| R hit 3 | Aren_Thrust | Sandbox Active |
| 2 Shield Bash | ShieldBash + Glint + ChargeDust | Sandbox Active |
| 3 Charge | ChargeDust trail | Sandbox Active |
| 4 Whirl | WhirlRing Back (−3) / Front (+3) | Sandbox Active |
| V Guard | Bulwark appear→loop→break | Sandbox Active |
| C Dawn | Aren_Dawn | Sandbox Active |
| Stun | StunStars on mob | When stun applied |

Gate: `ArenKitVfx.Active` (= `PreferDesignedSkills && Ready`). **Campaign PreferDesignedSkills=false** → legacy VFX path.

---

## 6. Stun logic

- Component: `StatusStun` (pattern like `StatusBleed`).
- Eligible: name contains Goblin / Gobrider.
- Duration: Goblin **~0.32s** (clamp 0.25–0.40); Gobrider **~0.20s** (0.15–0.25).
- Refresh = max remaining (no stacked components).
- While active: `EnemyController.enabled=false`, velocity cleared, **StunStars** looped above head.
- On end / death / disable: AI restored, stars destroyed.
- Applied from Shield Bash cone hit (`ApplyConeDamage`).

Verified: `FinishShieldBash` → HP 60→30, `stun=True`, `ai.enabled=False`.  
Coroutine waits use **unscaledDeltaTime** (agent/unfocused Play Mode).

---

## 7. Dark green void (P0)

| | |
|---|---|
| **Cause** | `Main Camera` clear color `#38522E` showing where **Ground** tilemap ended. Portrait view height (~16 wu) exceeded Ground (~y −8…10) when camera followed Aren north. Not a Pixel Perfect bug. |
| **Fix** | (1) Expand Ground with `GrassSoftTile` → bounds (−10,−12) size 21×27 (+315 cells). (2) Sandbox `ClampCameraToGround` from tilemap world AABB. (3) Aspect service no longer overwrites sandbox bounds. |
| **Not done** | Changing background color alone (forbidden). |
| Evidence | `R3D1_shots/void_before_ref.png` (R3D void) vs `void_after_north.png` |

---

## 8. Label overlap

R3D keep-out retained (wider 104×36 / sandbox 118×42, smooth nudge).  
Pack evidence reused: before raw clutter → after resolve (`labels_before.png` / `labels_after.png`).  
Name / Lv / HP never culled.

---

## 9. HUD Campaign 9:16

| Check | Result |
|---|---|
| Class debug OnGUI | **Sandbox only** (`ClassCombat`) — absent on campaign smoke |
| Campaign kit | `kit=False`, `PreferDesignedSkills=False` |
| Portrait 576×1024 HUD polish | **NOT TESTED** full campaign UI pass this session (smoke only) |
| Victory / GO / Retry | **NOT TESTED** |

---

## 10. Tests

| Check | Result | Evidence |
|---|---|---|
| VFX import 10/10 | **PASS** | Console `[ArenKitVfx] Imported/sliced 10/10` |
| Sandbox PreferDesigned + ShieldBash map | **PASS** | `kit2=Shield Bash/ShieldBash` |
| Stun Goblin | **PASS** | FinishShieldBash → stun+AI off |
| Whirl / Bulwark layers | **PASS** | Shots |
| Portrait 576×1024 | **PASS** | Screen confirm + shots |
| Landscape combat | **PASS** | `landscape_combat.png` (1920×1080) |
| Ground void north | **PASS** | Expanded tiles + clamp |
| Console errors (session) | **PASS** | 0 errors on import/smoke |
| Campaign ClassCombat absent | **PASS** | Smoke eval |
| Campaign designed kit OFF | **PASS** | PreferDesigned=false |
| R combo / HitStop / KB | **NOT RE-MEASURED** (logic unchanged except R3 Thrust VFX) |
| Tree occlusion | **NOT TESTED** |
| Level 1–4 full | **NOT TESTED** |
| Victory / GameOver / Retry | **NOT TESTED** |
| Save/Load | **NOT TESTED** |

---

## 11. Rollout status

| Stage | Status |
|---|---|
| Sandbox VFX + Shield Bash | **ON** (`ClassCombat` Aren F1) |
| Campaign smoke | **Done** — no ClassCombat; designed kit off |
| Enabled for L1–4 designed kit | **NO** — keep legacy skill 2 Flame Burst until approved |
| Sheets available in build | **YES** (Art + Resources) |

To enable L1–4 later: set `ArenKitVfx.PreferDesignedSkills=true` from a campaign bootstrap **or** call `FillDesignedAren` on an Aren kit assigned to PlayerCombat — after explicit approval.

---

## Shots

`Docs/ReferenceRebuild/R3/R3D1_shots/`

- `portrait_combat.png`
- `landscape_combat.png`
- `shield_bash_stun.png`
- `whirl_ring_layers.png`
- `bulwark_layers.png`
- `void_before_ref.png` / `void_after_north.png`
- `labels_before.png` / `labels_after.png`

---

## Manual video

Unity MCP still cannot record video. Sandbox: Aren F1 designed kit, Portrait 576×1024 — ready for your capture of Bash stun / Whirl / Bulwark.

---

**STOP — awaiting your acceptance.** No R4 / Phase 12E.
