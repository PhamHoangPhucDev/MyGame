# R3E.6 — Vex skill body animation (sandbox only)

Vex skill bodies play in the combat sandbox. Aren campaign, damage, cooldowns, and hit timing were not retuned. No commit. Brom, Lyra, and Selene were not touched.

## 1. Source art read

- `Docs/ReferenceRebuild/R2/R2C_OriginalHero/HANDOFF_INDEX.md`
- `Docs/ReferenceRebuild/R2/R2C_OriginalHero/vex/VEX_SKILL_BODY_NOTE.md`
- `Docs/ReferenceRebuild/R2/R2C_OriginalHero/unity_export_vex/Skills/Vex_SkillSheet.png` (128×416, 13 rows × 32)
- `Docs/ReferenceRebuild/R2/R2C_OriginalHero/unity_export_vex/Skills/Vex_SkillAnchors.csv` (25 rows, `hand_x = -99`)
- `Docs/ReferenceRebuild/R2/R2C_OriginalHero/unity_export_vex/Skills/VEX_SKILL_LAYOUT.csv`
- Prior reports: `R3E1_WEAPON_ANCHOR_REPORT.md`, `R3E3_AREN_SKILL_ANIMATION_REPORT.md`, `R3E5C_ENEMY_HP_VISIBILITY_REPORT.md`

The 10-cell dagger sheet and idle/walk/attack back anchors were already in `Assets/Art/Rebuild/R2C/Vex/`. They were not re-sliced. `Vex_Sheet.png` and the Docs copy of `Vex_Anchors.csv` were not overwritten.

## 2. Sprites imported

`Assets/Art/Rebuild/R2C/Vex/Skills/Vex_SkillSheet.png`

25 sprites, 32×32, pivot `(0.5, 0.0625)`, PPU 16, Point filter, mipmaps off, uncompressed, alpha is transparency.

| Rows | Sprites |
| --- | --- |
| Shadow Step Down / Up / Side | 2 frames each |
| Knife Throw Down / Up / Side | 2 frames each |
| Smoke Bomb Down / Up / Side | 1 frame each |
| Execute Down / Up / Side | 2 frames each |
| Blade Dance | 4 frames, no facing |

Names are `Vex_Skill_{Skill}_{View}_{frame}` and `Vex_Skill_BladeDance_0..3`.

## 3. Animation clips

New clips under `Assets/Art/Rebuild/R2C/Vex/Animations/`. States were added to the existing `Vex.controller`. Existing `Hero_*` states were left in place.

| Clip | Timing |
| --- | --- |
| `Vex_ShadowStep_{Down,Up,Side}` | 0, 0.08, end 0.23 |
| `Vex_KnifeThrow_{Down,Up,Side}` | 0, 0.10, end 0.22 |
| `Vex_SmokeBomb_{Down,Up,Side}` | hold 0.15 |
| `Vex_Execute_{Down,Up,Side}` | 0, 0.08, end 0.20 |
| `Vex_BladeDance` | 0, 0.05, 0.10, 0.15, end 0.20, loop |

`PlaySkillPose` now looks up `{ControllerName}_{skill}_{view}`. On Aren that is still `Aren_Whirl`. On Vex it is `Vex_ShadowStep_Down` and the other new states. A missing state returns false and the old swing remains.

The blink, knife spawn, execute hit, and stealth duration stay on the existing frame. The body clip plays around that instant. No delay was added.

## 4. Vex F5 mapping

Sandbox `ClassCombat` slot 4. `ArenKitVfx.PreferDesignedSkills` is false on Vex, so Aren's Whirl state is not used.

| Input | Skill | Body |
| --- | --- | --- |
| R | Dagger combo | Existing `Vex_Attack_*`. Daggers move to the hands, then back to the peek anchors. |
| 2 | Shadow Step | `Vex_ShadowStep_*` after the blink. |
| 3 | Knife Throw | `Vex_KnifeThrow_*`. Old slash only if the state is missing. |
| 4 | Blade Dance | `Vex_BladeDance` for 0.60s, no flip. Aren still uses `Aren_Whirl` while the designed flag is on. |
| V | Smoke Bomb | `Vex_SmokeBomb_*`, then the existing stealth timer. |
| C | Execute | `Vex_Execute_*`. Old slash only if the state is missing. |

Skill anchor rows were appended to `Assets/Art/Rebuild/R2C/Vex/Vex_Anchors.csv` (25 rows, first key `Skill_ShadowStep_Down_0`). Idle peek rows were kept. Each sampled skill showed two dagger renderers.

## 5. Missing frames

None for Shadow Step, Knife Throw, Blade Dance, Smoke Bomb, or Execute. Nothing was marked NOT IMPLEMENTED. No new VFX were drawn. Aren sheets and clips were not edited.

## 6. Combat test

Scene: `Assets/Scenes/ReferenceCombatSandbox.unity`, F5 Vex.

| Check | Result |
| --- | --- |
| Six hotbar slots, Vex icons | PASS. Slots 2, 3, 4, V, R visible with the Vex icons. |
| Idle | PASS. `Vex_Idle_Down_0`, daggers `Vex_Dagger_PeekL` + `Vex_Dagger_PeekR`. |
| Attack | PASS. `Vex_Attack_Down_0`, then idle with the back daggers. |
| Shadow Step | PASS. `TryCast` true, `Vex_Skill_ShadowStep_Down_0` / `Vex_Skill_ShadowStep_Up_0`. |
| Knife Throw | PASS. `Vex_Skill_KnifeThrow_Down_0`. |
| Blade Dance | PASS. `Vex_Skill_BladeDance_0`, `flipX` false. |
| Smoke Bomb | PASS. `Vex_Skill_SmokeBomb_Down_0`. Body alpha 0.35 after the stealth tick. |
| Execute | PASS. `Vex_Skill_Execute_Down_0`. Target HP dropped. |
| Hurt during a skill | PASS. 10 damage, sprite stayed `Vex_Skill_KnifeThrow_Down_0`. |
| Death / revive | PASS. `Vex_Death_Down_0`, two daggers, then `Vex_Idle_Down_0` at 320 HP. |
| F5 → F1 → F5 | PASS. Aren controller and Shield Bash, then Vex controller, two daggers, slot 2 Buoc Bong. Repeated twice. |
| One mob HP bar | PASS. Bar off, then on for 3.00s after a real hit. Name stayed on. |
| Six mobs | PASS. Five runtime copies of the existing Goblin. 6 labels, 1 bar on the damaged mob, 0 bars on the others. Copies were destroyed. |
| Status icons | PASS. Stun and bleed were on while that bar was off and the name stayed on. |
| Stop / Play ×3 | PASS. Each entry: `Select(4)`, controller `Vex`, skill states present, idle sprite `Vex_Idle_*`. |
| Console | PASS. 0 errors, 0 warnings at the end of the play tests. |

Shots: `Docs/ReferenceRebuild/R3/R3E6_shots/` — idle, attack, shadow, knife, dance, smoke, execute, landscape. The smoke shot was taken with alpha forced back to 1 so the pose is readable. The 0.35 stealth alpha was measured separately.

## 7. Portrait / landscape

| Aspect | Result |
| --- | --- |
| Portrait | PASS. Screen 576×1024, PPC 144×256, PPU 16. |
| Landscape | PASS. Screen 1920×1080, PPC 480×270, PPU 16. |

Portrait was restored before leaving the sandbox. The scene was not saved.

## 8. Campaign regression

`Assets/Scenes/GameplayPrototype_Phase4Test.unity`, Level 1 only. Level 5 was not started. `GameProgress` was not reset. The scene was not saved.

| Check | Result |
| --- | --- |
| One player, Aren body | PASS. `Aren_Idle_Up_3`, controller `Aren`. |
| Designed kit | PASS. `ClassKit_Aren_Designed`, slot 2 `ShieldBash`, `PreferDesignedSkills` true. |
| Vex not in the campaign | PASS. 0 sprites named `Vex_*`. |
| Fresh HP bars | PASS. 3 foes, 3 names, 0 bars. After 5 damage, 1 bar. |
| Portrait PPC | PASS after `ApplyDetected`: 576×1024, PPC 144×256, PPU 16. The camera read 480×270 for one sample before that call. |

L2–L4 were not replayed in this pass. Boss Warlord was not opened.

## 9. Git diff

No commit, no push, no reset, no clean.

Backup before the edit: `IncomingAssets/backup_before_R3E6_VexSkillBody/` (`Vex_Anchors.csv`, `Vex.controller`, `PlayerVisual.cs`, `PlayerCombat.cs`).

This task added:

- `Assets/Editor/R3E6VexSkillBodySetup.cs`
- `Assets/Art/Rebuild/R2C/Vex/Skills/`
- 13 new `Vex_*.anim` skill clips and their `.meta` files
- 13 states on `Vex.controller` (working tree `+377` lines; the controller was clean before this task)
- 25 appended rows in `Vex_Anchors.csv` (file was already dirty from R3E.1)
- Additive edits in `PlayerVisual.cs` and `PlayerCombat.cs` (both were already dirty from earlier R3 work)

`git diff --stat` on those four tracked files: 576 insertions, 41 deletions. That stat includes the earlier uncommitted R3 edits in the two scripts and the anchor file. Aren clips, `ClassKit_Aren_Designed`, `EnemyWorldUI`, Level 1–4, and `GameProgress` were not edited for this task.

Earlier uncommitted R3 reports, shots, and the leftover `Assets/Docs/R3E5B_whirl.png` and `Assets/Docs/R3E5C_before_hp_always.png` are still untracked. The temporary R3E.6 captures under `Assets/Docs/` were deleted after the copies were saved.

## 10. Result

| Area | Result |
| --- | --- |
| Vex sandbox skill bodies | PASS |
| Weapon anchors, two daggers | PASS |
| Portrait and landscape | PASS |
| Campaign Level 1 smoke | PASS |
| L2–L4 replay this pass | NOT TESTED |
| Boss HP bar | NOT TESTED |
| Brom, Lyra, Selene | NOT TESTED |

Stopped for review.
