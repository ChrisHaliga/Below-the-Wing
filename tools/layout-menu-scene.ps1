# Lays out Assets/Scenes/Menu.unity from scratch and copies it back into the project.
#
# Menu.unity is normally authored by hand. This throws that away and writes a fresh one from
# SceneBootstrap.LayOutTheMenuScene: the apron dressing, the camera stations, the crew line, the
# belt loader, the parked jet, and the UI document the menu is drawn into. Anything moved in the
# editor since the last run is lost.
#
# Run it to start the menu scene over, or after a change to what the menu needs wired into it.
# Day to day, open Menu.unity and move things instead.

param([string]$MirrorName = "testproj3")

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "mirror.ps1")

$Paths = Get-MirrorPaths $MirrorName
Sync-Mirror $Paths

$log = Join-Path $Paths.Results "menu-scene.log"
if (Test-Path $log) { Remove-Item $log -Force }

"=== lay out the menu scene ==="
$code = Invoke-Unity @(
    "-batchmode", "-quit", "-nographics",
    "-projectPath", $Paths.Mirror,
    "-executeMethod", "BelowTheWing.EditorTools.SceneBootstrap.LayOutTheMenuScene",
    "-logFile", $log
)

if ($code -ne 0) {
    "  failed (exit $code)."
    $ours = Get-ProjectCompileErrors $log
    if ($ours.Count -gt 0) {
        $ours | Select-Object -First 25 | ForEach-Object { "    $_" }
    }
    elseif (Test-Path $log) {
        Get-Content $log | Select-String -Pattern 'Exception|ArgumentNull|MissingReference' |
            Select-Object -First 25 | ForEach-Object { "    $_" }
        "  --- tail ---"
        Get-Content $log -Tail 25 | ForEach-Object { "    $_" }
    }
    throw "laying out the menu scene failed"
}

foreach ($file in @("Assets\Scenes\Menu.unity", "Assets\Scenes\Menu.unity.meta")) {
    if (Test-Path (Join-Path $Paths.Mirror $file)) {
        Copy-Item (Join-Path $Paths.Mirror $file) (Join-Path $Paths.Source $file) -Force
        "  copied back $file"
    }
}

Copy-NewScriptMetas $Paths
Copy-BuildSettings $Paths

"menu scene laid out"
