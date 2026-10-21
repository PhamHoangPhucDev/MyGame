# Weapons-on-back patch for Aren / Brom / Selene exports:
#  - rewrites only the Idle_* / Walk_* / Hurt_* rows of each *_Anchors.csv (Attack / Death rows untouched)
#  - appends the Aren hilt-peek cell (index 8) to Gear/Aren_Sword.png (384x48 -> 432x48)
# Run AFTER the class export scripts (RenderBrom.ps1 / RenderSelene.ps1 / Aren export), which would otherwise restore the old rows.
$D = Split-Path $PSScriptRoot -Parent
Add-Type -Path @("$D\tools\HeroConceptGen.cs", "$D\tools\ArenIdleGen.cs", "$D\tools\ArenWalkGen.cs", "$D\tools\EquipGen.cs", "$D\tools\FxGen.cs", "$D\tools\VfxConceptGen.cs", "$D\tools\ArenKitSheets.cs", "$D\tools\BromGen.cs", "$D\tools\BromKit.cs", "$D\tools\SeleneGen.cs", "$D\tools\SeleneKit.cs", "$D\tools\BackGearGen.cs") -ReferencedAssemblies System.Drawing, System.Core
Add-Type -AssemblyName System.Drawing
$targets = @(@("Aren", "unity_export\Aren_Anchors.csv"), @("Brom", "unity_export_brom\Brom_Anchors.csv"), @("Selene", "unity_export_selene\Selene_Anchors.csv"))
foreach ($t in $targets) {
    $path = Join-Path $D $t[1]
    $new = @{}; foreach ($row in [BackGearGen]::Rows($t[0])) { $new[$row.Split(',')[0]] = $row }
    $lines = [System.IO.File]::ReadAllLines($path); $changed = 0
    for ($i = 1; $i -lt $lines.Length; $i++) { $k = $lines[$i].Split(',')[0]; if ($new.ContainsKey($k)) { if ($lines[$i] -ne $new[$k]) { $changed++ }; $lines[$i] = $new[$k] } }
    [System.IO.File]::WriteAllText($path, (($lines -join "`n") + "`n"), (New-Object System.Text.UTF8Encoding $false))
    "{0}: {1} rows rewritten" -f $t[1], $changed
}
# Aren sword sheet: keep cells 0-7, add the hilt peek as cell 8 (pivot = cell centre = anchor)
$swPath = Join-Path $D "unity_export\Gear\Aren_Sword.png"
$old = [System.Drawing.Bitmap]::FromFile($swPath); $w = $old.Width
$sheet = New-Object System.Drawing.Bitmap 432, 48
for ($y = 0; $y -lt 48; $y++) { for ($x = 0; $x -lt [math]::Min($w, 384); $x++) { $c = $old.GetPixel($x, $y); if ($c.A -gt 0) { $sheet.SetPixel($x, $y, $c) } } }
$old.Dispose()
$peek = [BackGearGen]::ArenHiltPeek()
foreach ($kv in $peek.Px.GetEnumerator()) { $sheet.SetPixel(8 * 48 + 24 + $kv.Key.X, 24 + $kv.Key.Y, $kv.Value) }
$sheet.Save($swPath)
$bad = 0; for ($c = 0; $c -lt 9; $c++) { for ($i = 0; $i -lt 48; $i++) { foreach ($p in @(@(($c * 48 + $i), 0), @(($c * 48 + $i), 47), @(($c * 48), $i), @(($c * 48 + 47), $i))) { if ($sheet.GetPixel($p[0], $p[1]).A -gt 0) { $bad++ } } } }
"Aren_Sword.png 432x48 (9 cells) edgePixels=$bad"
$sheet.Dispose()
