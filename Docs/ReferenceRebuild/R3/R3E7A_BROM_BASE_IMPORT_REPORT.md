# R3E.7A — Brom base import (visual only)

Brom’s approved body, hurt, death-down and gear are in the sandbox as a visual preview. No Brom kit was created. F2 stays empty. Aren and Vex were not retuned. No commit.

## 1. Art nguồn thực tế

From `HANDOFF_INDEX.md`. Brom’s export is `Docs/ReferenceRebuild/R2/R2C_OriginalHero/unity_export_brom/`.

| File | Size checked |
| --- | --- |
| `Brom_Sheet.png` | 128×288, 4×9 cells of 32 |
| `Brom_Anchors.csv` | 14 columns |
| `BROM_MANIFEST.csv` | grid, pivot, PPU |
| `Gear/Brom_Hammer.png` | 384×48, 8 poses |
| `Gear/Brom_Shield.png` | 48×16, 3 views |
| `Gear/Brom_Hand.png` | 4×4 |
| `Gear/Brom_ComboVfx.png` | 864×48, 18 cells |
| `Extra/Brom_Hurt_{Down,Up,Side}_{0,1}.png` | 32×32 |
| `Extra/Brom_Death_Down_{0,1,2}.png` | 32×32 |

Also read: `brom/BROM_KIT_SPEC.md`, `brom/BROM_SKILL_BODY_NOTE.md`, `roster/BACK_GEAR_NOTE.md`, `roster/CLASS_AUDIT_FIX_NOTE.md`, `R3E6_VEX_SKILL_BODY_REPORT.md`.

## 2. Đã duyệt / chưa duyệt

| Art | Status |
| --- | --- |
| Body sheet, hammer, shield, glove, combo VFX, hurt, death down, back-gear anchors | Imported. Handoff lists this export as not yet in Unity. The kit spec’s separate approval is for stun, taunt and the 15% damage reduction, which were not coded. |
| Skill body `Skills/Brom_SkillSheet.png` and skill VFX | Poses are marked approved in `BROM_SKILL_BODY_NOTE.md`. Not imported. This step does not build a skill kit. |
| `Extra/Brom_Death_Up_*` and `Brom_Death_Side_*` | Awaiting approval in `CLASS_AUDIT_FIX_NOTE.md`. Not imported. Their anchor rows were left out of the Unity CSV. |

## 3. Sprite đã import

72 sprites under `Assets/Art/Rebuild/R2C/Brom/`. PPU 16, Point, no mipmaps, uncompressed, alpha transparency, full-rect mesh, pivot from the manifest.

| Group | Count |
| --- | --- |
| Idle Down/Up/Side | 4 frames each |
| Walk Down/Up/Side | 4 frames each |
| Attack Down/Up/Side | 3 frames each |
| Hurt Down/Up/Side | 2 frames each |
| Death Down | 3 frames |
| Hammer R, UR, U, UL, L, DL, D, DR | 8 |
| Shield Down, Up, Side | 3 |
| Hand | 1 |
| Combo VFX, step × view × frame | 18 |

`Brom_Idle_Down_0` rect is 32×32 at the top row, pivot `(0.5, 0.0625)`. Runtime pivot reads 16, 2 pixels.

## 4. Animator clips

13 states on `Assets/Art/Rebuild/R2C/Brom/Animators/Brom.controller`. State names stay `Hero_*` so the existing `PlayerVisual` can play them. No skill states were added.

| Clip | Timing |
| --- | --- |
| `Brom_Idle_{Down,Up,Side}` | 0, 0.25, 0.40, 0.65, end 0.80, loop. Same body timing as Vex. |
| `Brom_Walk_{Down,Up,Side}` | 0, 0.125, 0.25, 0.375, end 0.50, loop. |
| `Brom_Attack_{Down,Up,Side}` | 0, 0.16, 0.29, end 0.42. Impact at 0.16s from `BROM_KIT_SPEC.md`. |
| `Brom_Hurt_{Down,Up,Side}` | 0, 0.06, end 0.18. |
| `Brom_Death` | Down only: 0, 0.15, 0.30, hold at 0.60, end 0.68. `PlayerVisual` plays `Hero_Death`. |

## 5. Equipment / anchors

`Assets/Art/Rebuild/R2C/ClassKits/EquipmentSet_Brom.asset` only. Aren and Vex sets were not edited.

The Unity CSV is the Docs table without the six unapproved Death Up/Side rows. Idle, walk and hurt keep the hammer on the back and the hands empty. Attack and the death kneel draw the hammer, glove and shield. Side facing left is the sheet direction. Facing right sets `flipX` and mirrors the local X of the hammer and shield.

Sampled order offsets: idle-down hammer is 2 behind the body; idle-up shield is in front. One hammer renderer, never a second copy. Death frame 1 hides the hammer, glove and shield.

`@step` rows are in the CSV. They are not driven, because there is no Brom kit and `CurrentComboStep` stays −1. The existing Aren combo tilt in `EquipmentVisual` was not changed.

## 6. Preview

**VISUAL ONLY.**

Play `ReferenceCombatSandbox`, then run `Game01/R3E7A/Preview Brom Visual (Play Mode)`. That calls the existing `RebindBodyController` and `EquipmentVisual.Configure`. It does not call `SetKit`.

The class label and hotbar stay on the active kit (Aren at start). F2 still toasts that the kit is missing and does not change the slot. `Game01/R3E7A/Restore Active Class Visual (Play Mode)`, or F1 / F5, puts the active class body back.

No second `PlayerCombat`. No new class picker. No Aren or Vex skills were copied onto a Brom kit.

## 7. Hai aspect

| Aspect | Result |
| --- | --- |
| Portrait | PASS. 576×1024, PPC 144×256, PPU 16. |
| Landscape | PASS. 1920×1080, PPC 480×270, PPU 16. |

Portrait was restored. Neither scene was saved.

## 8. Regression Aren / Vex

| Check | Result |
| --- | --- |
| F2 while Brom is only a visual | PASS. `Select(1)` returned false. Active stayed Aren slot 0. |
| Restore, then F1 | PASS. Controller `Aren`, `Aren_Idle_Down_0`, slot 2 `ShieldBash`. |
| F5 | PASS. Controller `Vex`, `Vex_Idle_Down_0`, slot 2 `ShadowStep`. |
| F5 → F1 | PASS. Aren body returned. |
| Campaign Level 1 | PASS. 1 player, `ClassKit_Aren_Designed`, controller `Aren`, 0 `Brom_` sprites. |
| Enemy labels and HP bar | PASS. 3 names, 0 bars. After 5 damage, 1 bar. |
| DamagePopup | PASS. `DamagePopup.Spawn` created one popup. No exception. |
| Console during the play tests | PASS. 0 errors, 0 warnings after the console clear. |

The import compile still printed the existing `ArenKitVfxImporter` CS0618 warning. It is not from this task.

Levels 2–4 were not replayed. Boss Warlord was not opened.

## 9. Còn thiếu / chưa làm

- Death Up and Death Side clips. Art is in Docs and is awaiting approval.
- Skill body and skill VFX. Approved on paper, not part of this base step.
- A playable Brom kit: stun, taunt, 15% damage reduction, and the five skills. Not created.
- Death lying frame (`Death_Down_2`) gear was not isolated in a screenshot. Frame 0 and frame 1 were.

## 10. Git diff của task này

No commit, no push, no reset. Scenes were not saved. Their existing uncommitted diffs are unchanged (sandbox 5 lines, campaign 43 lines).

Added:

- `Assets/Editor/R3E7ABromBaseSetup.cs`
- `Assets/Art/Rebuild/R2C/Brom/`
- `Assets/Art/Rebuild/R2C/ClassKits/EquipmentSet_Brom.asset`
- `IncomingAssets/backup_before_R3E7A_BromBase/README.txt`
- this report and `Docs/ReferenceRebuild/R3/R3E7A_shots/`

The R3E.1–R3E.6 tree was not overwritten. Some temporary captures remain under `Assets/Docs/R3E7A_*.png` because deleting them was blocked. The copies in `R3E7A_shots/` are the review set. Docs source art was not deleted.

## 11. Kết quả

| Area | Result |
| --- | --- |
| Brom base sprites, clips, anchors | PASS |
| Sandbox preview | PASS, VISUAL ONLY |
| Portrait and landscape | PASS |
| F1 Aren, F5 Vex, F2 stays empty | PASS |
| Campaign Level 1 | PASS |
| Levels 2–4, Boss | NOT TESTED |
| Brom skills, Lyra, Selene | NOT IMPLEMENTED |

Shots: `Docs/ReferenceRebuild/R3/R3E7A_shots/` — idle, walk, attack and hurt in Down, Up and Side, death down, and one landscape frame.

Stopped for review.
