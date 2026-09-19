# Rebuilds the apron scene and prefabs in a mirror, then copies the results back.
#
# The editor holds a lock on the real project, so the menu item cannot be run there from a
# script. This mirrors, runs SceneBootstrap.Rebuild in batch mode, and copies back the apron
# scene and everything the run writes under Assets\Content and Assets\UI: the prefabs, and the
# profile assets ContentBootstrap creates for equipment the project has none for yet. Menu.unity is authored by hand and is left
# alone; tools\layout-menu-scene.ps1 is what writes that one.

param([string]$MirrorName = "testproj3")

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "mirror.ps1")

$Paths = Get-MirrorPaths $MirrorName
Sync-Mirror $Paths

$log = Join-Path $Paths.Results "rebuild.log"
if (Test-Path $log) { Remove-Item $log -Force }

"=== rebuild ==="
$code = Invoke-Unity @(
    "-batchmode", "-quit", "-nographics",
    "-projectPath", $Paths.Mirror,
    "-executeMethod", "BelowTheWing.EditorTools.SceneBootstrap.Rebuild",
    "-logFile", $log
)

if ($code -ne 0) {
    "  rebuild failed."
    Explain-Failure $log $Paths $code | Out-Host
    throw "rebuild failed"
}

foreach ($folder in @("Assets\Content", "Assets\UI")) {
    if (Test-Path "$($Paths.Mirror)\$folder") {
        robocopy "$($Paths.Mirror)\$folder" "$($Paths.Source)\$folder" /MIR /NJH /NJS /NFL /NDL /R:2 /W:1 | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "copying $folder back failed (exit $LASTEXITCODE)" }
        "  copied back $folder"
    }
}

foreach ($file in @("Assets\Scenes\Apron.unity", "Assets\Scenes\Apron.unity.meta")) {
    if (Test-Path (Join-Path $Paths.Mirror $file)) {
        Copy-Item (Join-Path $Paths.Mirror $file) (Join-Path $Paths.Source $file) -Force
        "  copied back $file"
    }
}

Copy-NewScriptMetas $Paths
Copy-BuildSettings $Paths

"rebuilt"
