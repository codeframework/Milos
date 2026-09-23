[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$SolutionPath = (Join-Path $PSScriptRoot 'MilosSolutionPlatform.sln'),
    [string]$Configuration = 'Release',
    [string]$Source = 'https://api.nuget.org/v3/index.json',
    [string]$ApiKey = $env:NUGET_API_KEY,
    [string]$OutputPath = (Join-Path $PSScriptRoot 'artifacts\nuget'),
    [switch]$Pack,
    [switch]$SkipDuplicate = $true,
    [switch]$IncludeSymbols,
    [switch]$CleanOutput
)

$ErrorActionPreference = 'Stop'

function Get-SolutionProjects {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $solutionDirectory = Split-Path -Path $Path -Parent

    Get-Content -Path $Path |
        Where-Object { $_ -match '^Project\("\{[^\}]+\}"\) = ".+", ".+\.csproj", "\{[^\}]+\}"$' } |
        ForEach-Object {
            $segments = $_ -split ', '
            $projectPath = $segments[1].Trim('"')
            [System.IO.Path]::GetFullPath((Join-Path $solutionDirectory $projectPath))
        }
}

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -Path $SolutionPath)) {
    throw "Solution file not found: $SolutionPath"
}

if ($CleanOutput -and (Test-Path -Path $OutputPath)) {
    Remove-Item -Path $OutputPath -Recurse -Force
}

New-Item -Path $OutputPath -ItemType Directory -Force | Out-Null

$projects = Get-SolutionProjects -Path $SolutionPath
if (-not $projects) {
    throw "No C# projects were found in solution '$SolutionPath'."
}

if ($Pack) {
    foreach ($project in $projects) {
        if ($PSCmdlet.ShouldProcess($project, 'Pack project')) {
            Invoke-DotNet -Arguments @(
                'pack'
                $project
                '--configuration'
                $Configuration
                '--output'
                $OutputPath
            )
        }
    }
}

$packages = Get-ChildItem -Path $OutputPath -Filter '*.nupkg' -File |
    Where-Object {
        if ($IncludeSymbols) {
            $true
        }
        else {
            $_.Name -notlike '*.symbols.nupkg' -and $_.Name -notlike '*.snupkg'
        }
    } |
    Sort-Object Name

if (-not $packages) {
    throw "No packages were found in '$OutputPath'. Use -Pack to generate them first or point -OutputPath to an existing package folder."
}

if (-not $ApiKey) {
    throw 'No NuGet API key was provided. Pass -ApiKey or set the NUGET_API_KEY environment variable.'
}

foreach ($package in $packages) {
    $arguments = @(
        'nuget'
        'push'
        $package.FullName
        '--api-key'
        $ApiKey
        '--source'
        $Source
    )

    if ($SkipDuplicate) {
        $arguments += '--skip-duplicate'
    }

    if ($PSCmdlet.ShouldProcess($package.FullName, 'Push package')) {
        Invoke-DotNet -Arguments $arguments
    }
}

Write-Host "Processed $($packages.Count) package(s) from '$OutputPath'."
