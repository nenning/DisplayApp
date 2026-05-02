param (
    [string]$Version
)

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
    $patch = if ($versionParts.Length -ge 3) { [int]$versionParts[2] + 1 } else { 1 }
    $Version = "$($versionParts[0]).$($versionParts[1]).$patch"
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
