# DeepSeeArch Build Script
# PowerShell Script für automatisierten Build-Prozess

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [Parameter(Mandatory=$false)]
    [switch]$Clean,
    
    [Parameter(Mandatory=$false)]
    [switch]$Publish,
    
    [Parameter(Mandatory=$false)]
    [switch]$Test,
    
    [Parameter(Mandatory=$false)]
    [switch]$Package
)

$ErrorActionPreference = "Stop"

# Farben für Output
function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Error { Write-Host $args -ForegroundColor Red }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }

# Header
Write-Info "================================================"
Write-Info "  DeepSeeArch Build Script"
Write-Info "================================================"
Write-Info ""

# Prüfe ob .NET SDK installiert ist
Write-Info "Checking .NET SDK..."
try {
    $dotnetVersion = dotnet --version
    Write-Success "✓ .NET SDK found: $dotnetVersion"
} catch {
    Write-Error "✗ .NET SDK not found. Please install .NET 8 SDK."
    exit 1
}

# Projekt-Pfade
$rootPath = $PSScriptRoot
$projectPath = Join-Path $rootPath "DeepSeeArch"
$projectFile = Join-Path $projectPath "DeepSeeArch.csproj"
$outputPath = Join-Path $rootPath "build"
$publishPath = Join-Path $outputPath "publish"

Write-Info "Root Path: $rootPath"
Write-Info "Project Path: $projectPath"
Write-Info ""

# Clean
if ($Clean) {
    Write-Info "Cleaning previous builds..."
    
    if (Test-Path $outputPath) {
        Remove-Item $outputPath -Recurse -Force
        Write-Success "✓ Cleaned build directory"
    }
    
    # Clean bin/obj
    Get-ChildItem -Path $rootPath -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force
    Write-Success "✓ Cleaned bin/obj directories"
    
    Write-Info ""
}

# Restore NuGet Packages
Write-Info "Restoring NuGet packages..."
try {
    dotnet restore $projectFile
    Write-Success "✓ NuGet packages restored"
} catch {
    Write-Error "✗ Failed to restore NuGet packages"
    exit 1
}
Write-Info ""

# Build
Write-Info "Building project ($Configuration)..."
try {
    dotnet build $projectFile `
        --configuration $Configuration `
        --no-restore `
        --verbosity minimal
    Write-Success "✓ Build completed successfully"
} catch {
    Write-Error "✗ Build failed"
    exit 1
}
Write-Info ""

# Test
if ($Test) {
    Write-Info "Running tests..."
    try {
        # Wenn Tests vorhanden
        $testProjects = Get-ChildItem -Path $rootPath -Filter "*.Tests.csproj" -Recurse
        
        if ($testProjects.Count -gt 0) {
            foreach ($testProject in $testProjects) {
                dotnet test $testProject.FullName `
                    --configuration $Configuration `
                    --no-build `
                    --verbosity minimal
            }
            Write-Success "✓ All tests passed"
        } else {
            Write-Warning "⚠ No test projects found"
        }
    } catch {
        Write-Error "✗ Tests failed"
        exit 1
    }
    Write-Info ""
}

# Publish
if ($Publish) {
    Write-Info "Publishing application..."
    
    # Erstelle Publish-Verzeichnis
    New-Item -Path $publishPath -ItemType Directory -Force | Out-Null
    
    # Verschiedene Publish-Varianten
    $publishConfigs = @(
        @{
            Name = "win-x64-selfcontained"
            Runtime = "win-x64"
            SelfContained = $true
            SingleFile = $true
        },
        @{
            Name = "win-x64-framework"
            Runtime = "win-x64"
            SelfContained = $false
            SingleFile = $false
        }
    )
    
    foreach ($config in $publishConfigs) {
        $targetPath = Join-Path $publishPath $config.Name
        
        Write-Info "  Publishing $($config.Name)..."
        
        $publishArgs = @(
            "publish"
            $projectFile
            "--configuration", $Configuration
            "--runtime", $config.Runtime
            "--output", $targetPath
            "--no-build"
        )
        
        if ($config.SelfContained) {
            $publishArgs += "--self-contained", "true"
        } else {
            $publishArgs += "--self-contained", "false"
        }
        
        if ($config.SingleFile) {
            $publishArgs += "-p:PublishSingleFile=true"
            $publishArgs += "-p:IncludeNativeLibrariesForSelfExtract=true"
        }
        
        try {
            & dotnet $publishArgs
            Write-Success "  ✓ $($config.Name) published to $targetPath"
        } catch {
            Write-Error "  ✗ Failed to publish $($config.Name)"
        }
    }
    
    Write-Info ""
}

# Package (ZIP erstellen)
if ($Package) {
    Write-Info "Creating distribution packages..."
    
    if (-not $Publish) {
        Write-Warning "⚠ Publish flag not set. Run with -Publish to create packages."
    } else {
        $packagesPath = Join-Path $outputPath "packages"
        New-Item -Path $packagesPath -ItemType Directory -Force | Out-Null
        
        # Hole Version aus .csproj
        [xml]$csproj = Get-Content $projectFile
        $version = $csproj.Project.PropertyGroup.Version
        if (-not $version) {
            $version = "1.0.0"
        }
        
        foreach ($config in Get-ChildItem -Path $publishPath -Directory) {
            $zipName = "DeepSeeArch-$version-$($config.Name).zip"
            $zipPath = Join-Path $packagesPath $zipName
            
            Write-Info "  Creating $zipName..."
            
            try {
                Compress-Archive -Path "$($config.FullName)\*" `
                    -DestinationPath $zipPath `
                    -Force
                
                $zipSize = (Get-Item $zipPath).Length / 1MB
                Write-Success "  ✓ Package created: $zipName ($([math]::Round($zipSize, 2)) MB)"
            } catch {
                Write-Error "  ✗ Failed to create package $zipName"
            }
        }
    }
    
    Write-Info ""
}

# Zusammenfassung
Write-Info "================================================"
Write-Success "Build completed successfully!"
Write-Info "================================================"
Write-Info ""

if ($Publish) {
    Write-Info "Published binaries are located in:"
    Write-Info "  $publishPath"
    Write-Info ""
}

if ($Package) {
    Write-Info "Distribution packages are located in:"
    Write-Info "  $(Join-Path $outputPath 'packages')"
    Write-Info ""
}

Write-Info "To run the application:"
Write-Info "  Debug:   dotnet run --project $projectFile --configuration Debug"
Write-Info "  Release: dotnet run --project $projectFile --configuration Release"
Write-Info ""

Write-Success "Done! 🎉"
