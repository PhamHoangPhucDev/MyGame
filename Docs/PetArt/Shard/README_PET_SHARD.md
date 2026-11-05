# Pet Egg Shard: art spec

Art only. No Unity code, scene, prefab or import setting was changed.
Goes with `Docs/Pets/PET_SYSTEM_SPEC_CURSOR.md` §4 (sacrifice → shard, 10 shards → 1 Mystery Pet Egg).
Style: a broken piece of the Mystery Pet Egg (`Docs/PetArt/Egg`): cream shell, violet spot cut in half, 1 px ink outline,
1 px shell thickness on the broken edge, light from the top-left. No anti-aliasing, no semi-transparent pixels.

## Files

| File | Size | Use |
|---|---|---|
| `UI_Item_PetEggShard.png` | 16x16 | BAG icon for `pet_egg_shard`. Draw at (3,3) in the 22x22 `UI_Inventory_Slot`, like the egg icon |
| `UI_PetEggShard_32.png` | 32x32 | Large icon: EGG SHARDS area in the Pet board and the sacrifice confirm popup |
| `Pet_Sacrifice_FX_Sheet.png` | 288x48 (6 cells of 48x48) | Sacrifice effect, pivot bottom-centre (24,0) |
| `Pet_Shard_Combine_Sheet.png` | 512x64 (8 cells of 64x64) | Combine effect, pivot centre (32,32) |
| `Shard_Sample_Board.png`, `Shard_FX_Review_Board.png` | n/a | Review only, not for Unity |

## Sacrifice sheet (48x48)

Play it on the pet (UI preview or world body), feet on the bottom row.

| # | What | Pet | Suggested time |
|---|---|---|---|
| 0 | Gold ring on the ground, first motes | 100 % | 0.10 s |
| 1 | Violet outer ring, motes rise | fade to 60 % | 0.10 s |
| 2 | Light column | fade to 25 % | 0.12 s |
| 3 | Light orb, rays | hidden | 0.15 s |
| 4 | Orb shrinks | hidden | 0.10 s |
| 5 | The shard is left, sparkles | hidden | hold 0.4 s, then "+1 Egg Shard" banner |

The pet fade is done in code (sprite alpha). The sheet itself is opaque pixels only.

## Combine sheet (64x64)

| # | What | Suggested time |
|---|---|---|
| 0–4 | Ten shards spiral in; from cell 3 half of them are already inside the light | 0.08 s each |
| 5 | Flash with 12 rays | 0.12 s |
| 6 | Egg appears with rays | 0.15 s |
| 7 | Egg with sparkles | hold, then add the egg to the bag (or start the existing hatch sheet) |

## Import notes for Unity (Cursor side)

Point (no filter), compression None, sprite mode Multiple sliced by the cell size above, pivots as listed.
Same Pixels Per Unit as the other UI kit sprites.

## Rebuild

```powershell
Add-Type -Path "Docs\UI\tools\UiCore.cs","Docs\PetArt\tools\PetEgg.cs","Docs\PetArt\tools\PetShard.cs" -ReferencedAssemblies System.Drawing,System.Core
[Img]::Root = (Get-Location).Path; [PetShard]::Run((Join-Path (Get-Location).Path "Docs\PetArt\Shard"))
```
