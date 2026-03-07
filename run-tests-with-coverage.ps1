# Скрипт для запуска тестов с покрытием кода

Write-Host "=== Primo.MIA Tests with Coverage ===" -ForegroundColor Cyan
Write-Host ""

# Проверка наличия coverlet
$coverletInstalled = dotnet tool list -g | Select-String "coverlet.console"

if (-not $coverletInstalled) {
    Write-Host "Установка coverlet.console..." -ForegroundColor Yellow
    dotnet tool install -g coverlet.console
}

# Переход в директорию тестов
Set-Location -Path "Primo.MIA.Tests"

# Сборка проекта
Write-Host "Сборка проекта..." -ForegroundColor Yellow
dotnet build --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Ошибка сборки!" -ForegroundColor Red
    Set-Location -Path ".."
    exit 1
}

Write-Host ""
Write-Host "Запуск тестов с измерением покрытия..." -ForegroundColor Yellow
Write-Host ""

# Запуск тестов с покрытием
dotnet test --no-build --configuration Release `
    --collect:"XPlat Code Coverage" `
    --results-directory:"./TestResults" `
    --verbosity normal

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✓ Тесты завершены!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Отчет о покрытии сохранен в: Primo.MIA.Tests/TestResults/" -ForegroundColor Cyan
    
    # Поиск файла покрытия
    $coverageFile = Get-ChildItem -Path "./TestResults" -Filter "coverage.cobertura.xml" -Recurse | Select-Object -First 1
    
    if ($coverageFile) {
        Write-Host "Файл покрытия: $($coverageFile.FullName)" -ForegroundColor Gray
    }
} else {
    Write-Host ""
    Write-Host "✗ Тесты провалились" -ForegroundColor Red
    Set-Location -Path ".."
    exit 1
}

# Возврат в корневую директорию
Set-Location -Path ".."

Write-Host ""
Write-Host "Для просмотра отчета установите ReportGenerator:" -ForegroundColor Yellow
Write-Host "  dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Gray
Write-Host ""
Write-Host "Затем сгенерируйте HTML отчет:" -ForegroundColor Yellow
Write-Host "  reportgenerator -reports:""Primo.MIA.Tests/TestResults/**/coverage.cobertura.xml"" -targetdir:""CoverageReport"" -reporttypes:Html" -ForegroundColor Gray
