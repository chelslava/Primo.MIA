# Быстрый старт: Тестирование Primo.MIA

## Шаг 1: Сборка основного проекта

Проект использует MSBuild для XAML, поэтому нужно собрать его в Visual Studio:

1. Откройте `Primo.MIA.slnx` в Visual Studio
2. Выберите **Build → Build Solution** (или нажмите Ctrl+Shift+B)
3. Дождитесь успешной сборки

## Шаг 2: Запуск тестов

### Вариант A: Из командной строки

```powershell
cd Primo.MIA.Tests
dotnet test
```

### Вариант B: Из Visual Studio

1. Откройте **Test → Test Explorer**
2. Нажмите **Run All Tests**

## Шаг 3: Просмотр результатов

Вы должны увидеть:

```
✓ ListFilterLogicTests (15 тестов)
✓ DictionaryOperationsLogicTests (15 тестов)
✓ GeneratorLogicTests (10 тестов)
✓ ListTransformLogicTests (10 тестов)

Всего: 50 тестов
Пройдено: 50
Провалено: 0
```

## Что дальше?

- Добавьте тесты для остальных активностей
- Запустите с измерением покрытия: `.\run-tests-with-coverage.ps1`
- Прочитайте `RECOMMENDATIONS.md` для улучшения проекта

## Проблемы?

**Ошибка: "InitializeComponent не существует"**
→ Соберите проект в Visual Studio, не через `dotnet build`

**Ошибка: "Проект не найден"**
→ Убедитесь что находитесь в корневой папке проекта

**Тесты не запускаются**
→ Проверьте что установлен .NET Framework 4.6.2+
