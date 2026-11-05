# GAME01 UI kit renderer. Usage: powershell -NoProfile -File RenderUiKit.ps1 [-Only parts|all]
param([string]$Only = "all")
$ErrorActionPreference = "Stop"
$Tools = $PSScriptRoot
$UiDir = Split-Path $Tools -Parent
$Root = Split-Path (Split-Path $UiDir -Parent) -Parent
$src = @("$Tools\UiCore.cs", "$Tools\UiParts.cs", "$Tools\UiIcons.cs")
if ($Only -eq "all") { $src += @("$Tools\UiScene.cs", "$Tools\UiScreens.cs", "$Tools\UiBoards.cs", "$Tools\UiRun.cs") } else { $src += "$Tools\UiPreview.cs" }
Add-Type -Path $src -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
[Img]::Root = $Root
[Txt]::FontPath = Join-Path $Root "Assets\Art\Fonts\PressStart2P-Regular.ttf"
if ($Only -eq "all") { [UiRun]::Run($UiDir) } else { [UiPreview]::Run($UiDir) }
"done"
