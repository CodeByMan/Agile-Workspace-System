param(
    [string]$OutputPath = "Agile-Workspace-Portfolio.zip"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$requiredFiles = @(
    ".github\workflows\ci.yml",
    ".github\dependabot.yml",
    ".github\ISSUE_TEMPLATE\bug_report.yml",
    ".github\pull_request_template.md",
    "README.md",
    "AgileWorkspace.sln"
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repositoryRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Required repository file is missing: $relativePath"
    }
}

$excludedDirectoryNames = @(
    ".git",
    ".angular",
    ".vs",
    ".vscode",
    "bin",
    "coverage",
    "dist",
    "node_modules",
    "obj",
    "TestResults"
)
$excludedFilePatterns = @("*.zip", "*.log", ".env")
$outputFullPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputPath))

if (Test-Path -LiteralPath $outputFullPath) {
    Remove-Item -LiteralPath $outputFullPath -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::Open(
    $outputFullPath,
    [System.IO.Compression.ZipArchiveMode]::Create
)

try {
    Get-ChildItem -LiteralPath $repositoryRoot -Recurse -Force -File | ForEach-Object {
        $relativePath = $_.FullName.Substring($repositoryRoot.Length).TrimStart([char]92, [char]47)
        $segments = $relativePath -split '[\\/]'

        if ($segments | Where-Object { $excludedDirectoryNames -contains $_ }) {
            return
        }

        foreach ($pattern in $excludedFilePatterns) {
            if ($_.Name -like $pattern) {
                return
            }
        }

        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $archive,
            $_.FullName,
            (Join-Path "Agile-Workspace" $relativePath).Replace([char]92, [char]47),
            [System.IO.Compression.CompressionLevel]::Optimal
        ) | Out-Null
    }
}
finally {
    $archive.Dispose()
}

Write-Host "Created $outputFullPath"
Write-Host "Verified that CI, Dependabot, issue template, and pull-request template are included."
