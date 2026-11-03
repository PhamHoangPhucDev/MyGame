# Pet species sprites: art spec

Art only. No Unity code, scene, prefab or import setting was changed.
Species list approved by the owner on 2026-11-03: Dog, Cat, Fox, Rabbit, Wolf, Baby Dragon.
Owner direction for the redesign (same day): Dragon can fly and must look imposing; Wolf fierce and cold; Cat cute, haughty and lovable.
These replace the recoloured farm dog (`FM_Dog`) placeholders.

## Files

| File | Size | Use |
|---|---|---|
| `Pet_Dog.png` | 144x60 | Beagle (white, black saddle, tan head, floppy ears). Kept different from the shiba farm dog |
| `Pet_Cat.png` | 144x60 | Cute and haughty: silver tabby, big head, big blue eyes, pink cheeks, tail held high with a hook. Sit col 4 chin up with eyes shut, col 5 one eye peeks |
| `Pet_Fox.png` | 144x60 | Orange, white chest and tail tip, dark socks and ear tips |
| `Pet_Rabbit.png` | 144x60 | Light brown, white belly and cotton tail, long ears |
| `Pet_Wolf.png` | 144x60 | Fierce and cold: dark slate fur, icy mask, glowing ice-blue eyes under hard brows, raised hackles, fangs. Walks with the head low (stalking). Sit col 4 cold stare, col 5 snarl |
| `Pet_BabyDragon.png` | 144x60 | Flies, imposing: obsidian-indigo scales, gold belly and ridge, crimson wings, swept ivory horns, ember eyes, spade tail. Walk cols are a flight loop; sit cols are a landed pose |
| `Pet_<Species>_Portrait.png` | 24x24 | PETS list icon (front sit pose, feet on the bottom row) |
| `Pet_Species_Review_Board.png` | n/a | Review only, not for Unity |

## Grid (same as `FM_Dog`)

6 columns x 3 rows, cells 24x20, rows counted from the top of the image:

| Row | View | Cols 0-3 | Cols 4-5 |
|---|---|---|---|
| 0 | Down (front) | Walk | Sit (col 5: tail wag / ear twitch / wing flap) |
| 1 | Up (back) | Walk | Sit |
| 2 | Side, faces right (set `flipX` for left) | Walk | Sit |

Timing (same as the farm dog): walk 8 fps, sit 4 fps.
Rabbit walk is a hop: col 0 crouch, 1 push, 2 in the air, 3 landing. In cols 1-3 the feet leave the ground on purpose.
Baby Dragon flies: cols 0-3 are a wing-beat loop (up, mid, down, mid) and the body never touches the ground; it rises 1 px on the down-beat. Play it at 8 fps for all movement. Cols 4-5 are a landed pose (col 4 wings half-folded, col 5 wings flared with an ember breath); use them when the pet stops. A flying pet needs a ground shadow under its pivot: the sheet has none, so use the game's existing blob shadow or ask for one. The dragon portrait uses the flared pose.

So `FarmArt.Grid("<file>", 6, 3, 24, 20, Feet)` slices these sheets exactly like the dog.

## Import (Cursor side)
Sprite, PPU 16, Point, uncompressed, no mipmaps, pivot Bottom-Centre for sheets. Portraits: pivot Center, same PPU as the UI kit icons.

## Fixes against the farm dog grid
- Front/back heads now sit lower, so ear tips no longer go past the top of the cell.
- Front/back sit frames now stand on the bottom row (the farm dog's sit floats 1 px).

## Rebuild

```powershell
Add-Type -Path "Docs\UI\tools\UiCore.cs","Docs\PetArt\tools\PetSpecies.cs" -ReferencedAssemblies System.Drawing,System.Core
[Img]::Root = (Get-Location).Path; [PetSpecies]::Run((Join-Path (Get-Location).Path "Docs\PetArt\Species"))
```
