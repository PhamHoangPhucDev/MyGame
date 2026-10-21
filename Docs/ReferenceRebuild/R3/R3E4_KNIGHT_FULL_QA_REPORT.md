# R3E.4 — Aren Knight full QA

QA only. Designed Kit was not rolled onto Level 1–4. No commit. `ClassKit_Aren` on disk is still the legacy kit.

## 1. Scene Campaign thực tế

Editor Play loads whatever scene is open. A built player starts at build index 0.

| Scene | Build | Player | Aren.controller | LevelFlow |
| --- | --- | --- | --- | --- |
| `SampleScene` | enabled, index 0 | none | no | no |
| `GameplayPrototype` | enabled, index 1 | raw `Player`, not a prefab | no | no |
| `ReferenceCombatSandbox` | disabled | `Player_Aren` instance, named `Player` | yes | yes, disabled at runtime by the sandbox |
| `GameplayPrototype_Phase4Test` | not in the build | `Player_Aren` instance, named `Player` | yes | yes, this is Level 1–4 |

Level 1–4 run in `Assets/Scenes/GameplayPrototype_Phase4Test.unity`. R2C.1 and this play test agree. R3E.3 inspected `GameplayPrototype`, which is the old prototype and cannot run a wave. The two scenes plus the empty `SampleScene` at the front of the build are the source of the contradiction. Nothing was deleted or merged.

## 2. Player / Animator runtime

`Assets/Art/Rebuild/R2C/Aren/Prefabs/Player_Aren.prefab` is the runtime hero. It has `Aren.controller`, `PlayerCombat`, `PlayerVisual`, and `EquipmentVisual` on the hand-rest set `EquipmentSet_Aren`. It has no `ClassCombat`.

Sandbox adds `ClassCombat` on the instance, selects Aren, and swaps in `EquipmentSet_Aren_BackGear`. Campaign does not. One player in both scenes.

## 3. Skill mapping

`PreferDesignedSkills` is false until `ClassCombat.Select` on Aren. Campaign stayed false. Disk kit was not overwritten.

| Key | Disk `ClassKit_Aren` | Sandbox runtime | Campaign runtime |
| --- | --- | --- | --- |
| R | Kiem Hiep Si, 18, CD 0.35, 3 steps | same combo, `Hero_Attack_*`, back sword returns after the clip | same numbers from the prefab, `Hero_Attack_*`, hand sword `Aren_Sword_D` |
| 2 | Flame Burst, 30, CD 5, radius 1.7 | Shield Bash, 30, CD 5, radius 1.4, cone 70°, stun 0.32, body 0.26s | Flame Burst 30. One Goblin took 30 and was not stunned |
| 3 | Dash Slash, 28, CD 4, duration 0.16 | Charge, 26, CD 4, speed 14, duration 0.30, radius 0.8 | Dash Slash 28 from the prefab |
| 4 | Whirlwind, 20 × 3, interval 0.11, CD 7 | Whirl Ring, same numbers, `Aren_Whirl` 0.60s, no flip | Whirlwind, no skill body |
| V | Guard, 1.4s, ×0.35, CD 8 | Bulwark, same numbers, `Aren_Bulwark_{view}` | Guard, same numbers, no Bulwark body |
| C | Crescent Strike, 42, CD 10, anticipation 0.12 | Dawn, same numbers, `Aren_Dawnbreaker_{view}` 0.35s | Crescent Strike, no Dawn body |

Sandbox VFX and hotbar icons exist only while that `ClassCombat` is active. Campaign hotbar stayed on the procedural glyphs.

## 4. Sáu kỹ năng Aren (Sandbox)

| Skill | Result |
| --- | --- |
| R combo | PASS. Three hits, 54 damage (18×3). One `Equip_Main`. Attack frame `Aren_Attack_Side_0`, hand sword, then idle can return to `Aren_Sword_BackHilt`. |
| R held 10s | NOT TESTED. No key was held for 10 seconds. |
| Shield Bash | PASS. 30 damage once, Goblin `IsStunned`, cone on frame `ShieldBash_Down_1`. |
| Charge | PASS. Override `(0, -14)` for 0.3s, body `Charge_Down_0`, 26 damage once. |
| Charge vs a wall | NOT TESTED. `Physics2D.Simulate` did not run, so a zero move is not evidence. |
| Whirl Ring | PASS. 60 damage = 3×20. `Aren_Skill_Whirl_1`, `flipX` false. |
| Bulwark | PASS. 20 damage became 7. Body `Bulwark_Down_0`, one back and one front layer. End returned to `Aren_Idle_Down_2` and `Aren_Sword_BackHilt`, VFX cleared. |
| Dawn | PASS. 42 once. A second tick did 0. Impact frame `Dawnbreaker_Side_1` flips when facing right. |

Gobrider stun length was not re-timed. The duration function was not edited (Goblin 0.32s, Gobrider 0.2s).

## 5. Hurt / Death

A locked skill pose is intentional. `hurtInterruptsAttack` stays false, same rule as the sword combo. HP still drops and `HeroHurt` still plays. The body does not switch to `Hero_Hurt` until the pose ends. That rule was not changed.

Death plays `Hero_Death`, clears the skill timer, and `PlayerCombat` stops whirl and guard. A Shield Bash already in the air could still land after death. `FinishShieldBash` now returns when the hero is dead. Measured: bash while dead did 0 damage.

`ReviveFull` restored HP but left `PlayerVisual._dead` set, so Retry kept the death sprite. `PlayerVisual` now clears that lock when health is alive again. After Retry: flag false, HP 400, sword `Aren_Sword_D`, and the next body step is `Hero_Idle_Up`.

## 6. DamagePopup và Mob Label

Sandbox had one `WorldLabelsCanvas`, six labels, alpha 1, text `Goblin Lv.3` / `Gobrider Lv.5`, HP bars visible. Campaign Level 1 showed the same. Labels are not hidden.

Stun icon showed on the portrait bash shot while `IsStunned` was true. Bleed, stealth, and empowered icons were not shown this pass. The Bulwark reduction was measured; its status icon was not confirmed on screen.

Stop/Play three times and the scene change did not throw. Popup count went back to 0. Console errors: 0.

## 7. Portrait / Landscape

Portrait 576×1024, PPC 144×256, PPU 16. Landscape 1920×1080, PPC 480×270, PPU 16. Both on Sandbox and on the campaign scene.

Sandbox landscape shows dark green side bars because the sandbox ground is a narrow strip. Campaign landscape fills the frame. Zoom was not changed.

Tree canopy alpha measured 0.40 while Aren stood on `Tree_B` in the sandbox. Campaign has 22 `TreeCanopyOcclusion` objects. Body sorting stayed on the Y-sort order (campaign body order 4640).

## 8. Regression Level 1–4

Played `GameplayPrototype_Phase4Test` from the menu.

| Level | Result |
| --- | --- |
| 1 | PASS. Playing, wave 1/4, 3 enemies, 1 player, Aren controller, designed kit false, legacy flame 30. |
| 2 | PASS. Playing, 4 waves, 3 enemies, 1 player. |
| 3 | PASS. Playing, 4 waves, 6 enemies, 1 player. |
| 4 | PASS. Playing, 4 waves, 10 enemies, 1 player. |

Enemy controllers were live and spread across the map. Legacy hotbar glyphs stayed on. Back-gear anchors stayed off the campaign prefab.

## 9. Victory / Game Over / Retry / Save

Level 1 waves were cleared by defeating the spawned enemies. The flow went Wave 1 → 2 → 3 → 4 → `WaveTransition` → `Victory` through `OnWaveCleared`. `TriggerVictory()` was not called from the test.

Game Over fired on a lethal hit. Retry returned to Playing.

Save before and after: unlock 5, L1 1174/40/3/35.526, L2 832/22/3/34.086, L3 1037/30/3/50.315, L4 953/16/3/7.516, L5 781/0/3/27.387. Unchanged, so nothing was restored.

## 10. Console và performance

Console errors: 0. Warnings are the existing obsolete `OverlapBoxNonAlloc`, an unused `maxNudge` field, and the `Physics2D.Simulate` warnings from the invalid wall probe.

FPS was not measured. No FPS PASS.

Sandbox counts while a fight was up: 1 player, 6 mobs, 1 label canvas, popup pool returned to 0 after the restarts, Bulwark VFX returned to 0 on end. Stop/Play ×3 with domain reload off: one player, one canvas, designed kit still on, no errors.

A continuous 30-second FPS sample was not taken.

## 11. File đã sửa ở lượt này

- `Assets/Scripts/Player/PlayerVisual.cs` — revive clears the death lock
- `Assets/Scripts/Combat/PlayerCombat.cs` — a dead hero's Shield Bash does not land
- `Docs/ReferenceRebuild/R3/R3E4_KNIGHT_FULL_QA_REPORT.md`
- `Docs/ReferenceRebuild/R3/R3E4_shots/`

Backup: `IncomingAssets/backup_before_R3E4_QA/`. No scene save. `ClassKit_Aren` diff is still only the BackGear equipment guid from R3E.1. `GameplayPrototype_Phase4Test` is clean.

## 12. PASS / FAIL / NOT TESTED

| Item | Result |
| --- | --- |
| Campaign scene identity | PASS. Phase4Test is Level 1–4. |
| Six designed skills in Sandbox | PASS, except the 10s R hold and the wall test |
| Hurt lock | PASS as the existing uninterruptible rule |
| Death, then Retry pose | PASS after the two small fixes |
| Labels, one canvas, popup restart | PASS |
| Portrait and landscape PPC | PASS |
| Campaign L1–4, legacy kit, one player | PASS |
| Victory, Game Over, Retry, save unchanged | PASS |
| Tree canopy fade | PASS in Sandbox. Campaign objects present. |
| FPS / 30s soak | NOT TESTED |
| R held 10 seconds | NOT TESTED |
| Charge blocked by a collider | NOT TESTED |
| Gobrider stun timing | NOT TESTED |
| Bleed / stealth / empowered icons | NOT TESTED |
| Designed Kit on Campaign | not rolled out |

No FAIL remained after the death fix.

## 13. Đề xuất Campaign rollout

Do not do this until it is accepted. Do not add `ClassCombat` to the prefab: the sandbox already adds one on the instance, and a second copy would double input.

1. Put `GameplayPrototype_Phase4Test` in the build as the first enabled scene. Leave `SampleScene` and `GameplayPrototype` in the project, but out of the player build. Do not delete them.
2. Add one `ClassCombat` on the campaign player instance only. Point it at a new `ClassKit_Aren_Designed` asset, cloned from the current runtime `FillDesignedAren` values. Leave `ClassKit_Aren` as Flame Burst / Dash Slash / Whirlwind / Guard / Crescent Strike.
3. On that new asset, reference `EquipmentSet_Aren_BackGear`, the existing `Aren.controller`, and `ClassUiIcons_Sandbox`. `Select` already sets `PreferDesignedSkills`. No second body animator.
4. Accept the skill-3 difference before flipping it on. Disk and the prefab dash are 28 damage over 0.16s. Designed Charge is 26 damage over 0.30s. Skill 2 changes from a flame circle to a cone stun. Do not retune either set in the rollout.
5. Saves store unlock and best scores, not skill ids. The tested keys do not need a migration. Do not change LevelFlow.

## 14. Rủi ro còn lại

- A build started today opens `SampleScene`, not the campaign.
- `GameplayPrototype` still looks like the game and has no waves.
- Portrait labels clip at the left screen edge (`blin Lv.3`). The text alpha is still 1.
- Sandbox 16:9 has empty green bars beside the narrow ground.
- Skill poses do not play hurt. Death now wins. Other in-flight coroutines besides Shield Bash were not individually sealed.
- Campaign still shows the hand-rest sword until the rollout above.

Shots: `Docs/ReferenceRebuild/R3/R3E4_shots/` — `portrait_combat`, `portrait_bash`, `portrait_charge`, `portrait_whirl`, `portrait_bulwark`, `portrait_dawn`, `portrait_hurt`, `portrait_death`, `campaign_level1`, `landscape_hud`, `campaign_landscape`.
