# Lays out Assets/Scenes/Menu.unity from scratch and copies it back into the project.
#
# Menu.unity is normally authored by hand. This throws that away and writes a fresh one from
# SceneBootstrap.LayOutTheMenuScene: the apron dressing, the three camera stations, the crew line,
# the belt loader, the parked jet, and the UI document the menu is drawn into. Anything moved in
# the editor since the last run is lost.
#
# Run it to start the menu scene over, or after a change to what the menu needs wired into it.
# Day to day, open Menu.unity and move things instead.
#
# The editor holds a lock on the real project, so this mirrors and runs Unity there in batch mode.

param([string]$MirrorName = "testproj3")

$ErrorActionPreference = "Stop"

$Unity   = if ($env:BTW_UNITY) { $env:BTW_UNITY }
           else { "C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Unity.exe" }
$Source  = Split-Path -Parent $PSScriptRoot
$Mirror  = Join-Path ([IO.Path]::GetTempPath()) "below-the-wing\$MirrorName"
$Results = Join-Path ([IO.Path]::GetTempPath()) "below-the-wing\results-$MirrorName"

New-Item -ItemType Directory -Force -Path $Mirror, $Results | Out-Null

foreach ($folder in @("Assets", "Packages", "ProjectSettings")) {
    robocopy "$Source\$folder" "$Mirror\$folder" /MIR /NJH /NJS /NFL /NDL /R:2 /W:1 | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed for $folder (exit $LASTEXITCODE)" }
}
"mirrored Assets, Packages, ProjectSettings"

$log = Join-Path $Results "menu-scene.log"

$unityArgs = @(
    "-batchmode", "-quit", "-nographics",
    "-projectPath", $Mirror,
    "-executeMethod", "BelowTheWing.EditorTools.SceneBootstrap.LayOutTheMenuScene",
    "-logFile", $log
)

"=== lay out the menu scene ==="
$proc = Start-Process -FilePath $Unity -ArgumentList $unityArgs -Wait -PassThru -NoNewWindow

if ($proc.ExitCode -ne 0) {
    "  failed (exit $($proc.ExitCode))."
    if (Test-Path $log) {
        Get-Content $log | Select-String -Pattern 'error CS|Exception|ArgumentNull|MissingReference' |
            Select-Object -First 25 | ForEach-Object { "    $_" }
        "  --- tail ---"
        Get-Content $log -Tail 25 | ForEach-Object { "    $_" }
    }
    throw "laying out the menu scene failed"
}

foreach ($file in @("Assets\Scenes\Menu.unity", "Assets\Scenes\Menu.unity.meta")) {
    if (Test-Path (Join-Path $Mirror $file)) {
        Copy-Item (Join-Path $Mirror $file) (Join-Path $Source $file) -Force
        "  copied back $file"
    }
}

# A script imported for the first time in the mirror gets its guid there, and the scene just written
# records it. Importing the same script elsewhere would assign a different one, and every component
# the scene placed would read as a missing script with nothing logged.
$adopted = 0
foreach ($script in Get-ChildItem "$Source\Assets" -Recurse -Filter *.cs) {
    $meta = "$($script.FullName).meta"
    if (Test-Path $meta) { continue }

    $fromMirror = $meta.Replace($Source, $Mirror)
    if (Test-Path $fromMirror) {
        Copy-Item $fromMirror $meta -Force
        $adopted++
    }
}
if ($adopted -gt 0) { "  copied back $adopted script .meta files the mirror generated" }

# Which scenes are in the build, and in what order, lives outside Assets.
$buildSettings = "ProjectSettings\EditorBuildSettings.asset"
if (Test-Path (Join-Path $Mirror $buildSettings)) {
    Copy-Item (Join-Path $Mirror $buildSettings) (Join-Path $Source $buildSettings) -Force
    "  copied back $buildSettings"
}

"menu scene laid out"
