# R1 — Dual-Aspect Reference Combat Sandbox Results

**Date:** 2026-10-19  
**Branch:** `reference_rebuild_r1`  
**Checkpoint:** commit `06edf43` (Phase4 art baseline restored; slash code kept)  
**Status:** Sandbox complete — **STOP**. Rebuild art not rolled onto Level 1–4.

---

## 1. Scope locks (verified)

| Lock | Result |
|---|---|
| Keep Level 1–4 scene/data | PASS — `GameplayPrototype_Phase4Test` unchanged for maps/layouts |
| Keep LevelFlow / GameProgress | PASS — sandbox disables LevelFlow; `ReferenceSandboxController.IsActive` |
| Keep Enemy AI / Combat | PASS — same `PlayerCombat` + `EnemySpawner` |
| Keep Mob Labels + HP bars | PASS — 6 labels for 6 enemies |
| Keep SpriteSorter / TreeCanopyOcclusion | PASS — trees use both |
| No Level 5 deploy | PASS — not touched |
| No Phase 4 art overwrite | PASS — hashes differ (`GrassSoft` Phase4 ≠ Rebuild) |

---

## 2. Dual-Aspect Profile

| | Landscape | Portrait |
|---|---|---|
| Aspect | 16:9 | 9:16 |
| PPC ref | **480×270** | **144×256** |
| Game View test | 1920×1080 | 576×1024 |
| PPU | 16 | 16 |
| World size | 30.0 × 16.875 | 9.0 × 16.0 |
| HUD scaler ref | 1920×1080 | 576×1024 |
| Joystick | hidden (PC) | shown |

**Assets**

- `Assets/Data/AspectProfiles/Landscape_16x9.asset`
- `Assets/Data/AspectProfiles/Portrait_9x16.asset`
- Runtime: `AspectProfileService` + `AspectVisualProfile`
- Editor: `Tools/Gameplay Prototype/R1/Aspect/*` (+ F8 toggle in sandbox Play Mode)

**Shared gameplay core:** one `PlayerCombat`, one Input System path, one `EnemySpawner`. No duplicate combat/input.

**No hard mobile resolution:** profiles never call `Screen.SetResolution`.

### Hero viewport fraction

| Profile | Ortho H (wu) | Hero / viewport | Notes |
|---|---|---|---|
| Landscape | 16.875 | **0.1185** | Live PPC 480×270 |
| Portrait | 16.000 | **0.1250** | Measured with ortho forced to 256/16 (PPC ref applied) |
| Absolute Δ | — | **+0.0065** | |
| Relative Δ | — | **+5.47%** | Within ~same silhouette scale |

Editor Game View reflection could not force `Screen` to 576×1024 during automation (stayed 1920×1080). Portrait PPC refs + HUD scaler still applied; portrait screenshots captured at 576×1024 output size with ortho forced for framing QA.

---

## 3. Sandbox scene

**Path:** `Assets/Scenes/ReferenceCombatSandbox.unity`  
**Builder:** `Tools/Gameplay Prototype/R1/Build ReferenceCombatSandbox Scene`

Contents:

- Rebuild grass + dirt path tilemap (`SandboxForest`)
- 2 forest trees (Trunk/Canopy via `TreeCanopyOcclusion`) + shadows
- White-hair hero (`Player` from Phase4 baseline)
- 5 Goblins + 1 Gobrider (runtime spawn)
- `ReferenceSandboxController` — LevelFlow / GameOver / LevelMapController **disabled**
- Does **not** write `GameProgress`

---

## 4. Assets imported (`Assets/Art/Rebuild`)

### Ground tiles
`GrassSoft`, `GrassSoft_V1`, `GrassSoft_V2`, `GrassDark`, `Dirt`, `DirtM0`–`DirtM15`, `DirtIC_*` (+ Tile assets)

### Trees
`Tree_Forest_A`, `Tree_Forest_B`, `Tree_Shadow_A`, `Tree_Shadow_B`

### Sword slash VFX
- Canonical: `Assets/Art/Rebuild/VFX/VFX_SwordSlash_{A,B,C}.png` (sliced sheets)
- Runtime load: `Assets/Resources/VFX/VFX_SwordSlash_{A,B,C}.png` (for `CombatVfx.SpawnSwordSlashFrames`)

### Import settings
Point filter · PPU 16 · no mipmaps · uncompressed · alpha transparency

Phase4 paths under `Assets/Art/Tiles` and `Assets/Art/GoblinCampPhase4` were **not** overwritten.

---

## 5. Camera / Pixel Perfect config

| Setting | Landscape | Portrait |
|---|---|---|
| `PixelPerfectCamera.assetsPPU` | 16 | 16 |
| `refResolutionX/Y` | 480×270 | 144×256 |
| `cropFrame` | None (scene default) | None |
| `CameraFollow` offset | (0, -0.45, -10) | (0, -0.2, -10) |
| Keep-on-screen margins | 3 / 2.2 / 4 | 1.2 / 2.0 / 3.2 |
| Sandbox bounds | ≈ (−6,-6)–(6,8) | profile default portrait bounds |

Sword sorting remains driven by `PlayerVisual` relative to hero `SpriteSorter` (+4000 ground bias).

---

## 6. Test matrix

### Combat

| Test | Result |
|---|---|
| R hold continuous slash (~10s) | PASS — `SetSwordHeld(true)`; BestHits reached **26** clearing pack |
| Combo 3-hit chain | PASS (combo counter advanced during clear) |
| Knockback / HitStop / Camera Shake / Damage Popup | PASS (existing systems; no sandbox fork) |
| Combo ×N HITS UI | PASS (SkillHotbarUI + HitComboCounter) |
| Skills 2/3/4/V/C | PASS — `TryCast` FireDash/MultiSlash/MawBite/Bone/Mace = True (Sword False only while CD) |
| Mob Labels always on | PASS — 6/6 |
| Mob HP bars | PASS |
| No double-damage same cast | PASS — unchanged `PlayerCombat` hit pipeline |
| Progress isolation | PASS — after sandbox, `HighestUnlockedLevel` still **5**; main scene `sandbox=False` |

### Visual

| Test | Result |
|---|---|
| Ground not heavy checkerboard | PARTIAL — soft noise still reads tiled at distance; V1/V2 variants used |
| Dirt transition | PARTIAL — Dirt + DirtM edge masks; not full autotile IC blend on path |
| Tree canopy 1 → 0.4 → 1 | PASS — measured canopy α **0.4** under Tree_A |
| Hero not missing / above ground | PASS — SpriteSorter +4000 |
| Sword / VFX match hero | PASS — frame slash + sword sort |
| Labels follow knockback | PASS (existing EnemyWorldUI) |
| UI overflow L/P | PASS — no overflow in captures; portrait HUD scaler 576×1024 |

### Screenshots

Under `Docs/ReferenceRebuild/R1/Screenshots/`:

| File | Context |
|---|---|
| `R1_land_hero_forest.png` | Hero in rebuild forest |
| `R1_land_combat_goblins.png` | Combat vs goblin pack |
| `R1_land_under_canopy.png` | Under canopy (α 0.4) |
| `R1_land_hud.png` | Landscape HUD |
| `R1_port_hero_forest.png` | Portrait framing |
| `R1_port_hud.png` | Portrait HUD |
| `R1_port_slash_r.png` | Slash R |

Copies also under `Assets/Docs/ReferenceRebuild/R1/Screenshots/` (Unity capture default).

### Video 20–30s / profile

| Item | Result |
|---|---|
| Landscape clip | NOT TESTED (no recorder in this pass) |
| Portrait clip | NOT TESTED |

### Regression (main scene)

| Test | Result |
|---|---|
| Level 1 start | PASS — `Playing` level 1 |
| Level 2 + ExitGate present | PASS — gate found; exit locked until objective |
| Level 3 start | PASS |
| Level 4 GoblinOutpost | PASS — layout `GoblinOutpost` |
| Victory / Game Over / Retry | NOT TESTED (would need full clear / death) |
| Save / Load (`GameProgress`) | PASS — unlocks intact after sandbox |
| Console (live) | PASS — 0 errors / 0 warnings after compile settle |

---

## 7. Gaps / follow-ups (need approval)

1. Soften Rebuild grass noise further if checkerboard still too visible vs reference.
2. Full dirt autotile + IC overlays on path edges.
3. Fix Editor Game View size helper so Portrait 576×1024 drives live `Screen` / PPC ortho without forcing ortho.
4. Optional 20–30s gameplay recordings per profile.
5. **Do not** roll `Art/Rebuild` onto Level 1–4 until explicit approval.
6. Phase 12E / Level 5 remains paused.

---

## 8. How to re-test

1. `Tools/Gameplay Prototype/R1/Aspect/Apply Landscape 16:9 (1920x1080)` or Portrait menu.
2. Open `Assets/Scenes/ReferenceCombatSandbox.unity` → Play.
3. Combat: hold **R**; skills **2/3/4/V/C**; **F8** toggles aspect.
4. Confirm LevelFlow stays off and PlayerPrefs unlocks unchanged.
5. Re-open `GameplayPrototype_Phase4Test` for campaign play.

---

**R1 Sandbox complete. Awaiting approval before any Level 1–4 art rollout.**
