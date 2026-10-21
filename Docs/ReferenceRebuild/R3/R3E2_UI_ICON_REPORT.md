# R3E.2 — DamagePopup + hotbar / status icons

Sandbox only. Campaign Level 1–4 was not switched to the Aren Designed Kit. No commit.

## 1. DamagePopup cause and fix

`DamagePopup.Spawn` called `WorldLabelsCanvas.Ensure()`, then parented the popup to `Instance.Root`. `Ensure()` could return the scene canvas before `Awake`, so `Instance` was still null. `SetParent` threw `NullReferenceException`. Stack from the R3E.1 play session: `EnemyController.FixedUpdate` → `TickAttack` → `DealDamage` → `DamagePopup.Spawn` → `GetFromPool` (`DamagePopup.cs` old line 44).

`Ensure()` now builds that canvas and assigns `Instance` before anything parents to it. A missing root throws `InvalidOperationException`. The existing overlay canvas is reused. The pool still lives on one `DontDestroyOnLoad` root, and a destroyed pooled object is skipped.

## 2. Files

- `Assets/Scripts/UI/WorldLabelsCanvas.cs`
- `Assets/Scripts/Combat/DamagePopup.cs`
- `Assets/Scripts/UI/SkillHotbarUI.cs` — class sprites, cooldown disc inset, sandbox HUD stays visible, hero buff row
- `Assets/Scripts/UI/ClassUiIcons.cs` — new catalog
- `Assets/Scripts/Combat/ClassCombat.cs` — assigns that catalog in `Awake`
- `Assets/Scripts/Enemy/EnemyWorldUI.cs` — stun / bleed icons above the name
- `Assets/Scripts/Combat/StatusBleed.cs` — `IsBleeding`
- `Assets/Editor/R3E2IconSetup.cs` — menu `Game01/R3E2/Import Sandbox Icons`
- `Assets/Art/Rebuild/R2C/UI/` — sprites and `ClassUiIcons_Sandbox.asset`
- `Assets/Scenes/ReferenceCombatSandbox.unity` — `ClassCombat.uiIcons` only, plus the already uncommitted F5 Vex kit reference

Weapon sheets, anchors, and `ClassKit_Aren` damage fields were not edited. The uncommitted `ClassKit_Aren` diff is still the single R3E.1 equipment guid (`0582f77d4b4097f4e87c750e692ab297`). Slot 2 on disk is still `Flame Burst`.

## 3. Imported icons

Source: `Docs/ReferenceRebuild/R2/R2C_OriginalHero/icons/`. Hotbar 32×32, status 24×24. Sprite, Point, PPU 16, no mipmaps, alpha preserved, uncompressed (Default and Standalone).

`Assets/Art/Rebuild/R2C/UI/Icons/`

- `Icon_Aren_R_Combo.png`, `Icon_Aren_2_ShieldBash.png`, `Icon_Aren_3_Charge.png`, `Icon_Aren_4_WhirlSlash.png`, `Icon_Aren_V_Bulwark.png`, `Icon_Aren_C_Dawnbreaker.png`
- `Icon_Vex_R_Combo.png`, `Icon_Vex_2_ShadowStep.png`, `Icon_Vex_3_KnifeThrow.png`, `Icon_Vex_4_BladeDance.png`, `Icon_Vex_V_SmokeBomb.png`, `Icon_Vex_C_Execute.png`

`Assets/Art/Rebuild/R2C/UI/Icons/Status/`

- `Status_Stun.png`, `Status_Bleed.png`, `Status_DamageReduction.png`, `Status_Stealth.png`, `Status_Empowered.png`

Catalog: `Assets/Art/Rebuild/R2C/UI/ClassUiIcons_Sandbox.asset`, referenced by the sandbox player only.

## 4. F1 Aren

Same hotbar. Sprites follow the designed kit that `ClassCombat.Select(0)` already applies at runtime. Skill logic and `ClassKit_Aren` numbers were not changed.

| Slot | Sprite | Runtime skill |
| --- | --- | --- |
| R | `Icon_Aren_R_Combo` | Sword / thrust finisher |
| 2 | `Icon_Aren_2_ShieldBash` | Shield Bash |
| 3 | `Icon_Aren_3_Charge` | Charge |
| 4 | `Icon_Aren_4_WhirlSlash` | Whirl Ring |
| V | `Icon_Aren_V_Bulwark` | Bulwark |
| C | `Icon_Aren_C_Dawnbreaker` | Dawn |

## 5. F5 Vex

| Slot | Sprite | Kit skill |
| --- | --- | --- |
| R | `Icon_Vex_R_Combo` | Combo |
| 2 | `Icon_Vex_2_ShadowStep` | Shadow Step |
| 3 | `Icon_Vex_3_KnifeThrow` | Knife Throw |
| 4 | `Icon_Vex_4_BladeDance` | Blade Dance |
| V | `Icon_Vex_V_SmokeBomb` | Smoke Bomb |
| C | `Icon_Vex_C_Execute` | Execute |

F5 replaces every Aren sprite. `SetKit` clears cooldowns; measured fill was 0.00 on all six slots after the switch. Vex combat and gear were not edited.

## 6. Status icons

| Icon | Connection | This session |
| --- | --- | --- |
| Stun | `StatusStun.IsStunned` on the mob label | Shown. One stun icon enabled, sprite `Status_Stun`. |
| Bleed | `StatusBleed.IsBleeding` on the mob label | Wired. Live bleed was not applied. |
| Damage Reduction | `PlayerCombat.IsGuarding` on the hero buff row | Wired. Guard was not activated. |
| Stealth | `PlayerCombat.IsStealthed` | Wired. Not activated. |
| Empowered | `PlayerCombat.NextHitMultiplier > 1` | Wired. Not activated. |

Not imported and not shown: Slow, Taunt, Absorb, Haste, DodgeReady, Reflect. No matching runtime status.

## 7. Portrait 9:16

Screen 576×1024, PPC 144×256, PPU 16. HUD on. Six mob name/HP labels visible. Damage number `18` on the hero, on screen. F1 and F5 sprites matched the tables. Cooldown disc is 0.62× the glyph (R 96px disc 59px, other slots 64px disc 39px) so the frame stays outside the disc. One `WorldLabelsCanvas`. Console errors: 0.

`Docs/ReferenceRebuild/R3/R3E2_shots/portrait_f1_damage.png`  
`Docs/ReferenceRebuild/R3/R3E2_shots/portrait_f5_stun.png`

## 8. Landscape 16:9

Screen 1920×1080, PPC 480×270, PPU 16. HUD on. Popup spawned on the hero was inside the screen. F1 and F5 sprites matched. Labels remained. Console errors: 0.

The capture shows dark-green margins beside the ground. Camera bounds, orthographic size (32), and PPC authored values were put back to the scene file. They were not retuned.

`Docs/ReferenceRebuild/R3/R3E2_shots/landscape_f1_damage.png`  
`Docs/ReferenceRebuild/R3/R3E2_shots/landscape_f5.png`

## 9. Results

| Check | Result |
| --- | --- |
| Null `WorldLabelsCanvas.Instance`, then `Spawn` | PASS. One canvas, parent `WorldLabelsCanvas`. |
| One enemy hit and six enemies hitting Aren | PASS. Hero HP 216→192, then 392→374 on the third reload. |
| Enemy killed while a popup was alive | PASS. No new exception. Six labels still present during the death delay. |
| Stop / Play ×3, domain reload off | PASS. Popup, one canvas, six labels, icon catalog non-null each time. |
| Sandbox Retry | NOT TESTED. Sandbox disables `LevelFlow` and `GameOverController`, so there is no Retry control. |
| F1 / F5 icons and cooldown reset | PASS |
| Stun icon | PASS |
| Bleed, Guard, Stealth, Empowered live | NOT TESTED |
| Campaign Level 1–4 play | NOT TESTED |
| Tree occlusion | NOT TESTED |

## 10. Still missing

Brom, Lyra, and Selene hotbar art exists in the manifest and was not imported. F2–F4 stay on the existing “no kit” notice. Status art for Slow, Taunt, Absorb, Haste, DodgeReady, and Reflect was not imported.

## 11. Git diff in scope

No commit. No push.

Modified: `DamagePopup.cs`, `WorldLabelsCanvas.cs`, `SkillHotbarUI.cs`, `EnemyWorldUI.cs`, `ClassCombat.cs`, `StatusBleed.cs`, `ReferenceCombatSandbox.unity` (`uiIcons` plus the earlier F5 kit reference), `ClassKit_Aren.asset` (R3E.1 equipment guid only).

Untracked: `ClassUiIcons.cs`, `R3E2IconSetup.cs`, `Assets/Art/Rebuild/R2C/UI/`, `Docs/ReferenceRebuild/R3/R3E2_shots/`, this report.

## 12. Campaign

`GameplayPrototype.unity` has no `ClassCombat` and no icon catalog. `ArenKitVfx.PreferDesignedSkills` still defaults to false and is set true only when sandbox F1 selects Aren. With no catalog, the hotbar keeps the procedural glyphs: R Sword, 2 FireDash, 3 MultiSlash, 4 MawBite, V Bone, C Mace. Designed Kit, these icons, and the new equipment were not rolled onto Level 1–4. A campaign play pass was not run.
