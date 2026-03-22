param(
    [string]$Configuration = "Debug",
    [string]$TestFilter
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $repoRoot "Primo.MIA.Tests\Primo.MIA.Tests.csproj"
$testAssembly = Join-Path $repoRoot "Primo.MIA.Tests\bin\$Configuration\net462\Primo.MIA.Tests.dll"
$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"

if (-not (Test-Path $vswhere)) {
    throw "vswhere.exe не найден. Установите Visual Studio Installer."
}

$vsPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
if (-not $vsPath) {
    throw "Не удалось найти установленную Visual Studio с MSBuild."
}

$msbuild = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
$vstest = Join-Path $vsPath "Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"

if (-not (Test-Path $msbuild)) {
    throw "MSBuild.exe не найден: $msbuild"
}

if (-not (Test-Path $vstest)) {
    throw "vstest.console.exe не найден: $vstest"
}

Write-Host "Building tests with MSBuild..."
& $msbuild $testProject /t:Build /p:Configuration=$Configuration /m
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild завершился с кодом $LASTEXITCODE"
}

if (-not (Test-Path $testAssembly)) {
    throw "Тестовая сборка не найдена: $testAssembly"
}

Write-Host "Running tests with VSTest..."
$vstestArgs = @($testAssembly)
if ($TestFilter) {
    $vstestArgs += "/TestCaseFilter:$TestFilter"
}

& $vstest @vstestArgs
exit $LASTEXITCODE
