# Rebuilds the dungeon stage-select art into ..\export. Run from any folder:
#   powershell -ExecutionPolicy Bypass -File Docs\ReferenceRebuild\DungeonSelect\tools\run.ps1
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $here '..\..\..\..')
$out = Join-Path $here '..\export'
New-Item -ItemType Directory -Force $out | Out-Null
$out = (Resolve-Path $out).Path
$wm = Join-Path $root 'Assets\Art\UI\Game01\WorldMap'

Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition (Get-Content -Raw (Join-Path $here 'DungeonBoardArt.cs')) -ReferencedAssemblies System.Drawing

[DungeonBoardArt]::Board((Join-Path $out 'UI_Dungeon_Board_GoblinCamp.png'))
[DungeonBoardArt]::Fog($out, 'UI_Dungeon_Fog_GoblinCamp_Stage')
[DungeonBoardArt]::BossPins($out, (Join-Path $wm 'UI_WorldMap_Pin_Locked.png'), (Join-Path $wm 'UI_WorldMap_Pin_Unlocked.png'))
[DungeonBoardArt]::MapTypeIcons($out)
Get-ChildItem $out -Filter *.png | ForEach-Object { $_.Name }
