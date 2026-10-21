# R3E.5B — Aren Designed Kit campaign rollout

Campaign Level 1–4 now runs `ClassKit_Aren_Designed` from the scene Player. No commit. No Level 5. No Phase 12E. Rollback was not executed.

## 1. Campaign Player after rollout

Scene `Assets/Scenes/GameplayPrototype_Phase4Test.unity`. The Player is still the `Player_Aren.prefab` instance (source guid `8e48e32bf488b6241a97ffb626b09e7e`).

One added component on that instance only:

| Field | Value |
| --- | --- |
| Script | `ClassCombat` (`921aad619e7d16d49bbc19b921a22d96`) |
| `kits[0]` | `ClassKit_Aren_Designed` (`3b1a7d70ccfde54429c02fb038234eba`) |
| `kits[1..4]` | null |
| `startSlot` | 0 |
| `showLabel` | false |
| `uiIcons` | `ClassUiIcons_Sandbox` (`2f19053892bd7fd4a89ab998d474447b`) |

`Player_Aren.prefab` has no `ClassCombat`. Play mode had one Player, one `PlayerCombat`, one `ClassCombat`, one `WorldLabelsCanvas`, one Animator.

`ClassCombat` runs at execution order −10. `Awake` assigns the icon catalog. `Start` calls `Select(0)`. `Select` on a null kit returns false and leaves the active kit. `Select(2)` during the main menu stayed on slot 0. F2–F5 cannot swap to Brom, Lyra, Selene, or Vex on this scene.

`OnDestroy` clears `ClassUiIcons.Active` and sets `ArenKitVfx.PreferDesignedSkills` false only when the destroyed kit's slot 2 is Shield Bash. Retry does not destroy the component, so the flag stays. Stop/Play three times with domain reload off (`m_EnterPlayModeOptionsEnabled: 1`, `m_EnterPlayModeOptions: 1`) each came back as one Player, designed true, slot 2 Shield Bash.

PASS.

## 2. Runtime skill map, Level 1–4

Same map on every level load, after Retry, and after each Stop/Play:

| Key | Runtime name | Measured |
| --- | --- | --- |
| R | Sword combo | 18 per hit. Hold produced 60 hits, 1080 damage, remainder 0, over `Time.time` 547.49–565.47 |
| 2 | Shield Bash | 30. Goblin stun remaining 0.32. Gobrider stun 0.20 and AI paused, then AI resumed |
| 3 | Charge | 26, duration 0.30 |
| 4 | Whirl Ring | 20 × 3. Three Goblins, 180 total, no fourth wave |
| V | Bulwark | 20 incoming became 7 |
| C | Dawn | 42 once. A second tick dealt 0 |

Keys 2 and 3 did not cast Flame Burst or Dash Slash. `ClassKit_Aren.asset` still has `displayName: Flame Burst`. `FillDesignedAren()` was not called.

PASS.

## 3. Designed Kit activation

`PreferDesignedSkills` was true from the main menu through Level 1, 2, 3, and 4, and after Retry. Console log: `Aren (F1) combo 3 hits, skills: Shield Bash / Charge / Whirl Ring / Bulwark / Dawn`.

`LevelFlow` was not edited. Level load does not clear the kit. `Select` does not rewrite the asset.

PASS.

## 4. Animation, VFX, equipment

- Body states used: `Aren_Skill_ShieldBash_Down_1`, `Aren_Skill_Charge_Side_1`, `Aren_Skill_Whirl_1`, `Aren_Skill_Bulwark_Down_0`, `Aren_Skill_Dawnbreaker_Down_2`.
- Death clip after a kill during Dawn: `Aren_Death` / `Aren_Death_Down_2`. After Retry the lock cleared and the body returned to `Aren_Idle_Up_0`.
- Equipment set: `EquipmentSet_Aren_BackGear`. Idle sword sprite `Aren_Sword_DR`.
- One Animator on the Player.
- Whirl damage and the Shield Bash stun stars path are the designed VFX/status path (`StatusStun.Apply` from `ApplyConeDamage`).

PASS.

## 5. Hotbar and status

Hotbar sprites on Level 1 and still on the Level 4 victory screen:

`Icon_Aren_2_ShieldBash`, `Icon_Aren_3_Charge`, `Icon_Aren_4_WhirlSlash`, `Icon_Aren_V_Bulwark`, `Icon_Aren_C_Dawnbreaker`, `Icon_Aren_R_Combo`.

Retry left those six icons. Cooldowns summed to 0 after Retry. Guard was off.

Stun showed on a successful Shield Bash (remaining 0.32 on Goblin, 0.20 on Gobrider). Bulwark reduced damage only while `IsGuarding` was true. Slow, Taunt, Absorb, Haste, Dodge, and Reflect were not shown.

Mob labels stayed on: `Goblin Lv.3`, and on Level 4 `Elite Gobrider Lv.6`, with HP bars. One `WorldLabelsCanvas`. Alpha was not forced off.

Overlap that was left visible:

- Two Goblin labels stacked on each other during the Whirl shot.
- A Goblin label sat across the Game Over stats line.
- A label placed at the bottom of the portrait view sat on the hotbar. It was not hidden.

PASS for content. Overlap remains, see risks.

## 6. Level 1–4

Waves were the real spawner waves. Clears went through `Health.TakeDamage` → death → `OnWaveCleared`. Exit levels used the active `LevelExitGate` trigger. `TriggerVictory()` was not called. Enemy counts and stats were not edited.

| Level | Result | Evidence |
| --- | --- | --- |
| 1 | PASS | Designed kit. Bash 30 + stun 0.32. Charge 26. Combo 18. Whirl 180 on 3. Bulwark 7. Dawn 42 once. Waves 1–4. Objective CLEAR WAVES. Victory. Best score stayed 1174 |
| 2 | PASS | Shield Bash / Charge 26 still selected. Wave-1 Goblins spawned with AI. Four waves. Objective became REACH THE EXIT. Gate trigger victory. Best score stayed 832 |
| 3 | PASS | Whirl body `Aren_Skill_Whirl_1`. Whirl 180 on the three live Goblins. Labels `Goblin Lv.3`. CLEAR WAVES victory. Best score stayed 1037 |
| 4 | PASS | Kit still Shield Bash. Wave 2 Gobrider: 30 damage, stun 0.20, AI off, then AI on again at 70 HP. Wave 4 commander: label `Elite Gobrider Lv.6`, max HP 160, scale 1.12, bash 30, stun 0.20. No stun-immunity flag exists, so the commander is stunned as a Gobrider. Four waves. Exit at (0.2, 10.5). Victory. Best score stayed 953. Unlock stayed 5. Level 5 was not started |

## 7. Victory, Game Over, Retry

Death during Dawn: hero died with the hit still pending (`_crescentDelay` 0.12). `LevelFlow` disables `PlayerCombat` in `LockGameplay`, so `TickCrescentStrike` did not run. The Goblin stayed at 300 HP. `ApplyAreaDamage` also returns when the hero is dead. The pending flag stayed true because Update no longer runs while the component is disabled. Retry calls `ResetCombatState`, which clears it.

Game Over UI: GAME OVER, wave, kills, combo, time, RETRY, BACK TO MENU.

Retry: state Playing, 1 Player, 1 ClassCombat, 1 world canvas, designed true, slot 2 Shield Bash, alive at 400 HP, cooldown sum 0, guard false, six Aren icons, death pose cleared to idle.

Level 1 victory UI: LEVEL COMPLETE, 22 defeats, score 650 (below the stored 1174). Level 4 victory UI: LEVEL COMPLETE, 21 defeats, score 645 (below the stored 953). Next Level was not pressed.

PASS.

## 8. Save / Load

Read before Level 2 and again after Level 4. Unchanged:

| Key | Value |
| --- | --- |
| `gp.unlockedLevel` | 5 |
| `gp.bestScore.l1` | 1174 |
| `gp.bestScore.l2` | 832 |
| `gp.bestScore.l3` | 1037 |
| `gp.bestScore.l4` | 953 |
| `gp.bestScore.l5` | 781 |

Combo, stars, and best time keys were read before Level 2 and matched the existing record (L1 combo 40 / stars 3 / time 35.526, and the same pattern for L2–L5). `GameProgress.Reset` was not called. No save file was overwritten. Old keys still resolve.

PASS.

## 9. Portrait and landscape

| Profile | Screen | PPC | PPU |
| --- | --- | --- | --- |
| Portrait | 576×1024 | 144×256 | 16 |
| Landscape | 1920×1080 | 480×270 | 16 |

Landscape Level 1 filled the frame with the campaign ground. Trees, Aren, the BackGear read, hotbar, and `Goblin Lv.3` labels were on screen. `TreeCanopyOcclusion` count 70, `coveredAlpha` 0.40. Camera zoom, PPU, and filter were not changed.

PASS.

## 10. FPS and console

Editor only. Not a player build and not a phone.

- One Level 3 frame: CPU 2.21 ms, main thread 1.33 ms, GPU 0.24 ms, 133 draw calls, 12 batches. Mono used 972 MB, mono heap 1372 MB.
- Across the Level 3 clear window: frame 468286 → 515641 (47355 frames), realtime 720.03 → 797.40 (77.4 s), about 612 FPS average. That window includes idle time between checks.

Console after the level runs and three Stop/Play cycles: 0 errors, 0 warnings. `ClassCombat` logged the designed skill names. Aspect logs were the portrait and landscape applies.

Charge into the Level 1 bush: start x 1.60, bush face 2.15, far side 2.65, stop x 1.87, velocity 0, far side not crossed.

PASS.

## 11. Files changed

- `Assets/Scenes/GameplayPrototype_Phase4Test.unity` — one `ClassCombat` on the Player instance.
- `Assets/Scripts/Combat/ClassCombat.cs` — campaign comment, and `OnDestroy` clears the icon catalog and the designed-skill flag.
- `Docs/ReferenceRebuild/R3/R3E5B_CAMPAIGN_ROLLOUT_REPORT.md`
- `Docs/ReferenceRebuild/R3/R3E5B_shots/` — the captures below.
- `IncomingAssets/backup_before_R3E5B_Rollout/` — scene and `ClassCombat.cs` from before the edit.

Not changed: `Player_Aren.prefab`, `ClassKit_Aren.asset`, `ClassKit_Aren_Designed.asset` numbers, `LevelFlow`, `GameProgress`, sandbox slot 0, enemy counts, camera, PPU.

`Assets/Docs/R3E5B_whirl.png` and its `.meta` are a leftover temp capture. The copy used by this report is under `R3E5B_shots`. Delete that Assets file when convenient. The other temp captures were removed.

## 12. PASS / FAIL / NOT TESTED

| Check | Result |
| --- | --- |
| One campaign ClassCombat, prefab untouched | PASS |
| Designed kit on start, level change, Retry, Stop/Play ×3 | PASS |
| F2–F5 do not drop Aren | PASS |
| Skill numbers and no legacy 2/3 | PASS |
| Body anim, BackGear, one Animator, death clip | PASS |
| Hotbar icons, stun, Bulwark reduction, labels | PASS |
| Level 1 waves and victory | PASS |
| Level 2 kit, AI, exit | PASS |
| Level 3 body, whirl, labels, victory | PASS |
| Level 4 kit, Gobrider, commander, exit, no Level 5 | PASS |
| Death during cast does no later damage | PASS |
| Game Over and Retry | PASS |
| Save keys unchanged and readable | PASS |
| Portrait and landscape | PASS |
| Console errors | PASS |
| Editor frame sample | PASS, editor only |
| Label vs hotbar / stacked labels | PASS for visibility. Overlap not solved |
| Elite stun immunity | PASS as current design: commander is stunned for 0.20 s. There is no immunity |

Overall: PASS.

## 13. Rollback (not executed)

Restore the campaign Player to the pre-rollout scene. Keep the prefab, the legacy kit, the designed kit, and the sandbox.

1. Replace `Assets/Scenes/GameplayPrototype_Phase4Test.unity` with `IncomingAssets/backup_before_R3E5B_Rollout/GameplayPrototype_Phase4Test.unity`, or delete the added `ClassCombat` on the Player instance (`fileID 5193642696194807065`, script guid `921aad619e7d16d49bbc19b921a22d96`, `m_AddedComponents` on prefab instance `5193642696194807063`).
2. Leave `Player_Aren.prefab` as it is.
3. Leave `ClassKit_Aren.asset` (Flame Burst) and `ClassKit_Aren_Designed.asset` on disk.
4. Leave sandbox `kits[0]` on `ClassKit_Aren_Designed`.
5. Do not call `GameProgress.Reset` and do not write PlayerPrefs.
6. Optional code restore: `IncomingAssets/backup_before_R3E5B_Rollout/ClassCombat.cs`. The `OnDestroy` cleanup is safe to keep if the scene component is removed.

Removing the scene component returns combat to the prefab `PlayerCombat` fields (Flame Burst / Dash Slash). Do not delete the designed asset.

## 14. Remaining risk

- Labels can overlap each other, the Game Over stats, or the hotbar when a mob is at the bottom edge. They stay readable and were not hidden.
- `StatusStun` has no elite exception. `Elite Gobrider Lv.6` takes the 0.20 s Gobrider stun.
- A Dawn started as the hero dies leaves `_crescentPending` true until Retry or the next `ResetCombatState`. The component is disabled, and `ApplyAreaDamage` bails out if the hero is dead, so the hit did not land.
- The R-hold rate on a frozen Goblin was 60 hits in about 18 s, each exactly 18. That is slightly faster than a strict 0.35 s gap. Damage was not doubled. Cooldown values were not changed.
- Editor FPS is not a mobile figure.
- One temp PNG remains at `Assets/Docs/R3E5B_whirl.png`.

## Shots

`Docs/ReferenceRebuild/R3/R3E5B_shots/`

- `R3E5B_l1_portrait_bash.png` — Level 1 portrait, Shield Bash, stun badge, Goblin label, hotbar
- `R3E5B_l1_landscape.png` — Level 1 landscape, trees, labels, hotbar
- `R3E5B_charge.png` — Charge body
- `R3E5B_whirl.png` — Whirl body and two Goblins
- `R3E5B_bulwark.png` — Bulwark body
- `R3E5B_dawn.png` — Dawnbreaker body
- `R3E5B_victory_l1.png` — Level 1 victory
- `R3E5B_gameover.png` — Game Over and Retry
- `R3E5B_hotbar.png` — Level 4 victory with the six Aren icons

Stopped after R3E.5B.
