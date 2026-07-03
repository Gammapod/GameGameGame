param(
    [switch]$Detailed,
    [switch]$Json
)

$ErrorActionPreference = 'Stop'

function Get-Category {
    param([string]$Path)

    $normalizedPath = $Path -replace '\\', '/'

    if ($normalizedPath -match '\.ya?ml$') {
        return 'yaml content'
    }

    if ($normalizedPath -match '^tests/') {
        return 'tests'
    }

    if ($normalizedPath -match '^(docs/|README\.md$|\.opencode/)' -or
        $normalizedPath -match '\.(md|mmd|txt)$') {
        return 'agent/docs'
    }

    if ($normalizedPath -match '^src/.+\.cs$') {
        return 'functional code'
    }

    return 'project/config'
}

function Get-LineCount {
    param([string]$Path)

    if ((Get-Item -LiteralPath $Path).Length -eq 0) {
        return 0
    }

    return (Get-Content -LiteralPath $Path | Measure-Object -Line).Lines
}

$repoRoot = git rev-parse --show-toplevel

if ($LASTEXITCODE -ne 0) {
    throw 'git rev-parse failed. Run this script from inside a Git repository.'
}

$trackedFiles = @(git -C $repoRoot ls-files)

if ($LASTEXITCODE -ne 0) {
    throw 'git ls-files failed. Run this script from inside a Git repository.'
}

$files = @(
    foreach ($file in $trackedFiles) {
        $fullPath = Join-Path -Path $repoRoot -ChildPath $file
        $lineCount = Get-LineCount -Path $fullPath

        [pscustomobject]@{
            Category = Get-Category -Path $file
            Lines = $lineCount
            File = $file
        }
    }
)

$categories = @('functional code', 'tests', 'agent/docs', 'yaml content', 'project/config')

$summary = @(
    foreach ($category in $categories) {
        $categoryFiles = @($files | Where-Object { $_.Category -eq $category })

        [pscustomobject]@{
            Category = $category
            Files = $categoryFiles.Count
            Lines = [int](($categoryFiles | Measure-Object -Property Lines -Sum).Sum)
        }
    }
)

$total = [pscustomobject]@{
    Category = 'total'
    Files = $files.Count
    Lines = [int](($summary | Measure-Object -Property Lines -Sum).Sum)
}

if ($Json) {
    $report = [ordered]@{
        Summary = @($summary + $total)
    }

    if ($Detailed) {
        $report.Files = $files
    }

    [pscustomobject]$report | ConvertTo-Json -Depth 4
    exit
}

$summary + $total | Format-Table -AutoSize

if ($Detailed) {
    $files | Sort-Object Category, File | Format-Table Category, Lines, File -AutoSize
}
