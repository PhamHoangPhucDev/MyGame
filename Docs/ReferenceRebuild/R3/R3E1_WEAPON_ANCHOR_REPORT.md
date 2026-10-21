# R3E.1 — Aren & Vex weapon slice + back anchors

**Date:** 2026-10-21 · **Branch:** `reference_rebuild_r2c` · **HEAD at start:** `3d0e022` (after `590ba45`; later commits are monster art only)  
**Sandbox only.** Campaign `EquipmentSet_Aren` and `Aren_Anchors.csv` were not switched. No commit.

## 1. Git status at start

Branch `reference_rebuild_r2c`. Tracked tree clean. Untracked and left untouched: `.claude/`, `.cursor/`, `Assets/_Recovery/`, `IncomingAssets/backup_before_R3D_ReferenceMatch/`, `IncomingAssets/backup_before_R3D1_ArenVfx/`.

Backup of files this task could replace: `IncomingAssets/backup_before_R3E1_WeaponAnchors/`.

## 2. Source files

| Use | Path |
|---|---|
| Index | `Docs/ReferenceRebuild/R2/R2C_OriginalHero/HANDOFF_INDEX.md` |
| Aren back gear | `roster/BACK_GEAR_NOTE.md` |
| Vex back daggers | `vex/VEX_BACK_DAGGERS_NOTE.md` |
| Sword 432×48 | `unity_export/Gear/Aren_Sword.png` |
| Aren anchors | `unity_export/Aren_Anchors.csv` |
| Dagger 480×48 | `unity_export_vex/Gear/Vex_Dagger.png` |
| Vex anchors | `unity_export_vex/Vex_Anchors.csv` |

Pixels in the first 384×48 of both new sheets match the sheets already in `Assets/`. Cells 0–7 were not redrawn.

## 3. Import

PPU 16, Filter Point, no mipmaps, default compression None, alpha preserved. Existing 8-dir sprite IDs kept. New cells only:

| Sprite | Rect | Pivot |
|---|---|---|
| `Aren_Sword_BackHilt` | x=384, 48×48 | (0.5, 0.5) |
| `Vex_Dagger_PeekL` | x=384, 48×48 | (0.5, 0.5) |
| `Vex_Dagger_PeekR` | x=432, 48×48 | (0.5, 0.5) |

Menu: `Game01/R3E1/Apply Back Gear Slices`. Log: `sword=9 dagger=10`.

## 4. Slice counts

- Aren sword: 9 sprites, none null. Index 8 = `Aren_Sword_BackHilt`.
- Vex dagger: 10 sprites, none null. Index 8 = `Vex_Dagger_PeekL`, 9 = `Vex_Dagger_PeekR`. `weaponPoses` and `offPoses` are the same 10.

## 5. Anchor mapping

Idle / Walk / Hurt use the new rows. Attack rows in the source CSV are unchanged and still use dirs 0–7. `EquipmentVisual` was not modified. `Pick()` already rejects an index past the array, and the combo `Wrap8` only runs while a combo step is active.

| State | Aren main | Aren order | Vex main / off |
|---|---|---|---|
| Down rest | cell 8 BackHilt, hand hidden | −2 (behind) | PeekL −2 / PeekR −1 |
| Up rest | cell 7 (DR), shield Down in front | +1 | DR +1 / DL +2 |
| Side rest | cell 5 (DL), shield Side | −2 | DL −2 / DR −1; flipX when facing right |
| Attack Side frame 0 | `Aren_Sword_UR` in the hand anchor | −2 | `Vex_Dagger_UR` on both hands |
| After attack | returns to the rest row for the facing | | returns to the rest row |

Measured local positions match the CSV (body pivot 16,30, PPU 16). Example: Aren Idle Down (21,16) → (0.31, 0.88). Walk Down frame 0 is one pixel lower, (0.31, 0.81). One `Equip_Main`. Legacy `Sword` renderer stayed off.

Sandbox wiring: `ClassKit_Aren.equipment` → `EquipmentSet_Aren_BackGear` (new CSV). `Player_Aren` and `EquipmentSet_Aren` still use `Aren_Anchors.csv` and 8 poses, so a scene that does not run `ClassCombat` keeps the previous hand rest. `ClassKit_Aren` skill numbers were put back to the committed asset (`Flame Burst` and the rest). Sandbox play still calls `FillDesignedAren()` on F1.

F5 in `ReferenceCombatSandbox` now references the existing `ClassKit_Vex`. No new class selector.

## 6. Files changed

- `Assets/Art/Rebuild/R2C/Aren/Gear/Aren_Sword.png` (+ meta cell 8)
- `Assets/Art/Rebuild/R2C/Aren/Aren_Anchors_BackGear.csv`
- `Assets/Art/Rebuild/R2C/ClassKits/EquipmentSet_Aren_BackGear.asset`
- `Assets/Art/Rebuild/R2C/ClassKits/ClassKit_Aren.asset` (equipment reference only)
- `Assets/Art/Rebuild/R2C/Vex/Gear/Vex_Dagger.png` (+ meta cells 8–9)
- `Assets/Art/Rebuild/R2C/Vex/Vex_Anchors.csv`
- `Assets/Art/Rebuild/R2C/ClassKits/EquipmentSet_Vex.asset`
- `Assets/Editor/R3E1BackGearSetup.cs`
- `Assets/Editor/R2CArenSandboxSetup.cs`, `R2CVexSandboxSetup.cs` (slice lists include the extra cells)
- `Assets/Scenes/ReferenceCombatSandbox.unity` (F5 = existing Vex kit)
- Shots: `Docs/ReferenceRebuild/R3/R3E1_shots/`

Not changed: `EquipmentSet_Aren.asset`, `Aren_Anchors.csv`, `Player_Aren.prefab`, `GameplayPrototype.unity`, `PlayerCombat` damage, Enemy AI, labels, TreeCanopyOcclusion.

## 7. Tests

| ID | Result | Evidence |
|---|---|---|
| T1 Aren 9 cells | **PASS** | importer log + meta |
| T2 Vex 10 cells | **PASS** | importer log + meta |
| T3 no null sprites | **PASS** | live set, nulls=0 |
| T4 Aren idle back sword | **PASS** | Down BackHilt; Up DR; Side DL, flip on right |
| T5 Aren walk sticks to the body | **PASS** | Walk Down BackHilt y=0.81; Walk Side follows flip |
| T6 Aren hurt stays attached | **PASS** | Hurt Down BackHilt |
| T7 Aren attack to the hand | **PASS** | Attack Side `Aren_Sword_UR` at (0.19, 0.63) |
| T8 after attack, sword returns | **PASS** | next sample Idle Down BackHilt |
| T9 R damage, no second weapon | **PASS** | `TickSlashStrike` Goblin 60→42 (damage 18); `Equip_Main` count 1 |
| T10 Vex idle | **PASS** | Down PeekL+PeekR; Up DR/DL in front; Side behind, flip right |
| T11 Vex walk / hurt | **PASS** | Walk Down peeks y=0.56; Hurt Down peeks |
| T12 Vex attack to the hand | **PASS** | Attack Side `Vex_Dagger_UR`, then Up rest returns |
| T13 facings in the sheets | **PASS** | Down, Up, Side, Side+flipX |
| T14 sort vs body | **PASS** | rest Down −2, Up +1, Side −2; body order still follows the body |
| T15 tree occlusion | **NOT TESTED** | script not edited; canopy fade not re-walked |
| T16 portrait | **PASS** | Screen 576×1024, aspect 0.563, PPC 144×256, PPU 16, Point |
| T17 landscape | **PASS** | Screen 1920×1080, aspect 1.778, PPC 480×270, PPU 16 |
| T18 no duplicate visual | **PASS** | 1 EquipmentVisual, 1 Equip_Main, legacy sword off |
| T19 skills and labels | **PASS** for unchanged code | `PlayerCombat` / label scripts not edited. F1 still logs Shield Bash / Charge / Whirl Ring / Bulwark / Dawn. Full HUD pass **NOT TESTED** |
| T20 Level 1–4 and save | **NOT TESTED** | `GameplayPrototype.unity` and save scripts not in the diff |
| T21 console | **FAIL runtime / PASS compile** | 0 compile errors. During play, `DamagePopup.GetFromPool` threw when a mob landed a hit (`WorldLabelsCanvas.Instance` null). Stack is `EnemyController` → `DamagePopup`, not the weapon files |

## 8. Remaining

- Back gear is on the sandbox kits only. `Player_Aren` / `EquipmentSet_Aren` still use the hand-rest CSV, so opening that prefab without `ClassCombat` does not show the back sword.
- Tree fade, full Level 1–4, Victory / Game Over, and Save/Load were not replayed.
- Attack2 / Attack3 body sheets are still absent. This task did not add them.
- Shots are full sandbox frames, so the sprite is small. Anchor numbers above are the precise check.

## 9. Sandbox scope

`ReferenceCombatSandbox` F1 uses `EquipmentSet_Aren_BackGear`. F5 uses `EquipmentSet_Vex`. `PreferDesignedSkills` is still set only by `ClassCombat` in that scene. Level 1–4 scene was not edited.

## 10. Files safe to commit later (this task only)

Do not `git add` `.claude/`, `.cursor/`, `Assets/_Recovery/`, or the `IncomingAssets/backup_*` folders.

- `Assets/Art/Rebuild/R2C/Aren/Gear/Aren_Sword.png` + `.meta`
- `Assets/Art/Rebuild/R2C/Aren/Aren_Anchors_BackGear.csv` + `.meta`
- `Assets/Art/Rebuild/R2C/ClassKits/EquipmentSet_Aren_BackGear.asset` + `.meta`
- `Assets/Art/Rebuild/R2C/ClassKits/ClassKit_Aren.asset`
- `Assets/Art/Rebuild/R2C/ClassKits/EquipmentSet_Vex.asset`
- `Assets/Art/Rebuild/R2C/Vex/Gear/Vex_Dagger.png` + `.meta`
- `Assets/Art/Rebuild/R2C/Vex/Vex_Anchors.csv`
- `Assets/Editor/R3E1BackGearSetup.cs` + `.meta`
- `Assets/Editor/R2CArenSandboxSetup.cs`
- `Assets/Editor/R2CVexSandboxSetup.cs`
- `Assets/Scenes/ReferenceCombatSandbox.unity`
- `Docs/ReferenceRebuild/R3/R3E1_WEAPON_ANCHOR_REPORT.md`
- `Docs/ReferenceRebuild/R3/R3E1_shots/`

**STOP.** Waiting for acceptance. No icons, skill body frames, monsters, other classes, or campaign rollout.
