# Status icons (approved v6): export (..\icons\status\) + pixel checks
$D = Split-Path $PSScriptRoot -Parent
$Exp = Join-Path $D "icons\status"; New-Item -ItemType Directory -Force $Exp | Out-Null
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\ArenKitSheets.cs", "$D\tools\StatusIconGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$n = [StatusIconGen]::Ids.Length
$atlas = New-Object System.Drawing.Bitmap ($n * 24), 24
$csv = New-Object System.Text.StringBuilder; [void]$csv.AppendLine("file,id,name_vi,type,source,atlas_col,size")
$bad = 0; $semi = 0
for ($i = 0; $i -lt $n; $i++) {
    $ic = [StatusIconGen]::Icon($i)
    $id = [StatusIconGen]::Ids[$i]; $file = "Status_$id.png"
    $ic.Save((Join-Path $Exp $file))
    for ($y = 0; $y -lt 24; $y++) { for ($x = 0; $x -lt 24; $x++) {
        $c = $ic.GetPixel($x, $y)
        if ($c.A -gt 0) { $atlas.SetPixel($i * 24 + $x, $y, $c); if ($x -eq 0 -or $y -eq 0 -or $x -eq 23 -or $y -eq 23) { $bad++ }; if ($c.A -lt 255) { $semi++ } }
    } }
    $type = if ([StatusIconGen]::Debuff[$i]) { "debuff" } else { "buff" }
    [void]$csv.AppendLine(('{0},{1},{2},{3},"{4}",{5},24' -f $file, $id, [StatusIconGen]::NamesVi[$i], $type, [StatusIconGen]::Source[$i], $i))
}
$atlas.Save((Join-Path $Exp "Status_Atlas.png"))
[System.IO.File]::WriteAllText((Join-Path $Exp "STATUS_MANIFEST.csv"), $csv.ToString(), (New-Object System.Text.UTF8Encoding $false))
"exported $n icons; edgePixels=$bad semiAlpha=$semi"
