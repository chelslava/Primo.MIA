param(
    [string]$Configuration = "Debug",
    [string]$TestFilter,
    [string]$ResultsPath,   # опционально: полный путь к .trx с результатами тестов (для CI)
    # issue #21: разрешить прогон без тестов (integration-job, пока нет тестов
    # с трейтом Category=Integration — иначе защита ниже бросит "пустой прогон")
    [switch]$AllowEmptyRun
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $repoRoot "Primo.MIA.Tests\Primo.MIA.Tests.csproj"
$testAssembly = Join-Path $repoRoot "Primo.MIA.Tests\bin\$Configuration\net481\Primo.MIA.Tests.dll"
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

Write-Host "Restoring NuGet packages..."
# Restore отдельным вызовом: при объединённом -t:Restore,Build MSBuild строит
# проект по оценке ДО записи .nuget.g.props — импорты из NuGet (props пакетов,
# свойства $(Pkg...)) тогда недоступны, и адаптер xUnit не копируется в output
& $msbuild $testProject /t:Restore /p:Configuration=$Configuration /m
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild restore завершился с кодом $LASTEXITCODE"
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
if ($ResultsPath) {
    $resultsDir = Split-Path -Parent $ResultsPath
    if (-not (Test-Path $resultsDir)) {
        New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
    }
    $vstestArgs += "/logger:trx;LogFileName=$ResultsPath"
}

& $vstest @vstestArgs
$vstestExit = $LASTEXITCODE

# Защита от «пустого зелёного прогона»: vstest возвращает 0, даже если
# тесты не обнаружены (нет адаптера, сборка не загрузилась). Проверяем trx.
if ($ResultsPath -and (Test-Path $ResultsPath)) {
    [xml]$trx = Get-Content $ResultsPath
    $total = [int]$trx.TestRun.ResultSummary.Counters.total
    $failed = [int]$trx.TestRun.ResultSummary.Counters.failed
    if ($total -eq 0 -and -not $AllowEmptyRun) {
        throw "Пустой прогон: в trx 0 тестов (сборка не загрузилась или адаптер не найден)."
    }
    if ($total -eq 0 -and $AllowEmptyRun) {
        Write-Host "Прогон пуст (0 тестов) — допустимо для -AllowEmptyRun (issue #21)."
    }
    if ($failed -gt 0) {
        Write-Host "Тесты завершились с ошибками: failed=$failed"
    }
    Write-Host "Прогон: total=$total, failed=$failed"
}
elseif ($ResultsPath) {
    throw "Файл результатов не создан: $ResultsPath"
}

exit $vstestExit
