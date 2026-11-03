# Mystery Pet Egg: art spec

Art only. No Unity code, scene, prefab or import setting was changed.
Style: UI kit palette (`Docs/UI/tools/UiCore.cs`): cream shell, violet spots, 1 px ink outline, light from the top-left, no anti-aliasing, no semi-transparent pixels.

## Files

| File | Size | Use |
|---|---|---|
| `UI_Item_MysteryPetEgg.png` | 16x16 | BAG item icon for `mystery_pet_egg`. Draw at (3,3) in the 22x22 `UI_Inventory_Slot`, like other item icons |
| `Pet_Egg_Hatch_Sheet.png` | 336x64 (7 cells of 48x64) | Hatch screen egg animation, one row, left to right |
| `frames/Pet_Egg_<Name>.png` | 48x64 each | Same 7 frames as single files |
| `Egg_Review_Board.png` | n/a | Review only, not for Unity |

## Hatch sheet cells (48x64, pivot bottom-centre = (24, 0) from the bottom)

The egg stands on the bottom row of every cell. The top 16 px are headroom for the flying cap and the pet pop.

| # | Name | Suggested use |
|---|---|---|
| 0 | Idle | Resting egg |
| 1 | TiltL | Wobble, tipped 8° left |
| 2 | TiltR | Wobble, tipped 8° right |
| 3 | Crack1 | First crack at the top |
| 4 | Crack2 | Cracks spread, light leaks out |
| 5 | Burst | Cap flies off, light rays, shell chips |
| 6 | Open | Bottom shell left behind, sparkles. Show the pet in front of this frame |

Suggested sequence: Idle → (TiltL, Idle, TiltR, Idle) ×2 at ~0.08 s per frame → Crack1 0.4 s → wobble once → Crack2 0.4 s → Burst 0.15 s → Open (hold while the pet appears).

## Import notes for Unity (Cursor side)
Point (no filter), compression None, sprite mode Multiple sliced by 48x64 cells, pivot Bottom Center. Use the same Pixels Per Unit as the other UI kit sprites.

## Rebuild

```powershell
Add-Type -Path "Docs\UI\tools\UiCore.cs","Docs\PetArt\tools\PetEgg.cs" -ReferencedAssemblies System.Drawing,System.Core
[Img]::Root = (Get-Location).Path; [PetEgg]::Run((Join-Path (Get-Location).Path "Docs\PetArt\Egg"))
```
