# R3E.5A — Knight campaign rollout preparation

Designed Kit is not enabled on Level 1–4. No commit. `ClassKit_Aren.asset` is still Flame Burst / Dash Slash / Whirlwind / Guard / Crescent Strike.

## 1. Build entry

Unity 6 has no Build Profile assets. The scene list is `ProjectSettings/EditorBuildSettings.asset`.

| Order | Scene | Enabled |
| --- | --- | --- |
| 0 | `Assets/Scenes/GameplayPrototype_Phase4Test.unity` | yes, startup |
| 1 | `Assets/Scenes/ReferenceCombatSandbox.unity` | yes, dev/test |
| 2 | `Assets/Scenes/SampleScene.unity` | no, file kept |
| 3 | `Assets/Scenes/GameplayPrototype.unity` | no, file kept |

Play from that first scene loaded `GameplayPrototype_Phase4Test`. `SceneManager.GetSceneByBuildIndex(0)` is the same scene. Main menu, one Aren, `PreferDesignedSkills` false.

PASS.

## 2. Designed Kit asset

`Assets/Art/Rebuild/R2C/ClassKits/ClassKit_Aren_Designed.asset`

Created by `FillDesignedAren()`. Not a copy over the legacy asset.

| Key | Name | Numbers |
| --- | --- | --- |
| R | Kiem Hiep Si | 18, CD 0.35, 3 steps |
| 2 | Shield Bash | 30, CD 5, radius 1.4, speed 11, duration 0.18, cone 70, stun 0.32 |
| 3 | Charge | 26, CD 4, speed 14, duration 0.30, radius 0.8, dash 4 |
| 4 | Whirl Ring | 20 × 3, interval 0.11, CD 7, radius 2.2 |
| V | Bulwark | 1.4s, ×0.35, CD 8 |
| C | Dawn | 42, CD 10, anticipation 0.12, radius 2.1 |

`bodyController` is `Aren.controller`. `equipment` is `EquipmentSet_Aren_BackGear`. Skill bodies are the states already in that controller. VFX stays in `ArenKitVfx` and turns on only when the active slot 2 is Shield Bash. Hotbar icons stay on `ClassCombat.uiIcons` (`ClassUiIcons_Sandbox`). The kit ScriptableObject has no icon field, so none was added.

PASS. Legacy asset was not rewritten.

## 3. Input / combat owner

`PlayerCombat` is still the only skill executor. `ClassCombat` only selects a kit and sets `PreferDesignedSkills`. It no longer calls `FillDesignedAren()` , so selecting Aren does not mutate `ClassKit_Aren`.

Sandbox has one `ClassCombat` on the Player instance. Its slot 0 now points at `ClassKit_Aren_Designed`. Measured: one component, Shield Bash, Charge 26 / 0.3s, designed true.

`Player_Aren.prefab` has no `ClassCombat`. Campaign has none. Adding it to the prefab would duplicate the sandbox component, so it was not added.

PASS.

## 4. R held

Held for 15.39s of unscaled time. 44 hits, 792 damage, exactly 18 each. Rate matches the 0.35s cooldown. The Goblin was kinematic so knockback could not carry it out of range.

PASS.

## 5. Charge collision

Charged right into `Tree_A` (non-trigger). Start x −3.73, wall face −3.28, stop x −3.56. The body collider met the trunk. It did not cross to the far side (−1.92). Override ended, velocity 0, sprite returned to idle.

PASS.

## 6. Gobrider stun

Shield Bash dealt 30. `StatusStun._remaining` was 0.20. AI disabled during the stun, then the component removed itself and AI was enabled again. Not infinite.

PASS.

## 7. Death / Retry

Killed during each designed skill. Later damage did not land.

| Skill | Result |
| --- | --- |
| Shield Bash | Impact coroutine did 0 after death. HP stayed 500. |
| Charge | The opening hit was 26. No second hit. |
| Whirl Ring | First wave was 20. Later waves did not fire. |
| Bulwark | Guard ended. Damage multiplier returned to 1. |
| Dawn | The delayed hit did not fire. HP stayed 500. |

`ResetCombatState` stops coroutines, clears the dash override, zeroes cooldowns, and ends guard. After revive: idle, death flag false, HP 400, one `Died` listener before and after, one player, one `ClassCombat`.

Campaign Retry after Game Over: `Aren_Idle_Up_0`, flag false, HP 400, designed still false.

PASS.

## 8. Mob label edges

`ClampLabel` uses the text preferred width, the HP bar, the safe area, and the canvas scale. Alpha stays 1. Name and level are not hidden.

| Aspect | Edges | Text |
| --- | --- | --- |
| Portrait 576×1024, PPC 144×256, PPU 16 | 4/4 inside | `Goblin Lv.3` |
| Landscape 1920×1080, PPC 480×270, PPU 16 | 4/4 inside | `Goblin Lv.3` |

Campaign label read `Goblin Lv.3` at alpha 1. One `WorldLabelsCanvas`.

PASS.

## 9. Performance

Editor play, attack held, six mobs, hero kept alive for the sample.

| Measure | Value |
| --- | --- |
| Window | 37.70s |
| Frames | 21953 |
| Average | 582 FPS |
| CPU frame sample | 3.39 ms |
| GPU frame sample | 0.23 ms |
| Mono used | 1019 MB → 1044 MB |
| Mono heap | 1372 MB, unchanged |
| Active VFX names | 2 |
| Damage popups | 0 (pool empty) |
| Living mobs | 4 |
| Players | 1 |
| Console errors | 0 |

This is the editor game loop, not a standalone player. The number is measured.

PASS as a measurement. Not a player-build FPS claim.

## 10. Level 1–4 regression

Startup scene is the campaign. `PreferDesignedSkills` stayed false. No `ClassCombat`. One player. Aren controller.

| Level | Result |
| --- | --- |
| 1 | Playing, 4 waves, 3 enemies |
| 2 | Playing, 4 waves, 6 enemies |
| 3 | Playing, 4 waves, 9 enemies |
| 4 | Playing, 4 waves, 13 enemies |

Legacy Flame Burst hit for 30 and did not stun. Level 4 waves cleared by killing enemies, the exit opened, Aren entered it, state became Victory. Game Over and Retry passed. Save stayed unlock 5, L1 1174, L4 953, L5 781.

12 tree canopies on that level. 0 new console errors.

PASS.

## 11. Git status in this task

Branch `reference_rebuild_r2c`. Not committed.

This step:

- `ProjectSettings/EditorBuildSettings.asset` — scene order above
- `Assets/Art/Rebuild/R2C/ClassKits/ClassKit_Aren_Designed.asset` — new
- `Assets/Editor/R3E5ADesignedKitSetup.cs` — creates the asset and can reapply the build entry
- `Assets/Scripts/Combat/ClassCombat.cs` — does not rewrite the legacy kit
- `Assets/Scripts/Combat/PlayerCombat.cs` — death and retry cancel pending hits
- `Assets/Scripts/Enemy/EnemyWorldUI.cs` — edge clamp
- `Assets/Scenes/ReferenceCombatSandbox.unity` — slot 0 guid only, plus the earlier Vex/icon lines

`ClassKit_Aren.asset` diff is still the one BackGear equipment line from R3E.1. `GameplayPrototype_Phase4Test.unity` is unchanged. Backup: `IncomingAssets/backup_before_R3E5A_Prep/`.

## 12. Risks

- A player build now starts on the campaign scene. `SampleScene` and `GameplayPrototype` are still on disk and disabled.
- Sandbox F1 is the designed asset. Pointing that slot back at `ClassKit_Aren` returns legacy skills, because select no longer calls `FillDesignedAren()`.
- Campaign Dash Slash on the prefab is still 28 damage over 0.16s. The designed Charge is 26 over 0.30s. Turning the kit on changes that skill. Do not retune either set.
- Editor FPS is not a standalone build measurement.
- Portrait labels can sit over the hotbar when a mob is at the bottom edge. The full name stays visible.

## 13. What is left to turn the kit on

Do this only after acceptance. Do not start it in this step.

1. On the campaign Player instance only, add one `ClassCombat`.
2. `kits[0]` = `ClassKit_Aren_Designed`. Leave slots 1–3 empty. Slot 4 can stay empty on the campaign.
3. `uiIcons` = `ClassUiIcons_Sandbox`. `startSlot` = 0.
4. Do not add `ClassCombat` to `Player_Aren.prefab`.
5. Do not delete `ClassKit_Aren`. Rollback is removing that one campaign component, or pointing it at `ClassKit_Aren`. Saves do not store skill ids.
6. Accept the skill 2 and skill 3 behaviour change listed above. No other balance edit.

## PASS / FAIL / NOT TESTED

| Item | Result |
| --- | --- |
| Build entry and startup scene | PASS |
| Designed asset, legacy asset untouched | PASS |
| Single combat owner | PASS |
| R held ≥ 10s | PASS |
| Charge vs tree | PASS |
| Gobrider stun 0.2s and recovery | PASS |
| Death during five skills, Retry | PASS |
| Label edges, both aspects | PASS |
| 30s+ combat FPS | PASS, editor measurement 582 average |
| Level 1–4, Victory, Game Over, Retry, save | PASS |
| Designed Kit enabled on Campaign | not done |

No FAIL.
