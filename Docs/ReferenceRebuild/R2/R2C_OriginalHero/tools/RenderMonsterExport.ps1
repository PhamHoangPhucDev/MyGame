# Monster sets -> ..\unity_export_monsters\ (sheets + MONSTERS_MANIFEST.csv + MONSTERS_FRAMES.csv)
$D = Split-Path $PSScriptRoot -Parent
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\MonsterGen.cs", "$D\tools\RiderGen.cs", "$D\tools\MonsterAnim.cs") -ReferencedAssemblies System.Drawing, System.Core
[MonsterAnim]::Export((Join-Path $D "unity_export_monsters")); "ok"
