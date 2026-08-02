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
if (-not (Test-Path -LiteralPath $betaStageDirectory)) {
    throw "Published build did not include expected content directory: $betaStageDirectory"
}

Copy-Item -LiteralPath $manifestFullPath -Destination (Join-Path $betaStageDirectory "Manifest.yaml")
Copy-Item -LiteralPath $manifestFullPath -Destination (Join-Path $betaStageDirectory "FeedbackManifest.yaml")

$readmePath = Join-Path $stageDirectory "README_FEEDBACK_BUILD.txt"
Set-Content -LiteralPath $readmePath -Value @(
    "GameGameGame feedback build",
    "",
    "Launch GameGameGame.exe to open the curated feedback scenario catalog.",
    "Bundled YAML content includes the full packaged Beta content folder for inspection/editor use.",
    "The normal frontend catalog uses Content/Beta/Manifest.yaml, replaced from FeedbackManifest.yaml, so validation/debug/log-testing scenarios are not exposed through the curated scenario list."
)

Compress-Archive -LiteralPath $stageDirectory -DestinationPath $zipPath -Force

"Feedback build package: $zipPath"
