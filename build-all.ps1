#Requires -Version 5.1
<#
.SYNOPSIS
  Batch-build NDToolsBox.dll for each installed 3ds Max year.

.EXAMPLE
  .\build-all.ps1
  .\build-all.ps1 -Configuration Release
  .\build-all.ps1 -Years 2022,2023,2024,2025,2026
  .\build-all.ps1 -Force -Years 2015
  .\build-all.ps1 -NoPause

  Output DLLs: dist\<year>\assemblies\
  Logs:        dist\logs\build-all-*.log  and  dist\<year>\build.log
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [ValidateSet('x64', 'AnyCPU', 'Any CPU')]
    [string] $Platform = 'x64',

    # Comma-separated years, e.g. "2022,2023,2024,2025,2026". Empty = all mapped years.
    [string] $Years = '',

    [switch] $Force,

    [string] $MsBuild = '',

    # Skip "Press Enter" at the end (for CI / piping).
    [switch] $NoPause
)

$ErrorActionPreference = 'Stop'
$RepoRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

# Normalize platform for MSBuild (solution uses "x64" / "Any CPU")
$MsBuildPlatform = if ($Platform -eq 'AnyCPU') { 'Any CPU' } else { $Platform }

$Projects = @(
    @{ Year = 2015; RelPath = 'Max2015\Max2015.csproj' }
    @{ Year = 2016; RelPath = 'Max2016\Max2016.csproj' }
    @{ Year = 2017; RelPath = 'Max2017\Max2017.csproj' }
    @{ Year = 2018; RelPath = 'Max2018\Max2018.csproj' }
    @{ Year = 2019; RelPath = 'Max2019\Max2019.csproj' }
    @{ Year = 2020; RelPath = 'Max202\Max2020.csproj' }
    @{ Year = 2021; RelPath = 'Max2021\Max2021.csproj' }
    @{ Year = 2022; RelPath = 'Max2022\Max2022.csproj' }
    @{ Year = 2023; RelPath = 'Max2023\Max2023.csproj' }
    @{ Year = 2024; RelPath = 'Max2024\Max2024.csproj' }
    @{ Year = 2025; RelPath = 'Max2025\Max2025.csproj' }
    @{ Year = 2026; RelPath = 'Max2026\Max2026.csproj' }
)

$LogDir = Join-Path $RepoRoot 'dist\logs'
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
$MainLog = Join-Path $LogDir ("build-all-{0:yyyyMMdd-HHmmss}.log" -f (Get-Date))

function Write-Log {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string] $Message,

        [ConsoleColor] $ForegroundColor = [ConsoleColor]::Gray
    )
    $line = '{0:yyyy-MM-dd HH:mm:ss} {1}' -f (Get-Date), $Message
    Add-Content -LiteralPath $MainLog -Value $line -Encoding UTF8
    if ($PSBoundParameters.ContainsKey('ForegroundColor')) {
        Write-Host $Message -ForegroundColor $ForegroundColor
    }
    else {
        Write-Host $Message
    }
}

function Append-LogFile {
    param(
        [string] $Path,
        [string[]] $Lines
    )
    if (-not $Lines -or $Lines.Count -eq 0) { return }
    Add-Content -LiteralPath $Path -Value $Lines -Encoding UTF8
    Add-Content -LiteralPath $MainLog -Value $Lines -Encoding UTF8
}

function Get-MsBuildPath {
    param([string] $Explicit)

    if ($Explicit -and (Test-Path -LiteralPath $Explicit)) {
        return (Resolve-Path -LiteralPath $Explicit).Path
    }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vswhere) {
        $found = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' 2>$null |
            Select-Object -First 1
        if ($found -and (Test-Path -LiteralPath $found)) {
            return $found
        }
    }

    $cmd = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
    if ($cmd) {
        return $cmd.Source
    }

    throw 'MSBuild.exe not found. Install Visual Studio / Build Tools, or pass -MsBuild path.'
}

function Test-MaxInstalled {
    param([int] $Year)

    $candidates = @(
        (Join-Path $env:ProgramFiles "Autodesk\3ds Max $Year\Autodesk.Max.dll"),
        (Join-Path 'D:\Program Files' "Autodesk\3ds Max $Year\Autodesk.Max.dll")
    )
    foreach ($p in $candidates) {
        if (Test-Path -LiteralPath $p) {
            return $true
        }
    }
    return $false
}

function Get-ExpectedDllPath {
    param(
        [string] $ProjectDir,
        [string] $Configuration,
        [string] $Platform
    )

    if ($Platform -eq 'x64') {
        return (Join-Path $ProjectDir "bin\x64\$Configuration\NDToolsBox.dll")
    }
    return (Join-Path $ProjectDir "bin\$Configuration\NDToolsBox.dll")
}

function Invoke-MsBuildLogged {
    param(
        [string] $MsBuildExe,
        [string[]] $Arguments,
        [string] $YearLog
    )

    $fileLog = [IO.Path]::ChangeExtension($YearLog, '.msbuild.log')
    $allArgs = $Arguments + @(
        '/fl'
        "/flp:logfile=$fileLog;verbosity=normal;encoding=UTF-8"
    )

    # Capture console stream so failures are not lost when the window closes.
    $output = & $MsBuildExe @allArgs 2>&1
    $code = $LASTEXITCODE
    $lines = @($output | ForEach-Object { "$_" })

    foreach ($line in $lines) {
        Write-Host $line
    }
    Append-LogFile -Path $YearLog -Lines $lines

    if (Test-Path -LiteralPath $fileLog) {
        Append-LogFile -Path $YearLog -Lines @(
            ''
            "---- MSBuild file log: $fileLog ----"
        )
        # Keep file log on disk; also note path in year log.
        Add-Content -LiteralPath $MainLog -Value "MSBuild file log: $fileLog" -Encoding UTF8
    }

    return $code
}

$exitCode = 0
try {
    $selectedYears = $null
    if (-not [string]::IsNullOrWhiteSpace($Years)) {
        $selectedYears = @{}
        foreach ($part in ($Years -split '[,;\s]+')) {
            if ([string]::IsNullOrWhiteSpace($part)) { continue }
            $y = 0
            if (-not [int]::TryParse($part.Trim(), [ref]$y)) {
                throw "Invalid year in -Years: '$part'"
            }
            $selectedYears[$y] = $true
        }
    }

    $msbuildPath = Get-MsBuildPath -Explicit $MsBuild
    Write-Log "MSBuild: $msbuildPath"
    Write-Log "Configuration=$Configuration Platform=$MsBuildPlatform Force=$Force"
    Write-Log "Main log: $MainLog"
    Write-Log ''

    $toBuild = @($Projects | Where-Object {
        (-not $selectedYears) -or $selectedYears.ContainsKey($_.Year)
    })
    if ($toBuild.Count -eq 0) {
        Write-Log "No projects match -Years filter. Known years: $($Projects.Year -join ', ')" -ForegroundColor Red
        $exitCode = 1
        throw 'No matching projects'
    }

    $results = @()
    $failCount = 0

    foreach ($item in $toBuild) {
        $year = $item.Year
        $projPath = Join-Path $RepoRoot $item.RelPath
        $projDir = Split-Path -Parent $projPath
        $dllPath = Get-ExpectedDllPath -ProjectDir $projDir -Configuration $Configuration -Platform $MsBuildPlatform
        $yearOutDir = Join-Path $RepoRoot "dist\$year"
        $yearLog = Join-Path $yearOutDir 'build.log'
        New-Item -ItemType Directory -Force -Path $yearOutDir | Out-Null
        if (Test-Path -LiteralPath $yearLog) {
            Remove-Item -LiteralPath $yearLog -Force
        }

        if (-not (Test-Path -LiteralPath $projPath)) {
            $results += [pscustomobject]@{
                Year   = $year
                Status = 'MissingProject'
                Detail = $projPath
            }
            $failCount++
            Write-Log "[$year] MissingProject  $projPath" -ForegroundColor Red
            continue
        }

        $installed = Test-MaxInstalled -Year $year
        if (-not $installed -and -not $Force) {
            $results += [pscustomobject]@{
                Year   = $year
                Status = 'Skip'
                Detail = '3ds Max not detected (Autodesk.Max.dll)'
            }
            Write-Log "[$year] Skip           Max not installed" -ForegroundColor DarkYellow
            continue
        }

        if (-not $installed -and $Force) {
            Write-Log "[$year] Force build    (Max not detected)" -ForegroundColor Cyan
        }
        else {
            Write-Log "[$year] Building..." -ForegroundColor Cyan
        }

        Write-Log "[$year] Project: $projPath"
        Write-Log "[$year] Year log: $yearLog"

        # Non-SDK projects may ignore Restore; attempt once without failing the whole matrix.
        $null = & $msbuildPath @(
            $projPath
            '/t:Restore'
            "/p:Configuration=$Configuration"
            "/p:Platform=$MsBuildPlatform"
            '/v:quiet'
            '/nologo'
        ) 2>&1

        $buildArgs = @(
            $projPath
            '/t:Build'
            "/p:Configuration=$Configuration"
            "/p:Platform=$MsBuildPlatform"
            '/v:minimal'
            '/nologo'
            '/m'
        )

        $code = Invoke-MsBuildLogged -MsBuildExe $msbuildPath -Arguments $buildArgs -YearLog $yearLog
        $dllExists = Test-Path -LiteralPath $dllPath
        if ($code -ne 0 -and -not $dllExists) {
            $results += [pscustomobject]@{
                Year   = $year
                Status = 'Fail'
                Detail = "MSBuild exit $code ; see $yearLog"
            }
            $failCount++
            Write-Log "[$year] Fail           exit $code  (log: $yearLog)" -ForegroundColor Red
            continue
        }
        if ($code -ne 0 -and $dllExists) {
            Write-Log "[$year] Warn           MSBuild exit $code but DLL exists; publishing to dist (often leftover PostBuild)." -ForegroundColor DarkYellow
        }

        if (-not $dllExists) {
            $results += [pscustomobject]@{
                Year   = $year
                Status = 'Fail'
                Detail = "DLL missing: $dllPath ; see $yearLog"
            }
            $failCount++
            Write-Log "[$year] Fail           DLL not found: $dllPath" -ForegroundColor Red
            continue
        }

        # Publish to dist\<year>\assemblies\
        $outDir = Join-Path $yearOutDir 'assemblies'
        New-Item -ItemType Directory -Force -Path $outDir | Out-Null
        $destDll = Join-Path $outDir 'NDToolsBox.dll'
        Copy-Item -LiteralPath $dllPath -Destination $destDll -Force

        $pdbPath = [IO.Path]::ChangeExtension($dllPath, '.pdb')
        if (Test-Path -LiteralPath $pdbPath) {
            Copy-Item -LiteralPath $pdbPath -Destination (Join-Path $outDir 'NDToolsBox.pdb') -Force
        }

        $results += [pscustomobject]@{
            Year   = $year
            Status = 'Ok'
            Detail = $destDll
        }
        Write-Log "[$year] Ok             $destDll" -ForegroundColor Green
    }

    Write-Log ''
    Write-Log '=== Summary ==='
    $summaryText = ($results | Format-Table -AutoSize Year, Status, Detail | Out-String)
    Write-Host $summaryText
    Add-Content -LiteralPath $MainLog -Value $summaryText -Encoding UTF8

    $ok = @($results | Where-Object Status -eq 'Ok').Count
    $skip = @($results | Where-Object Status -eq 'Skip').Count
    Write-Log "Ok=$ok  Skip=$skip  Fail=$failCount"
    Write-Log "Main log: $MainLog"

    if ($failCount -gt 0) {
        $exitCode = 1
        Write-Log 'One or more builds failed. Open the year build.log / .msbuild.log under dist\<year>\.' -ForegroundColor Red
    }
}
catch {
    $exitCode = 1
    $err = $_.Exception.Message
    if (-not $err) { $err = "$_" }
    try {
        Write-Log "ERROR: $err" -ForegroundColor Red
        if ($_.ScriptStackTrace) {
            Write-Log $_.ScriptStackTrace
        }
    }
    catch {
        Write-Host "ERROR: $err" -ForegroundColor Red
    }
}
finally {
    Write-Host ''
    Write-Host "Log saved: $MainLog" -ForegroundColor Cyan
    if (-not $NoPause) {
        try {
            Write-Host ''
            Read-Host 'Press Enter to close'
        }
        catch {
            # Non-interactive host
            Start-Sleep -Seconds 3
        }
    }
}

exit $exitCode
