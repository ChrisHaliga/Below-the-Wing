# Runs the Below the Wing test suites against a mirror of the project.
#
# The editor normally has the real project open, and Unity refuses to run a second
# instance against a locked project. So this copies the three folders that actually
# define the project -- Assets, Packages, ProjectSettings -- into a sibling directory
# and runs there. The mirror keeps its own Library, so the first run pays for an asset
# import and later runs do not.
#
#   .\run-tests.ps1                 both suites
#   .\run-tests.ps1 -Platform EditMode
#   .\run-tests.ps1 -Filter BelowTheWing.Tests.Vehicles

param(
    [ValidateSet("EditMode", "PlayMode", "Both", "None")]
    [string]$Platform = "Both",
    # Netcode is listed as a testable package so its multi-client harness compiles, and that also
    # brings its own ~7000 tests into every run -- fifteen minutes, and a handful that fail for
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

$Unity   = if ($env:BTW_UNITY) { $env:BTW_UNITY }
           else { "C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Unity.exe" }
$Source  = Split-Path -Parent $PSScriptRoot
$Mirror  = Join-Path ([IO.Path]::GetTempPath()) "below-the-wing\$MirrorName"
$Results = Join-Path ([IO.Path]::GetTempPath()) "below-the-wing\results-$MirrorName"

New-Item -ItemType Directory -Force -Path $Mirror, $Results | Out-Null

if (-not $SkipMirror) {
    foreach ($folder in @("Assets", "Packages", "ProjectSettings")) {
        # /MIR so deletions in the real project propagate; /NJH /NJS /NFL /NDL to keep it quiet.
        robocopy "$Source\$folder" "$Mirror\$folder" /MIR /NJH /NJS /NFL /NDL /R:2 /W:1 | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "robocopy failed for $folder (exit $LASTEXITCODE)" }
    }
    "mirrored Assets, Packages, ProjectSettings"
}

if ($Bootstrap) {
    $log = Join-Path $Results "bootstrap.log"
    Remove-Item $log -ErrorAction SilentlyContinue
    "=== bootstrap content ==="
    $args = @(
        "-batchmode", "-quit", "-nographics",
        "-projectPath", $Mirror,
        "-executeMethod", "BelowTheWing.EditorTools.ContentBootstrap.CreateMissingContent",
        "-logFile", $log
    )
    $proc = Start-Process -FilePath $Unity -ArgumentList $args -Wait -PassThru -NoNewWindow
    if ($proc.ExitCode -ne 0) {
        "  bootstrap failed (exit $($proc.ExitCode)). Tail of the editor log:"
        if (Test-Path $log) { Get-Content $log -Tail 40 | ForEach-Object { "    $_" } }
        throw "bootstrap failed"
    }

    if (Test-Path "$Mirror\Assets\Content") {
        robocopy "$Mirror\Assets\Content" "$Source\Assets\Content" /E /NJH /NJS /NFL /NDL /R:2 /W:1 | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "copying generated content back failed (exit $LASTEXITCODE)" }
        "  generated content copied back into the project"
        Get-ChildItem "$Source\Assets\Content" -Recurse -Filter *.asset | ForEach-Object { "    $($_.FullName.Replace($Source, ''))" }
    }
}

if ($Platform -eq "None") { return }

$platforms = if ($Platform -eq "Both") { @("EditMode", "PlayMode") } else { @($Platform) }
$failed = $false

foreach ($p in $platforms) {
    $xml = Join-Path $Results "$p.xml"
    $log = Join-Path $Results "$p.log"
    Remove-Item $xml, $log -ErrorAction SilentlyContinue

    $args = @(
        "-runTests", "-batchmode", "-nographics",
        "-projectPath", $Mirror,
        "-testPlatform", $p,
        "-testResults", $xml,
        "-logFile", $log
    )
    if ($Filter) { $args += @("-testFilter", $Filter) }

    "=== $p ==="
    $proc = Start-Process -FilePath $Unity -ArgumentList $args -Wait -PassThru -NoNewWindow
    $code = $proc.ExitCode

    if (-not (Test-Path $xml)) {
        "  NO RESULTS FILE. Unity exit code $code. Tail of the editor log:"
        if (Test-Path $log) { Get-Content $log -Tail 40 | ForEach-Object { "    $_" } }
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
