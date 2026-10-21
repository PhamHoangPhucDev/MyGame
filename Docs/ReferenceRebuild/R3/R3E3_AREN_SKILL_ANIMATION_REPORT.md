# R3E.3 — Aren skill body animation

Sandbox designed kit only. No commit. Idle / Walk / Hurt / Attack clips were not replaced.

## 1. Imported art

`Docs/ReferenceRebuild/R2/R2C_OriginalHero/unity_export/Skills/Aren_SkillSheet.png` (128×416, 13 rows × 32px) → `Assets/Art/Rebuild/R2C/Aren/Skills/Aren_SkillSheet.png`.

28 sprites, names `Aren_` + layout key. Pivot (0.5, 0.0625), PPU 16, Point, no mipmaps, uncompressed, alpha kept. Row 0 sits at texture y 384.

| Skill | Frames | Views |
| --- | --- | --- |
| Shield Bash | 2 | Down, Up, Side |
| Charge | 2, loop | Down, Up, Side |
| Bulwark | 1 | Down, Up, Side |
| Dawnbreaker | 3 | Down, Up, Side |
| Whirl | 4, one clip for every facing | no per-direction sheet |

28 anchor rows from `Aren_SkillAnchors.csv` were appended to `Aren_Anchors_BackGear.csv` only. `Aren_Anchors.csv` was not touched.

## 2. Clips

Under `Assets/Art/Rebuild/R2C/Aren/Animations/`:

- `Aren_ShieldBash_{Down,Up,Side}` — frame 0 for 0.08s, frame 1 held to 0.26s
- `Aren_Charge_{Down,Up,Side}` — 0.075s per frame, loops
- `Aren_Bulwark_{Down,Up,Side}` — one frame, held while guarding
- `Aren_Dawnbreaker_{Down,Up,Side}` — frame 0 to 0.12s, frame 1 for 0.08s, frame 2 to 0.35s
- `Aren_Whirl` — 0.05s × 4, loops

Dawn frame 1 starts at 0.12s so it matches the live crescent hit. The body note said 0.15s. Damage timing was left at 0.12s.

## 3. Animator

`Aren.controller` gained 13 states on the existing base layer. No transitions were added. The 13 `Hero_*` states and their clips are unchanged. `PlayerVisual` calls `Animator.Play` only while a designed-kit pose is active. Hurt still does not cancel a locked pose (same rule as the sword combo). Death still plays `Hero_Death` and blocks later poses.

## 4. Skill mapping

`ClassKit_Aren` on disk is still Flame Burst / Dash Slash / Whirlwind / Guard / Crescent Strike. Sandbox `ClassCombat.Select(0)` still calls `FillDesignedAren()` in memory. `PreferDesignedSkills` is set only on that path. Body poses are requested only when `ArenKitVfx.Active` is true, and only if the state exists.

| Input | Runtime | Body |
| --- | --- | --- |
| 2 | Shield Bash | `Aren_ShieldBash_{view}` for 0.26s |
| 3 | Charge | `Aren_Charge_{view}` for the existing 0.30s dash |
| 4 | Whirl Ring | `Aren_Whirl` for 0.60s (3 loops). No flip |
| V | Bulwark | `Aren_Bulwark_{view}` while `IsGuarding` |
| C | Dawn | `Aren_Dawnbreaker_{view}` for 0.35s |
| R | existing 3-step combo | `Hero_Attack_*` unchanged |

## 5. VFX and gear

Shield Bash cone VFX now plays in `FinishShieldBash` with the existing single damage call, while frame 1 is up. Dust stays at the start of the lunge. Stun still uses `StatusStun.DurationFor` (Goblin 0.32s, Gobrider 0.2s). One Goblin went 60 → 30 HP and `IsStunned` became true.

Charge override measured `(0, -14)` for 0.3s. Whirl and Bulwark each spawned one attached layer behind the body and one in front. One `Equip_Main`. Idle after the cast was `Aren_Idle_Down_2` with `Aren_Sword_BackHilt`.

## 6. Files

- `Assets/Scripts/Player/PlayerVisual.cs` — opt-in `PlaySkillPose`
- `Assets/Scripts/Combat/PlayerCombat.cs` — calls it only when the designed kit is active; Shield Bash VFX moved to the impact
- `Assets/Editor/R3E3SkillBodySetup.cs`
- `Assets/Art/Rebuild/R2C/Aren/Skills/`, the 13 clips, `Aren.controller`, `Aren_Anchors_BackGear.csv`

No scene save. `ReferenceCombatSandbox.unity` was not changed by this step.

## 7. Tests

| ID | Result |
| --- | --- |
| T1–T2 slice count, pivot, PPU | PASS. 28 sprites. |
| T3 Shield Bash body + cone on frame 1 | PASS |
| T4 Stun + stun icon | PASS. Goblin stunned. Icon visible in the portrait shot. |
| T5 Charge body + override + dust | PASS. Speed and duration unchanged. |
| T6 Whirl body, no flip, back/front | PASS. `whirlBack=1 whirlFront=1`. |
| T7 Bulwark body, back/front, then idle | PASS |
| T8 Dawn side frame 1, flip right, slash spawned | PASS |
| T9 Return to Idle + back sword | PASS. `Aren_Idle_Down_2`, `Aren_Sword_BackHilt`. |
| T10 Hurt / Death | PASS. Hurt during the pose stayed on the skill frame. Hurt after played `Hero_Hurt_Down`. Death played `Hero_Death` and blocked later poses until revive. |
| T11 One weapon, legacy sword off | PASS |
| T12 R combo | PASS. `Aren_Attack_Down_0`. |
| T13 Damage / hitstop / knockback numbers | PASS. Not edited. |
| T14 Labels | PASS. 6 labels on each reload. |
| T15 DamagePopup | PASS. Hero 400 → 396, no exception. |
| T16 F1 icons | PASS |
| T17 Portrait 576×1024, PPC 144×256, PPU 16 | PASS |
| T18 Landscape 1920×1080, PPC 480×270, PPU 16 | PASS. Side margins are the existing ground framing. |
| T19 Stop / Play ×3 | PASS. Poses, states, labels, popup. |
| T20 Campaign Level 1 fight | NOT TESTED. `GameplayPrototype` has a player and no `LevelFlow`, so no wave spawned. |
| T21 New console errors | PASS. 0 errors after the play tests. |

Shots: `Docs/ReferenceRebuild/R3/R3E3_shots/` — `portrait_shield_bash.png`, `portrait_charge.png`, `portrait_whirl.png`, `portrait_bulwark.png`, `portrait_dawn.png`, `portrait_idle.png`, `landscape_hud.png`.

## 8. Missing art

The sheet has no separate left/right cells (Side is left, runtime flip). Whirl has no per-direction row; that matches the layout. No approved Attack2/Attack3 body frames, so R was not retargeted. Vex, Brom, Lyra, and Selene skill sheets were not imported.

## 9. Campaign

After the change, campaign play still has `Kit == null`, `PreferDesignedSkills == false`, no `ClassCombat`, no icon catalog, and no animator on that player (`skillState=false`). The new states live on `Aren.controller`, which this scene does not use. A real Level 1 wave was not fought.

## 10. Git

No commit. No push.

This step: `PlayerVisual.cs`, `PlayerCombat.cs`, `Aren.controller`, `R3E3SkillBodySetup.cs`, `Aren/Skills/`, 13 new clips, skill rows inside the still-untracked `Aren_Anchors_BackGear.csv`.

`ClassKit_Aren.asset` is still only the earlier equipment-guid line. Slot 2 on disk remains Flame Burst.
