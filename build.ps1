# build.ps1
# DeepSeeArch Build Script (PowerShell 5.1+ kompatibel)
# - Restore / Build / Test / Publish
# - Keine kaputten try/catch-Klammern
# - Keine überschriebenen PowerShell-Cmdlets (z.B. Write-Error)

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter(Mandatory = $false)]
    [switch]$Clean,

    [Parameter(Mandatory = $false)]
    [switch]$Test,

    [Parameter(Mandatory = $false)]
    [switch]$Publish,

    [Parameter(Mandatory = $false)]
    [switch]$SelfContained,

    [Parameter(Mandatory = $false)]
    [switch]$SingleFile
)

$ErrorActionPreference = "Stop"

function Write-Ok([string]$msg)   { Write-Host $msg -ForegroundColor Green }
function Write-Info([string]$msg) { Write-Host $msg -ForegroundColor Cyan }
function Write-Warn([string]$msg) { Write-Host $msg -ForegroundColor Yellow }
function Write-Fail([string]$msg) { Write-Host $msg -ForegroundColor Red }

Write-Info "================================================"
Write-Info "  DeepSeeArch Build Script"
Write-Info "================================================"
Write-Info ""

# Paths
$RootPath    = $PSScriptRoot
$ProjectPath = Join-Path $RootPath "DeepSeeArch"
$ProjectFile = Join-Path $ProjectPath "DeepSeeArch.csproj"
$OutPath     = Join-Path $RootPath "out"
$PublishPath = Join-Path $OutPath "publish"

Write-Info "Root Path   : $RootPath"
Write-Info "Project File: $ProjectFile"
Write-Info "Config      : $Configuration"
Write-Info ""

if (-not (Test-Path $ProjectFile)) {
    Write-Fail "Project file not found: $ProjectFile"
    exit 1
}

# Check dotnet
Write-Info "Checking .NET SDK..."
try {
    $dotnetVersion = & dotnet --version
    Write-Ok "OK: dotnet $dotnetVersion"
} catch {
    Write-Fail "dotnet not found. Install .NET 8 SDK (recommended)."
    exit 1
}
Write-Info ""

# Clean
if ($Clean) {
    Write-Info "Cleaning build output..."
    try {
        if (Test-Path $OutPath) {
            Remove-Item $OutPath -Recurse -Force
        }

        # Clean bin/obj
        $dirs = Get-ChildItem -Path $RootPath -Recurse -Directory -Force | Where-Object { $_.Name -in @("bin", "obj") }
        foreach ($d in $dirs) {
            try {
                Remove-Item $d.FullName -Recurse -Force -ErrorAction SilentlyContinue
            } catch { }
        }

        Write-Ok "OK: Cleaned"
    } catch {
        Write-Fail "Clean failed: $($_.Exception.Message)"
        exit 1
    }
    Write-Info ""
}

# Restore
Write-Info "Restoring NuGet packages..."
try {
    & dotnet restore $ProjectFile
    Write-Ok "OK: Restore"
} catch {
    Write-Fail "Restore failed: $($_.Exception.Message)"
    exit 1
}
Write-Info ""

# Build
Write-Info "Building..."
try {
    & dotnet build $ProjectFile --configuration $Configuration --no-restore
    Write-Ok "OK: Build"
} catch {
    Write-Fail "Build failed: $($_.Exception.Message)"
    exit 1
}
Write-Info ""

# Test (optional)
if ($Test) {
    Write-Info "Running tests..."
    try {
        $testProjects = Get-ChildItem -Path $RootPath -Recurse -Filter "*.Tests.csproj" -File -ErrorAction SilentlyContinue
        if ($null -eq $testProjects -or $testProjects.Count -eq 0) {
            Write-Warn "No test projects found (*.Tests.csproj)."
        } else {
            foreach ($tp in $testProjects) {
                Write-Info "dotnet test: $($tp.FullName)"
                & dotnet test $tp.FullName --configuration $Configuration --no-build
            }
            Write-Ok "OK: Tests"
        }
    } catch {
        Write-Fail "Tests failed: $($_.Exception.Message)"
        exit 1
    }
    Write-Info ""
}

# Publish (optional)
if ($Publish) {
    Write-Info "Publishing..."
    try {
        New-Item -ItemType Directory -Path $PublishPath -Force | Out-Null

        $runtime = "win-x64"
        $sc = $false
        if ($SelfContained) { $sc = $true }

        $sf = $false
        if ($SingleFile) { $sf = $true }

        $args = @(
            "publish", $ProjectFile,
            "--configuration", $Configuration,
            "--output", $PublishPath,
            "--runtime", $runtime
        )

        if ($sc) {
            $args += "--self-contained"
            $args += "true"
        } else {
            $args += "--self-contained"
            $args += "false"
        }

        if ($sf) {
            $args += "/p:PublishSingleFile=true"
        }

        Write-Info ("dotnet " + ($args -join " "))
        & dotnet @args

        Write-Ok "OK: Publish -> $PublishPath"
    } catch {
        Write-Fail "Publish failed: $($_.Exception.Message)"
        exit 1
    }
    Write-Info ""
}

Write-Ok "DONE."
exit 0
