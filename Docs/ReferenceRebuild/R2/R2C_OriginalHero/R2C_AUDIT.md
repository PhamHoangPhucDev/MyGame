# R2C_AUDIT — OriginalHero runtime

**Date:** 2026-10-20 · **Branch:** `reference_rebuild_r2c` · Phase 12E paused

## 1. R2C inventory read (real paths)

| Area | Path | Notes |
|---|---|---|
| Roster / approval | `roster/R2C_ROSTER.md` | **Hero chính: Aren** (Brom/Lyra/Selene/Vex = class kit sau) |
| Unity export (Aren) | `unity_export/Aren_Sheet.png`, `Extra/*`, `Gear/*`, `Aren_Anchors.csv` | 128×288 body sheet + Hurt/Death + sword/shield/slash |
| Import runtime | `Assets/Art/Rebuild/R2C/Aren/**` | PPU 16, Point, no mip, uncompressed |
| Controller | `Assets/Art/Rebuild/R2C/Aren/Animators/Aren.controller` | GUID `e3a01f83…` |
| Equipment | `EquipmentSet_Aren.asset`, `EquipmentVisual.cs` | Anchor table + 8-dir sword |
| Prefab (new) | `Assets/Art/Rebuild/R2C/Aren/Prefabs/Player_Aren.prefab` | Shared visual snapshot |
| Docs/tests | `unity_test/R2C_AREN_SANDBOX_REPORT.md`, `unity_test/combat/R2C_AREN_COMBAT_REPORT.md` | Prior Sandbox PASS evidence |
| Vex / classkit | `vex/`, `classkit/`, `unity_export_vex/` | Optional F5 class — not the default Hero |

**Approved runtime Hero identified:** **Aren** from R2C_OriginalHero → `Assets/Art/Rebuild/R2C/Aren`.

## 2. Runtime Hero now

| Scene | bodyController | Sheet | EquipmentVisual |
|---|---|---|---|
| `ReferenceCombatSandbox` | **Aren.controller** | `Aren_Sheet.png` | Yes |
| `GameplayPrototype_Phase4Test` (L1–4) | **Aren.controller** | `Aren_Sheet.png` | Yes + `EquipmentSet_Aren` |

Play evidence (L1): `ctrl=Aren`, `sprite=Aren_Idle_Side_0`, `sheet=Assets/Art/Rebuild/R2C/Aren/Aren_Sheet.png`, `replaces` legacy R slash = True, sorting order > 4000.

## 3. Legacy Phase4 Hero references remaining

| Item | Status |
|---|---|
| `Assets/Art/GoblinCampPhase4/Hero_Sheet.png` + `Hero.controller` | **Still on disk** (archive). Hash Phase4 sheet `6E5A1F24…`. **Not** assigned on Sandbox or Phase4Test Player |
| Backup scenes under `Assets/Scenes/*_backup_*` | Still point at old `Hero.controller` GUID `de44e4b9…` |
| `Assets/Art/Rebuild/R2/Hero` (R2b / Hero_R2) | Archive / prior pipeline — **not** active on Players |
| Code fallback to Phase4 frames | **None** for body — missing R2C frames would break rather than substitute Phase4 |

## 4. Files changed this pass

- `Assets/Editor/R2CArenSandboxSetup.cs` — menu **Apply Aren Shared Hero (Sandbox + L1-4 scene)**
- `Assets/Scenes/ReferenceCombatSandbox.unity` — re-wired Aren
- `Assets/Scenes/GameplayPrototype_Phase4Test.unity` — `PlayerVisual.bodyController` → Aren; added `EquipmentVisual`
- `Assets/Art/Rebuild/R2C/Aren/Prefabs/Player_Aren.prefab` — created

Skills loaded: `egead-project-core`, `egead-r2c-original-hero`, `egead-unity-mcp-workflow` only.

## 5. Tests

| Test | Result |
|---|---|
| Aren displays (Sandbox) | **PASS** |
| Aren displays (L1) | **PASS** |
| Idle / Walk | **PASS** (`Aren_Idle_*`, `Aren_Walk_Side`) |
| R hold / Attack clip | **PASS** (Sandbox `Aren_Attack_Side`; combat reports prior) |
| R 3-hit VFX via EquipmentVisual | **PASS** (prior combat report; `ReplacesLegacySlash`) |
| Sword sorting / no orphan Phase4 sword | **PASS** (legacy Sword hidden while Equip on) |
| Hero above ground (SpriteSorter) | **PASS** (order 4000+) |
| Skills 2/3/4/V/C TryCast | **PASS** (Sandbox) |
| Mob Labels count | **PASS** (Sandbox 6) |
| Console errors | **PASS** (0) |
| Save unlocks intact | **PASS** (`HighestUnlockedLevel=5`) |
| Dual-aspect 9:16 / 16:9 on **Sandbox** | **PASS** (prior R1.1 / Aren sandbox reports) |
| Dual-aspect on **L1–4** (no AspectProfileService on campaign scene) | **NOT TESTED** |
| Prefab instance replace of all Players | **NOT TESTED** — scenes keep Player instances; share Aren assets/prefab snapshot |
| Delete Phase4 Hero assets | **NOT DONE** (needs your OK) |

## 6. Decisions needed

1. **Delete or quarantine** `GoblinCampPhase4/Hero_*` and `Rebuild/R2/Hero` from runtime search paths, or keep as archive only?
2. Add `AspectProfileService` to the campaign scene so L1–4 get the same 9:16 / 16:9 switch as Sandbox?
3. Promote `Player_Aren.prefab` to the sole spawn source (replace scene Players) vs keep scene instances wired to Aren assets?
4. Vex / other classes stay Sandbox F-keys only until approved?

**STOP.**
