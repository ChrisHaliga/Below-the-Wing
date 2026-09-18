# Shared by run-tests.ps1, rebuild-scene.ps1 and layout-menu-scene.ps1. Dot-source it.
#
# The editor holds an exclusive lock on the real project, so every script here copies the three
# folders that define the project into a mirror under the system temp folder and runs a headless
# Unity there. The mirror keeps its own Library, so only the first run pays for an import.

function Get-UnityPath {
    if ($env:BTW_UNITY) { return $env:BTW_UNITY }
    return "C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Unity.exe"
}

function Get-MirrorPaths([string]$MirrorName) {
    $source = Split-Path -Parent $PSScriptRoot
    return @{
        Source  = $source
        Mirror  = Join-Path ([IO.Path]::GetTempPath()) "below-the-wing\$MirrorName"
        Results = Join-Path ([IO.Path]::GetTempPath()) "below-the-wing\results-$MirrorName"
    }
}

function Sync-Mirror([hashtable]$Paths) {
    New-Item -ItemType Directory -Force -Path $Paths.Mirror, $Paths.Results | Out-Null

    foreach ($folder in @("Assets", "Packages", "ProjectSettings")) {
        # /MIR so deletions in the real project propagate; /NJH /NJS /NFL /NDL to keep it quiet.
        robocopy "$($Paths.Source)\$folder" "$($Paths.Mirror)\$folder" /MIR /NJH /NJS /NFL /NDL /R:2 /W:1 | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "robocopy failed for $folder (exit $LASTEXITCODE)" }
    }
    "mirrored Assets, Packages, ProjectSettings"

    Repair-PackageCache $Paths
}

# A Unity killed or crashed while extracting packages leaves Library\PackageCache holding a package
# with its source files but none of its .asmdef files. Later runs trust the folder as extracted,
# every source in it is ignored as being in an immutable folder with no assembly definition, and
# everything that references that assembly fails with types not found while Assets shows no error.
# Deleting the cache makes the next run extract again, from the editor's own copy for a built-in
# package and from the machine-wide cache for the rest.
function Repair-PackageCache([hashtable]$Paths) {
    $cache = Join-Path $Paths.Mirror "Library\PackageCache"
    if (-not (Test-Path $cache)) { return }

    $broken = Get-ChildItem $cache -Directory | Where-Object {
        (Get-ChildItem $_.FullName -Recurse -Filter *.cs -File | Select-Object -First 1) -and
        -not (Get-ChildItem $_.FullName -Recurse -Filter *.asmdef -File | Select-Object -First 1)
    }

    if ($broken) {
        "  package cache is partial ($(($broken | ForEach-Object { $_.Name -replace '@.*','' }) -join ', ') have sources but no asmdef); clearing it"
        Remove-Item $cache -Recurse -Force
    }
}

# Waits on the Unity process itself. Start-Process -Wait also waits for every descendant, and a
# Unity that had to spawn its own Unity.Licensing.Client leaves that child running after it exits,
# so -Wait never returns.
function Invoke-Unity([string[]]$Arguments) {
    $proc = Start-Process -FilePath (Get-UnityPath) -ArgumentList $Arguments -PassThru -NoNewWindow
    $proc.WaitForExit()
    return $proc.ExitCode
}

# Compile errors in the project, as distinct sorted lines, or an empty list.
function Get-ProjectCompileErrors([string]$Log) {
    if (-not (Test-Path $Log)) { return @() }
    return @(Select-String -Path $Log -Pattern 'error CS' -SimpleMatch |
        Where-Object { $_.Line -match 'Assets[\\/]' } |
        ForEach-Object { $_.Line -replace '^.*?Assets', 'Assets' } |
        Sort-Object -Unique)
}

function Get-PackageCompileErrorCount([string]$Log) {
    if (-not (Test-Path $Log)) { return 0 }
    return @(Select-String -Path $Log -Pattern 'error CS' -SimpleMatch | Where-Object { $_.Line -match 'PackageCache' }).Count
}

# Prints why a Unity run failed. Compile errors in the project are the code's fault and are
# listed. Compile errors only in Library\PackageCache, with none in Assets, are the mirror's fault:
# a build graph or package cache left half-written by a killed or crashed Unity, which later runs
# trust. The one cure that has worked every time is a fresh Library, so this deletes it and says
# so; the next run pays for one import and is then healthy.
function Explain-Failure([string]$Log, [hashtable]$Paths, [int]$ExitCode) {
    $ours = Get-ProjectCompileErrors $Log
    if ($ours.Count -gt 0) {
        "  $($ours.Count) distinct compile errors in the project:"
        $ours | Select-Object -First 40 | ForEach-Object { "    $_" }
        return
    }

    $packages = Get-PackageCompileErrorCount $Log
    if ($packages -gt 0) {
        "  Unity exit $ExitCode with no errors in Assets and $packages in Library\PackageCache."
        "  That is the mirror's Library, not the code. Deleting $($Paths.Mirror)\Library; run again."
        Remove-Item (Join-Path $Paths.Mirror "Library") -Recurse -Force -ErrorAction SilentlyContinue
        return
    }

    "  Unity exit $ExitCode. Tail of the log:"
    if (Test-Path $Log) {
        Get-Content $Log | Select-String -Pattern 'Exception|ArgumentNull|MissingReference' |
            Select-Object -First 25 | ForEach-Object { "    $_" }
        Get-Content $Log -Tail 25 | ForEach-Object { "    $_" }
    }
}

# A script imported for the first time in the mirror gets its guid there, and a scene written in
# that run records it. Importing the same script elsewhere assigns a different guid, and every
# component the scene placed then reads as a missing script with nothing logged. Taking the mirror's
# metas for scripts that had none keeps the one guid.
function Copy-NewScriptMetas([hashtable]$Paths) {
    $adopted = 0
    foreach ($script in Get-ChildItem "$($Paths.Source)\Assets" -Recurse -Filter *.cs) {
        $meta = "$($script.FullName).meta"
        if (Test-Path $meta) { continue }

        $fromMirror = $meta.Replace($Paths.Source, $Paths.Mirror)
        if (Test-Path $fromMirror) {
            Copy-Item $fromMirror $meta -Force
            $adopted++
        }
    }
    if ($adopted -gt 0) { "  copied back $adopted script .meta files the mirror generated" }
}

# Which scenes are in the build, and in what order, lives outside Assets.
function Copy-BuildSettings([hashtable]$Paths) {
    $buildSettings = "ProjectSettings\EditorBuildSettings.asset"
    if (Test-Path (Join-Path $Paths.Mirror $buildSettings)) {
        Copy-Item (Join-Path $Paths.Mirror $buildSettings) (Join-Path $Paths.Source $buildSettings) -Force
        "  copied back $buildSettings"
    }
}
