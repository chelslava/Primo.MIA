# Primo.MIA

**Библиотека расширенных активностей для платформы Primo RPA**

[![CI](https://github.com/chelslava/Primo.MIA/actions/workflows/ci.yml/badge.svg)](https://github.com/chelslava/Primo.MIA/actions/workflows/ci.yml)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8.1-blue.svg)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

---

## 📋 Обзор

Primo.MIA — это набор из **~130 переиспользуемых активностей** для автоматизации бизнес-процессов в среде **Primo Studio**. Библиотека расширяет стандартные возможности Primo RPA дополнительными инструментами для работы с браузерами, базами данных, HTTP-запросами, коллекциями, файлами и другими сущностями.

Распространяется в виде NuGet-пакета и интегрируется автоматически через механизм рефлексии LTools SDK.

---

## 🎯 Модули активностей

| Модуль | Количество | Описание |
|--------|------------|----------|
| **Browser** | 30 | Автоматизация браузеров на Selenium WebDriver. Работа с элементами, вкладками, окнами, куки, localStorage, shadow DOM, iframe. |
| **Database (ADO.NET)** | 15 | Операции с реляционными БД через ADO.NET. Запросы, транзакции, пакетные операции, DataTable. |
| **Database (MongoDB)** | 6 | NoSQL операции с MongoDB. Запросы, вставка, обновление, удаление, агрегация. |
| **Database (Redis)** | 9 | Кэширование и очереди через Redis. Строки, хэши, списки, счётчики. |
| **HttpWeb** | 11 | HTTP/HTTPS запросы, OAuth2 авторизация, перезапуск токенов, вебхуки, батчевые запросы, ожидание условия (poll, для long-running API), пагинация REST ({page}/{cursor} → агрегация элементов). |
| **List** | 9 | Операции со списками: фильтрация, сортировка, группировка, трансформация, агрегация, срезы, множества. |
| **Dictionary** | 11 | CRUD операции со словарями: создание, фильтрация, слияние, сериализация в/из JSON. |
| **Tuple** | 9 | Операции с кортежами: создание, распаковка, сортировка, zip/unzip. |
| **Files** | 3 | Поиск файлов по маске, ожидание появления файла, очистка старых файлов. |
| **Text** | 5 | Парсинг текста по шаблонам, шаблонизация строк, извлечение сущностей, транслитерация, Regex (совпадения с группами в DataTable, замена с подстановками, ReDoS-таймаут). |
| **Calendar** | 5 | Загрузка и парсинг производственных календарей (CSV/JSON/XML/TXT). Подсчёт рабочих/выходных дней. |
| **Json** | 2 | Парсинг JSON и запросы по JsonPath. |
| **Xml** | 2 | Парсинг XML и запросы по XPath. |
| **Utilities** | 9 | Генераторы данных, логирование, DataTable ↔ HTML и фильтрация DataTable (SQL-подобные выражения и конструктор условий, сохранение порядка строк), перерасчёт ячеек Excel, чтение TOML конфигов, CSV ↔ DataTable (RFC 4180). |
| **Криптография** | 5 | Хэширование строки/файла (MD5/SHA), шифрование строк AES-256 (PBKDF2), Base64. |
| **SFTP** | 5 | Загрузка/скачивание/листинг/удаление по SFTP (SSH.NET). Аутентификация паролем или ключом, проверка host key. |
| **FTP** | 5 | Загрузка/скачивание/листинг/удаление/создание папки по FTPS — explicit TLS (FtpWebRequest, без зависимостей). Сертификаты — стандартная валидация (fail-closed). |
| **Очереди сообщений** | 2 | RabbitMQ: публикация и ожидание сообщений (persistent, TLS, AutoAck). |
| **Архивы** | 2 | Создание и распаковка ZIP (System.IO.Compression): файлы/папки/маски, уровни сжатия, политики перезаписи, защита от Zip Slip. Пароли/AES не поддерживаются стандартным .NET. |
| **JsonDataTable** | 1 | Конвертация DataTable ↔ JSON без зависимостей. Три формата, выборка колонок, автотипы. |

---

## 🔧 Системные требования

- **ОС**: Windows 10/11, Windows Server 2016+
- **Среда выполнения**: .NET Framework 4.8.1
- **Зависимости**: Primo Studio (LTools.*.dll) установленный по пути:
  - `C:\Program Files\Primo\Primo Studio Community x64\`
- **Тестирование**: .NET Framework 4.8.1 (xUnit + FluentAssertions)

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
| Selenium.WebDriver | 4.52.0 | Браузерная автоматизация |
| Selenium.Support | 4.52.0 | Вспомогательные классы Selenium |
| MongoDB.Driver | 2.30.0 | MongoDB (NoSQL) |
| StackExchange.Redis | 2.8.41 | Redis (кэш/очереди) |
| Npgsql | 7.0.8 | PostgreSQL (ADO.NET) |
| xUnit | 2.9.3 | Тестовый фреймворк |
| FluentAssertions | 7.2.0 | Утверждения в тестах (Apache-2.0) |

---

## ⚠️ Известные особенности

### Глобальное состояние
`RepoDict` — статический словарь для хранения контекста сессий (браузерных и др.). Используется паттерн **Ambient Context** через `BrowserSessionContext` для неявной передачи ID сессии между активностями.

Предполагается **один робот на процесс** (ограничение платформы Primo Studio): глобальный контекст не изолирован между параллельными прогонами в общем хосте.

### Жизненный цикл сессий браузера
Контейнер «Открыть браузер» регистрирует сессию в `BrowserSessionRegistry` (issue #20). При аварийном прерывании робота между «Открыть» и «Закрыть браузер» SDK не вызывает Quit() — поэтому заброшенные сессии (старше **24 ч**) автоматически закрываются и удаляются из `RepoDict` при регистрации следующей сессии (по образцу TTL транзакций БД). «Закрыть браузер» снимает из ambient-контекста конкретную сессию — `Pop(sessionId)`, а не вершину стека: вложенные/чередующиеся контейнеры не снимают записи друг друга.

### Только .NET Framework
Библиотека разработана под **.NET Framework 4.8.1** и не поддерживает .NET Core / .NET 5+.

### Зависимость от Primo SDK
Требует установки Primo Studio (LTools.*.dll не доступны через NuGet). В CI сборки SDK-сборки устанавливаются из приватного репозитория `chelslava/primo-ltools-sdk`.

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
├── Primo.MIA.sln
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
