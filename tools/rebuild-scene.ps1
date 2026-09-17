# Rebuilds the apron scene and prefabs in a mirror, then copies the results back.
#
# The editor holds a lock on the real project, so the menu item cannot be run there from a
# script. This mirrors, runs SceneBootstrap.Rebuild in batch mode, and copies back the
# scene, the prefabs and the UI assets it generates.

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

$log = Join-Path $Results "rebuild.log"
Remove-Item $log -ErrorAction SilentlyContinue

$unityArgs = @(
    "-batchmode", "-quit", "-nographics",
    "-projectPath", $Mirror,
    "-executeMethod", "BelowTheWing.EditorTools.SceneBootstrap.Rebuild",
    "-logFile", $log
)

"=== rebuild ==="
$proc = Start-Process -FilePath $Unity -ArgumentList $unityArgs -Wait -PassThru -NoNewWindow

if ($proc.ExitCode -ne 0) {
    "  rebuild failed (exit $($proc.ExitCode))."
    if (Test-Path $log) {
        Get-Content $log | Select-String -Pattern 'error CS|Exception|ArgumentNull|MissingReference' |
            Select-Object -First 25 | ForEach-Object { "    $_" }
        "  --- tail ---"
        Get-Content $log -Tail 25 | ForEach-Object { "    $_" }
    }
    throw "rebuild failed"
}

foreach ($folder in @("Assets\Scenes", "Assets\Content\Prefabs", "Assets\UI")) {
    if (Test-Path "$Mirror\$folder") {
        robocopy "$Mirror\$folder" "$Source\$folder" /MIR /NJH /NJS /NFL /NDL /R:2 /W:1 | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "copying $folder back failed (exit $LASTEXITCODE)" }
        "  copied back $folder"
    }
}

"rebuilt"
