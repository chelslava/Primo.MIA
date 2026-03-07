# Скрипт для запуска тестов

Write-Host "=== Primo.MIA Tests ===" -ForegroundColor Cyan
Write-Host ""

# Переход в директорию тестов
Set-Location -Path "Primo.MIA.Tests"

# Сборка проекта
Write-Host "Сборка тестового проекта..." -ForegroundColor Yellow
dotnet build --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Ошибка сборки!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Запуск тестов..." -ForegroundColor Yellow
Write-Host ""

# Запуск всех тестов
dotnet test --no-build --configuration Release --verbosity normal

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✓ Все тесты пройдены успешно!" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "✗ Некоторые тесты провалились" -ForegroundColor Red
    exit 1
}

# Возврат в корневую директорию
Set-Location -Path ".."
