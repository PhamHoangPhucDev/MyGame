# Pet attack animations: art spec

Art only. No Unity code, scene, prefab or import setting was changed.
Goes with `Docs/Pets/PET_SYSTEM_SPEC_CURSOR.md` §3 (combat support). Owner approved: the pet itself rushes in and bites / claws / breathes fire in the world (2026-11-03).
Frames are built from the approved species sheets (`Docs/PetArt/Species`) with whole-pixel moves only (shift, drop or repeat one body row). Same palette, same outline, no anti-aliasing, no semi-transparent pixels.

## Files

| File | Size | Use |
|---|---|---|
| `Pet_<Species>_Attack.png` (Dog, Cat, Fox, Rabbit, Wolf, BabyDragon) | 128x72 (4 x 3 cells of 32x24) | Attack body frames |
| `Pet_Hit_FX_Sheet.png` | 48x64 (3 x 4 cells of 16x16) | Shared hit effects |
| `Pet_Dragon_Fire_Sheet.png` | 120x24 (5 cells of 24x24) | Fireball + impact |
| `Attack_Review_Board.png`, `Attack_Sample_Board.png` | n/a | Review only, not for Unity |

## Attack sheet grid (32x24 cells, pivot bottom-centre = (16, 0))

The 24x20 body sits at (4,4) inside the cell, so the feet are on the bottom row exactly like the walk sheet and the pet does not jump when the code swaps sheets.

| Row | View | Same as walk sheet row |
|---|---|---|
| 0 | Down (front) | 0 |
| 1 | Up (back) | 1 |
| 2 | Side, faces right (`flipX` for left) | 2 |

| Col | Beat | Suggested time |
|---|---|---|
| 0 | Wind-up (crouch back) | 0.10 s |
| 1 | Lunge / leap / inhale | 0.08 s |
| 2 | Hit: apply damage on this frame, spawn the hit FX | 0.10 s |
| 3 | Recover | 0.12 s, then back to walk/idle |

Total ≈ 0.4 s, shorter than every `AtkInterval` in the spec (0.7–1.5 s).

## Attack style per species

| Species | Style | Hit FX row | Spawn point |
|---|---|---|---|
| Dog | Lunge + bite | 0 Bite | at the target |
| Cat | Pounce + claw | 1 Claw | at the target |
| Fox | Pounce + claw | 1 Claw | at the target |
| Wolf | Lunge + bite, then ice claw | 0 Bite on col 2, 2 Ice claw on col 3 | at the target |
| Rabbit | Hop and stomp (col 1 is in the air) | 3 Stomp | under the target's feet |
| Baby Dragon | Rear back, ember in the mouth (col 1), breath (col 2) | Fire sheet | fireball leaves the mouth on col 2 and flies to the target (range 2.5) |

## Hit FX sheet (16x16, pivot centre)

Rows: 0 bite (jaws open → snap), 1 claw (gold slashes), 2 ice claw (wolf, blue), 3 stomp (dust ring). 3 frames per row, 0.06 s each, then remove.
Claw slashes face down-right. Flip X when the pet faces left.

## Dragon fire sheet (24x24, pivot centre)

Cells 0–1: fireball loop (faces right, flip for left), 0.08 s each, move it in code. Cells 2–4: impact, 0.07 s each at the target, then remove.

## Import notes for Unity (Cursor side)

Sprite, PPU 16 (same as the species sheets), Point, compression None, no mipmaps. Multiple mode sliced by the cell size above.

## Rebuild

```powershell
Add-Type -Path "Docs\UI\tools\UiCore.cs","Docs\PetArt\tools\PetAttack.cs" -ReferencedAssemblies System.Drawing,System.Core
[Img]::Root = (Get-Location).Path; [PetAttack]::Run((Join-Path (Get-Location).Path "Docs\PetArt\Attack"), $false)
```
