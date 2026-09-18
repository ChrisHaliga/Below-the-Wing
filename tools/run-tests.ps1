# Runs the Below the Wing test suites against a mirror of the project.
#
# The editor normally has the real project open, and Unity refuses to run a second instance
# against a locked project. So this copies the three folders that actually define the project,
# Assets, Packages and ProjectSettings, into a sibling directory and runs there. The mirror keeps
# its own Library, so the first run pays for an asset import and later runs do not.
#
#   .\run-tests.ps1                     both suites
#   .\run-tests.ps1 -Platform EditMode
#   .\run-tests.ps1 -Platform Compile   compile and quit, about a minute
#   .\run-tests.ps1 -Filter BelowTheWing.Tests.Vehicles

param(
    # Compile mirrors the project and asks Unity to compile it and quit. It finishes in about a
    # minute and exits non-zero on a compile error, where a test run with a compile error in it
    # sits waiting for a test runner that never starts.
    [ValidateSet("EditMode", "PlayMode", "Both", "Compile", "None")]
    [string]$Platform = "Both",
    # Netcode is listed as a testable package so its multi-client harness compiles, and that also
    # brings its own ~7000 tests into every run: fifteen minutes, and a handful that fail for
    # reasons of their own in a mirrored project. Ours are the ones under test here.
    [string]$Filter = "BelowTheWing",
    [switch]$SkipMirror,

    # Which copy of the project to run against. Unity takes an exclusive lock on a project, so two
    # runs against one mirror deadlock and both die with no results file. Separate mirrors let the
    # slow play mode suite run while the fast edit mode one is being iterated on.
    [string]$MirrorName = "testproj",

    # Generates any missing content assets in the mirror and copies them back into the real
    # project. The editor normally has the real project locked, so this is how an asset gets
    # authored without clicking anything.
    [switch]$Bootstrap
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "mirror.ps1")

$Paths = Get-MirrorPaths $MirrorName

if (-not $SkipMirror) {
    Sync-Mirror $Paths
}

if ($Bootstrap) {
    $log = Join-Path $Paths.Results "bootstrap.log"
    if (Test-Path $log) { Remove-Item $log -Force }
    "=== bootstrap content ==="
    $code = Invoke-Unity @(
        "-batchmode", "-quit", "-nographics",
        "-projectPath", $Paths.Mirror,
        "-executeMethod", "BelowTheWing.EditorTools.ContentBootstrap.CreateMissingContent",
        "-logFile", $log
    )
    if ($code -ne 0) {
        "  bootstrap failed (exit $code). Tail of the editor log:"
        if (Test-Path $log) { Get-Content $log -Tail 40 | ForEach-Object { "    $_" } }
        throw "bootstrap failed"
    }

    if (Test-Path "$($Paths.Mirror)\Assets\Content") {
        robocopy "$($Paths.Mirror)\Assets\Content" "$($Paths.Source)\Assets\Content" /E /NJH /NJS /NFL /NDL /R:2 /W:1 | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "copying generated content back failed (exit $LASTEXITCODE)" }
        "  generated content copied back into the project"
        Get-ChildItem "$($Paths.Source)\Assets\Content" -Recurse -Filter *.asset | ForEach-Object { "    $($_.FullName.Replace($Paths.Source, ''))" }
    }
}

if ($Platform -eq "None") { exit 0 }

if ($Platform -eq "Compile") {
    $log = Join-Path $Paths.Results "compile.log"
    if (Test-Path $log) { Remove-Item $log -Force }

    "=== compile ==="
    $code = Invoke-Unity @("-batchmode", "-quit", "-nographics", "-projectPath", $Paths.Mirror, "-logFile", $log)

    if ($code -ne 0 -or (Get-ProjectCompileErrors $log).Count -gt 0) {
        "  FAILED"
        Explain-Failure $log $Paths $code | Out-Host
        exit 1
    }

    "  compiled clean"
    exit 0
}

$platforms = if ($Platform -eq "Both") { @("EditMode", "PlayMode") } else { @($Platform) }
$failed = $false

foreach ($p in $platforms) {
    $xml = Join-Path $Paths.Results "$p.xml"
    $log = Join-Path $Paths.Results "$p.log"
    foreach ($f in @($xml, $log)) { if (Test-Path $f) { Remove-Item $f -Force } }

    $unityArgs = @(
        "-runTests", "-batchmode", "-nographics",
        "-projectPath", $Paths.Mirror,
        "-testPlatform", $p,
        "-testResults", $xml,
        "-logFile", $log
    )
    if ($Filter) { $unityArgs += @("-testFilter", $Filter) }

    "=== $p ==="
    $code = Invoke-Unity $unityArgs

    if (-not (Test-Path $xml)) {
        "  NO RESULTS FILE."
        Explain-Failure $log $Paths $code | Out-Host
        $failed = $true
        continue
    }

    [xml]$r = Get-Content $xml
    $run = $r.'test-run'
    "  total {0}  passed {1}  failed {2}  skipped {3}  ({4}s)" -f `
        $run.total, $run.passed, $run.failed, $run.skipped, $run.duration

    $cases = $r.SelectNodes("//test-case[@result='Failed']")
    foreach ($c in $cases) {
        "  FAIL  $($c.fullname)"
        $msg = $c.SelectSingleNode("failure/message")
        if ($msg) {
            ($msg.InnerText -split "`n" | Select-Object -First 4) | ForEach-Object { "          $($_.Trim())" }
        }
    }
    if ([int]$run.failed -gt 0) { $failed = $true }
}

if ($failed) {
    "`nSUITE RED"
    exit 1
}

"`nSUITE GREEN"
exit 0
