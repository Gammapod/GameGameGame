param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$ManifestPath = "src/GameGameGame.Content/Beta/FeedbackManifest.yaml",
    [string]$OutputRoot = "artifacts/feedback-build"
)

$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDirectory "..")
$manifestFullPath = Join-Path $repoRoot $ManifestPath

if (-not (Test-Path -LiteralPath $manifestFullPath)) {
    throw "Feedback manifest not found: $manifestFullPath"
}

$publishDirectory = Join-Path $repoRoot (Join-Path $OutputRoot "publish/$Runtime")
$packageName = "GameGameGame-feedback-$Runtime"
$stageParent = Join-Path $repoRoot (Join-Path $OutputRoot "stage")
$stageDirectory = Join-Path $stageParent $packageName
$zipPath = Join-Path $repoRoot (Join-Path $OutputRoot "$packageName.zip")

if (Test-Path -LiteralPath $publishDirectory) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}

if (Test-Path -LiteralPath $stageDirectory) {
    Remove-Item -LiteralPath $stageDirectory -Recurse -Force
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $stageParent -Force | Out-Null

dotnet publish (Join-Path $repoRoot "src/GameGameGame.SadConsole/GameGameGame.SadConsole.csproj") `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDirectory

Copy-Item -LiteralPath $publishDirectory -Destination $stageDirectory -Recurse

$betaStageDirectory = Join-Path $stageDirectory "Content/Beta"
if (Test-Path -LiteralPath $betaStageDirectory) {
    Remove-Item -LiteralPath $betaStageDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $betaStageDirectory | Out-Null

$manifestLines = Get-Content -LiteralPath $manifestFullPath
$contentPaths = @()
foreach ($line in $manifestLines) {
    if ($line -match '^\s*-?\s*contentPath:\s*(.+?)\s*$') {
        $contentPath = $Matches[1].Trim().Trim('"').Trim("'")
        if (-not [string]::IsNullOrWhiteSpace($contentPath)) {
            $contentPaths += $contentPath
        }
    }
}

$contentPaths = $contentPaths | Sort-Object -Unique
foreach ($contentPath in $contentPaths) {
    $sourcePath = Join-Path $repoRoot $contentPath
    if (-not (Test-Path -LiteralPath $sourcePath)) {
        throw "Feedback manifest references missing content file: $contentPath"
    }

    $normalized = $contentPath.Replace('\', '/')
    $marker = "GameGameGame.Content/Beta/"
    $markerIndex = $normalized.IndexOf($marker, [StringComparison]::OrdinalIgnoreCase)
    if ($markerIndex -lt 0) {
        throw "Feedback content path must live under GameGameGame.Content/Beta: $contentPath"
    }

    $relativeBetaPath = $normalized.Substring($markerIndex + $marker.Length).Replace('/', [IO.Path]::DirectorySeparatorChar)
    $destinationPath = Join-Path $betaStageDirectory $relativeBetaPath
    $destinationParent = Split-Path -Parent $destinationPath
    if (-not (Test-Path -LiteralPath $destinationParent)) {
        New-Item -ItemType Directory -Path $destinationParent | Out-Null
    }

    Copy-Item -LiteralPath $sourcePath -Destination $destinationPath
}

Copy-Item -LiteralPath $manifestFullPath -Destination (Join-Path $betaStageDirectory "Manifest.yaml")
Copy-Item -LiteralPath $manifestFullPath -Destination (Join-Path $betaStageDirectory "FeedbackManifest.yaml")

$readmePath = Join-Path $stageDirectory "README_FEEDBACK_BUILD.txt"
Set-Content -LiteralPath $readmePath -Value @(
    "GameGameGame feedback build",
    "",
    "Launch GameGameGame.exe to open the curated feedback scenario catalog.",
    "Bundled content is curated from src/GameGameGame.Content/Beta/FeedbackManifest.yaml.",
    "This package intentionally excludes validation/debug/log-testing scenarios."
)

Compress-Archive -LiteralPath $stageDirectory -DestinationPath $zipPath -Force

"Feedback build package: $zipPath"
