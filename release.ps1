param (
    [string]$Version
)

# Locate git — add common install paths if not already on PATH
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    $candidates = @(
        "$env:ProgramFiles\Git\cmd\git.exe",
        "$env:ProgramFiles\Git\bin\git.exe",
        "${env:ProgramFiles(x86)}\Git\cmd\git.exe"
    )
    $found = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($found) {
        $env:PATH = "$([System.IO.Path]::GetDirectoryName($found));$env:PATH"
    } else {
        Write-Error "git not found. Install Git for Windows or add it to your PATH."
        exit 1
    }
}

# Ensure we're on the master branch
$branch = git rev-parse --abbrev-ref HEAD
if ($branch -ne "master") {
    Write-Error "Must be on master branch to release. Current branch: $branch"
    exit 1
}

# Check for upstream changes before doing anything
Write-Host "Checking for upstream changes..."
git fetch origin 2>&1 | Out-Null
$upstreamChanges = git log HEAD..origin/master --oneline
if ($upstreamChanges) {
    Write-Warning "Upstream changes detected on origin/master. Pull before releasing:"
    Write-Host $upstreamChanges
    exit 1
}

# If no version is provided, read the current version and increment the patch number
if (-not $Version) {
    Write-Host "No version argument provided. Auto-incrementing patch version..."
    $csproj = Get-Content DisplayApp.csproj -Raw
    if ($csproj -match '<Version>(.*)</Version>') {
        $currentVersion = $Matches[1]
    } else {
        Write-Error "Could not find <Version> in DisplayApp.csproj. Please specify a version manually using -Version."
        exit 1
    }
    $versionParts = $currentVersion.Split('.')
    if ($versionParts.Length -lt 2 -or $versionParts.Length -gt 4) {
        Write-Error "Invalid version format: $currentVersion. Expected Major.Minor.Patch"
        exit 1
    }
    $minor = [int]$versionParts[1] + 1
    $Version = "$($versionParts[0]).$minor.0"
    Write-Host "Current version: $currentVersion -> New version: $Version"
}

# Update version in DisplayApp.csproj
(Get-Content DisplayApp.csproj) -replace '<Version>.*</Version>', "<Version>$Version</Version>" | Set-Content DisplayApp.csproj
Write-Host "Updated version to $Version in DisplayApp.csproj"

# Build to validate
Write-Host "Building to validate..."
dotnet build DisplayApp.csproj --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed. Aborting release."
    exit 1
}

# Commit, tag, and push
Write-Host "Creating git tag v$Version"
git add DisplayApp.csproj
git commit DisplayApp.csproj -m "Bump version to $Version"
git tag -a "v$Version" -m "Version $Version"

Write-Host "Pushing to origin..."
git push origin master:master
git push origin "v$Version"

Write-Host "Release process complete. The GitHub Action will build and publish the release."
