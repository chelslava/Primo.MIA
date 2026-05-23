# Primo.MIA

**Библиотека расширенных активностей для платформы Primo RPA**

[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.6.1-blue.svg)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

---

## 📋 Обзор

Primo.MIA — это набор из **~90 переиспользуемых активностей** для автоматизации бизнес-процессов в среде **Primo Studio**. Библиотека расширяет стандартные возможности Primo RPA дополнительными инструментами для работы с браузерами, базами данных, HTTP-запросами, коллекциями, файлами и другими сущностями.

Распространяется в виде NuGet-пакета и интегрируется автоматически через механизм рефлексии LTools SDK.

---

## 🎯 Модули активностей

| Модуль | Количество | Описание |
|--------|------------|----------|
| **Browser** | 26 | Автоматизация браузеров на Selenium WebDriver. Работа с элементами, вкладками, окнами, куки, localStorage, shadow DOM, iframe. |
| **Database** | 16 | Операции с базами данных через ADO.NET. Запросы, транзакции, пакетные операции, DataTable. |
| **HttpWeb** | 8 | HTTP/HTTPS запросы, OAuth2 авторизация, перезапуск токенов, вебхуки, батчевые запросы (RestSharp). |
| **List** | 12 | Операции со списками: фильтрация, сортировка, группировка, трансформация, агрегация, срезы, множества. |
| **Dictionary** | 14 | CRUD операции со словарями: создание, фильтрация, слияние, сериализация в/из JSON. |
| **Tuple** | 12 | Операции с кортежами: создание, распаковка, сортировка, zip/unzip. |
| **Files** | 3 | Поиск файлов по маске, ожидание появления файла, очистка старых файлов. |
| **Text** | 3 | Парсинг текста по шаблонам, шаблонизация строк, транслитерация. |
| **Calendar** | 2 | Загрузка и парсинг производственных календарей (CSV/JSON/XML/TXT). Подсчёт рабочих/выходных дней. |
| **Json** | 2 | Парсинг JSON и запросы по JsonPath. |
| **Xml** | 2 | Парсинг XML и запросы по XPath. |
| **Utilities** | 6 | Генераторы данных, логирование, DataTable ↔ HTML, перерасчёт ячеек Excel, чтение TOML конфигов. |
| **JsonDataTable** | 1 | Преобразование JSON в DataTable (простой и структурированный режимы). |

---

## 🔧 Системные требования

- **ОС**: Windows 10/11, Windows Server 2016+
- **Среда выполнения**: .NET Framework 4.6.1
- **Зависимости**: Primo Studio (LTools.*.dll) установленный по пути:
  - `C:\Program Files\Primo\Primo Studio Community x64\`
- **Тестирование**: .NET Framework 4.6.2 (xUnit + FluentAssertions)

---

## 🚀 Установка

### Через NuGet (локально)

```bash
# Сборка генерирует .nupkg в bin\Debug\
msbuild Primo.MIA.csproj /p:Configuration=Debug

# Установите пакет в Primo Studio через локальный источник NuGet
```

### Интеграция в Primo Studio

1. Соберите проект или получите готовый `Primo.MIA.*.nupkg`
2. В Primo Studio → **Настройки** → **Управление зависимостями**
3. Добавьте локальный источник NuGet с папкой, содержащей `.nupkg`
4. Установите пакет **Primo.MIA**

Активности автоматически появятся в панели **Элементы** Primo Studio.

---

## 📖 Паттерны разработки

### Структура активности

Каждая активность следует шаблону **3 файла**:

```
BrowserOpen.xaml          # UI (XAML разметка)
BrowserOpen.xaml.cs       # Code-behind (привязка)
BrowserOpenBack.cs        # Логика выполнения
```

### Базовые классы

- `BrowserActivityBase<T>` → для браузерных активностей
- `HttpActivityBase<T>` → для HTTP активностей
- `PrimoComponentTO<T>` → для всех остальных

### Ключевые конвенции

```csharp
// Валидация входных параметров
Guard.ThrowIfNull(sd.Variables, nameof(sd.Variables));
Guard.ThrowIfNullOrEmpty(url, "URL");

// Свойства с сохранением
[StoringProperty]
public string Prop_Url
{
    get => _url;
    set => InvokePropertyChanged(ref _url, value);
}

// Переопределение TimedAction для логики
public override ExecutionResult TimedAction(IScriptData sd)
{
    // Получить входы из sd.Variables
    // Выполнить логику
    // Установить выход через SetVariableValue()
    return ExecutionResult.Success;
}
```

### XAML стили

Используйте централизованные стили из `Common/Styles.xaml`:

```xml
<TextBox Style="{StaticResource ActivityTextBox}"
         Text="{Binding Prop_Url}" />
```

Для видимости по enum:

```xml
Visibility="{Binding Prop_Method, Converter={StaticResource EnumToVisibilityConverter}, ConverterParameter='GET|POST'}"
```

---

## 🧪 Тестирование

Проект использует **xUnit + FluentAssertions** с паттерном AAA.

```bash
# Собрать тесты
msbuild Primo.MIA.Tests\Primo.MIA.Tests.csproj /p:Configuration=Debug

# Запустить (через Visual Studio или консоль)
scripts\run-tests-vs.ps1
```

Категории тестов:
- `"Fast"` — быстрые модульные тесты
- `"Integration"` — интеграционные (требуют внешних ресурсов)

---

## 📦 Зависимости (NuGet)

| Пакет | Версия | Назначение |
|-------|--------|------------|
| Newtonsoft.Json | 13.0.4 | JSON сериализация |
| Tomlyn | 2.4.0 | Парсинг TOML конфигов |
| Selenium.WebDriver | 4.21.0 | Браузерная автоматизация |
| Selenium.Support | 4.21.0 | Вспомогательные классы Selenium |
| Selenium.WebDriver.ChromeDriver | 125.0.6422.14100 | Chrome WebDriver |
| RestSharp | 105.2.3 | HTTP клиент |
| xUnit | 2.9.0 | Тестовый фреймворк |
| FluentAssertions | 6.12.0 | Утверждения в тестах |

---

## ⚠️ Известные особенности

### Глобальное состояние
`RepoDict` — статический словарь для хранения контекста сессий (браузерных и др.). Используется паттерн **Ambient Context** через `BrowserSessionContext` для неявной передачи ID сессии между активностями.

### Только .NET Framework
Библиотека разработана исключительно под **.NET Framework 4.6.1** и не поддерживает .NET Core / .NET 5+.

### Зависимость от Primo SDK
Требует установки Primo Studio (LTools.*.dll не доступны через NuGet).

### Русская локализация
- UI строки в XAML — на русском
- XML комментарии к публичным API — на русском
- Документация — на русском

---

## 📁 Структура проекта

```
Primo.MIA/
├── Browser/           # 26 браузерных активностей + ADR docs
├── Calendar/          # Производственные календари
├── Common/            # Общие утилиты (Guard, FileHelper, конвертеры, стили)
├── Database/          # 16 активностей для БД
├── Dictionary/        # Операции со словарями
├── Files/             # Поиск/ожидание/очистка файлов
├── HttpWeb/           # HTTP, OAuth2, вебхуки
├── Json/              # JSON парсинг + запросы
├── List/              # Операции со списками
├── Text/              # Парсинг, шаблоны, транслит
├── Tuple/             # Операции с кортежами
├── Utilities/         # Генераторы, лог, DataTable, Excel, TOML, JsonDataTable
├── Xml/               # XML парсинг + запросы
├── doc/               # Документация по активностям (Markdown)
├── Primo.MIA.Tests/   # xUnit тесты
├── packages/          # Локальные NuGet пакеты
├── Enums.cs           # 2055 строк перечислений
├── RepoDict.cs        # Глобальное хранилище контекста
├── Primo.MIA.csproj
├── Primo.MIA.slnx
└── README.md
```

---

## 📝 Лицензия

MIT License — подробности в файле лицензии.

---

## 🤝 Контрибьюция

1. Форкните репозиторий
2. Создайте feature ветку (`git checkout -b feature/amazing-feature`)
3. Коммитьте изменения (`git commit -m 'feat: Добавить какую-то функциональность'`)
4. Отправьте на GitHub (`git push origin feature/amazing-feature`)
5. Создайте Pull Request

---

**Primo.MIA** — расширяйте возможности вашей RPA-автоматизации.
