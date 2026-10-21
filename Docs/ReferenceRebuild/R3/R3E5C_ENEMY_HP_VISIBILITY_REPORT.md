# R3E.5C — Contextual enemy HP bar

Name and level stay on. The HP bar appears only after that mob loses HP, then hides 3.0 seconds after the latest hit. No commit. Designed Kit, damage numbers, and Boss Warlord logic were not retuned.

## Scripts

Changed: `Assets/Scripts/Enemy/EnemyWorldUI.cs` only.

Not changed: `Health.cs`, `WorldLabelsCanvas.cs`, `DamagePopup.cs`, `EnemyController.cs`, `Player_Aren`, class kits, `LevelFlow`, `GameProgress`.

Backup: `IncomingAssets/backup_before_R3E5C_HpBar/EnemyWorldUI.cs`.

`Health.Damaged` already fires only after HP is actually reduced (`applied > 0`). Misses, `amount <= 0`, invulnerability, and a 0 multiplier return before that event. Aggro and attacks do not raise it.

## HP display

- Spawn and reuse start with `HpBg` inactive. Name stays active. The label root size is unchanged, so the name does not jump when the bar toggles.
- `OnDamaged` sets `_hpVisibleUntil = Time.time + showHpAfterHitSeconds` (`showHpAfterHitSeconds = 3`) and enables that mob's bar. Fill width is `barWidth * CurrentHp / MaxHp`.
- `LateUpdate` hides the bar when `Time.time` passes that stamp. No per-mob coroutine.
- `Time.time` stops while `timeScale` is 0, so Pause holds the remaining time.
- A later hit replaces the stamp. Other mobs keep their own stamps.
- `Configure` / `ReviveFull` refill HP through `Changed` and do not show the bar. `OnEnable` clears the stamp so a reused component does not keep the previous timer.
- Death calls the existing `DestroyRoot`. The bar is a child of that label, not a separate canvas object.
- Stun and bleed icons still toggle on their own images.
- `BossWarlordController` still forces the bar on. That path was not played. Phase 12E was not opened.

## Tests

| Test | Result |
| --- | --- |
| T1 Spawn: name + level, bar off | PASS. Level 1: 3 labels, 0 bars |
| T2 Goblin loses 18 | PASS. 60 → 42, fill width 39.2 (56 × 0.70), bar on |
| T3 Hidden after 3s, name remains | PASS. At t=104.20, 3 names, 0 bars |
| T4 Later hit refreshes that mob | PASS. Stamp 89.65 replaced by 92.45 (now + 3) |
| T5 Six mobs, only the hit one | PASS. Level 4 wave 3: 6 alive, 1 bar, 42/60 |
| T6 Shield Bash, independent bars | PASS. 3 mobs in the cone, each 200 → 170, 3 bars |
| T7 Whirl, no duplicate labels | PASS. 3 targets, drop 180, labels 3 → 3, 9 popups |
| T8 Chase without HP loss | PASS. State Approach, 200/200, bar off |
| T9 Stun icon while bar hidden | PASS. `Status_Stun` on, bar off, name on, HP unchanged |
| T10 Death removes the bar | PASS. After destroy: 2 labels, 2 bars, 2 alive |
| T11 Reuse clears the timer | PASS. Disable/enable: until −1, bar off, name on. New Level 2/3/4 spawns also started at 0 bars |
| T12 Pause freezes the timer | PASS. Paused 2.0s realtime, `Time.time` stayed 240.781, bar stayed on, then hid after resume |
| T13 Portrait | PASS. 576×1024, PPC 144×256, PPU 16. Corner label rect inside the screen |
| T14 Landscape | PASS. 1920×1080, PPC 480×270, PPU 16 |
| T15 Designed Kit on Level 1–4 | PASS. Slot 2 Shield Bash on L1, L2, L3, L4. L3 also Whirl Ring / Bulwark / Dawn |
| T16 Game Over, Retry, Victory | PASS. Game Over, Retry = 1 player, Shield Bash, fresh bars off. Level 1 wave 4 Victory. Best score stayed 1174, unlock stayed 5 |
| T17 Edge clamp | PASS. Portrait corner `Goblin Lv.3` stayed inside |
| T18 One canvas, no NRE | PASS. One `WorldLabelsCanvas` |
| T19 Console | PASS. 0 errors, 0 warnings |
| Boss Warlord bar | NOT TESTED. Code still keeps that bar visible |

Zero-damage and invulnerable `TakeDamage` left every bar off.

## Regression

Aren Designed Kit still selected. Hero HP bar at the top of the screen was not retargeted. Enemy counts, AI, and save keys were not edited. Labels can still overlap each other when mobs stack. That was already true and they were not hidden.

## Shots

`Docs/ReferenceRebuild/R3/R3E5C_shots/`

- `R3E5C_before_hp_always.png` — before, bars on at spawn
- `R3E5C_after_name_only.png` — after, name + level only
- `R3E5C_after_just_hit.png` — portrait, one Goblin at 42/60 with a bar, the others without
- `R3E5C_after_3s.png` — names remain, bars off
- `R3E5C_portrait_name.png` — portrait labels

`Assets/Docs/R3E5C_before_hp_always.png` is a leftover temp copy. The report copy is in `R3E5C_shots`.

Stopped after R3E.5C.
