# ReadTomlConfig — Чтение TOML конфигурации

Активность для универсального чтения конфигурационных файлов формата [TOML](https://toml.io/en/) в процессах Primo RPA. Реализована на базе библиотеки **Tomlyn**.

---

## Установка зависимости

Добавьте в `.csproj` проекта:

```xml
<PropertyGroup>
  <!-- Обязательно для .NET Framework — принудительно использует netstandard2.0 сборку -->
  <AssetTargetFallback>netstandard2.0</AssetTargetFallback>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Tomlyn" Version="0.9.1" />
</ItemGroup>
```

> **Важно — совместимость версий Tomlyn:**
>
> | Версия | Работает на .NET Framework 4.6.1 | Причина |
> |--------|----------------------|---------|
> | **0.9.1** | ✅ Работает | Проверено, содержит `netstandard2.0` |
> | 0.10.1 | ⚠️ Только с `AssetTargetFallback` | NuGet выбирает `net6.0` |
> | 0.16.2 | ⚠️ Только с `AssetTargetFallback` | NuGet выбирает `net6.0` |
> | 0.17.0–0.20.0 | ✅ Работает | Содержат `netstandard2.0` |
> | **2.4.0** | ✅ Работает (текущая) | API v2, `TomlSerializer.Deserialize<TomlTable>()` |
>
> Для версий 0.17.0+ Tomlyn продолжает поставлять `netstandard2.0` сборку.
> Начиная с v1.0.0 (март 2026) API изменён: `Toml.ToModel()` → `TomlSerializer.Deserialize()`.
> Tomlyn v1+ поддерживает только TOML 1.1.

---

## Режимы работы

Режим выбирается через свойство **Режим чтения** (`TomlReadMode`):

| Режим | Описание |
|-------|----------|
| `SingleValue` | Читает одно значение по ключу с поддержкой вложенности через точку |
| `SectionToDictionary` | Читает все ключи указанной секции в `Dictionary<string, string>` |
| `FullFileToDictionary` | Читает весь файл в плоский словарь с составными ключами |
| `ReadProfile` | Мёржит секцию `[default]` с секцией выбранного профиля окружения |

---

## Входные параметры

### Основные

| Свойство | Тип | Обязательность | Описание |
|----------|-----|----------------|----------|
| **Путь к файлу** | `String` | Всегда | Полный путь к `.toml` файлу конфигурации |
| **Режим чтения** | `TomlReadMode` | Всегда | Способ чтения данных из файла |
| **Ключ (section.key)** | `String` | `SingleValue` | Путь к ключу через точку: `server.database.host` |
| **Секция** | `String` | `SectionToDictionary` | Имя секции, поддерживает вложенность: `app.database` |
| **Значение по умолчанию** | `String` | Нет | Возвращается если ключ не найден (только `SingleValue`) |
| **Ошибка если не найдено** | `Boolean` | Нет | Если `true` — выбросить исключение при ненайденном ключе/профиле. По умолчанию `false` |

### Файл

| Свойство | Тип | По умолчанию | Описание |
|----------|-----|--------------|----------|
| **Кодировка** | `String` | `UTF-8` | Кодировка файла. Например: `UTF-8`, `windows-1251` |

### Профиль _(только для режима `ReadProfile`)_

| Свойство | Тип | По умолчанию | Описание |
|----------|-----|--------------|----------|
| **Имя профиля** | `String` | — | Имя профиля окружения: `production`, `staging`, `development` |
| **Имя секции default** | `String` | `default` | Имя базовой секции с общими значениями |
| **Стратегия слияния** | `ProfileMergeStrategy` | `DefaultThenProfile` | Способ объединения default и профиля |
| **Включать вложенные секции** | `Boolean` | `true` | Рекурсивно мёржить подсекции `[production.database]` |

#### Стратегии слияния профилей (`ProfileMergeStrategy`)

| Стратегия | Описание |
|-----------|----------|
| `DefaultThenProfile` | Default как база → профиль перекрывает его ключи. Ключи только из default сохраняются. **Стандартный вариант** |
| `ProfileOnly` | Только секция профиля, default полностью игнорируется |
| `ProfileThenDefault` | Профиль как база → default добавляет только те ключи, которых нет в профиле |

---

## Выходные параметры

| Свойство | Тип | Режимы | Описание |
|----------|-----|--------|----------|
| **Строковое значение** | `String` | `SingleValue` | Прочитанное значение по ключу |
| **Словарь значений** | `Dictionary<string, string>` | `SectionToDictionary`, `FullFileToDictionary`, `ReadProfile` | Итоговый словарь ключ-значение |
| **Ключ/профиль найден** | `Boolean` | `SingleValue`, `ReadProfile` | Флаг: был ли найден ключ или профиль |
| **Кол-во ключей** | `Int32` | Все | Количество ключей в итоговом словаре |
| **Доступные профили** | `List<string>` | `ReadProfile` | Список всех секций верхнего уровня файла |
| **Ключей из профиля** | `Int32` | `ReadProfile` | Сколько ключей пришло из секции профиля |
| **Ключей из default** | `Int32` | `ReadProfile` | Сколько ключей пришло из секции default |

---

## Примеры использования

### Режим `SingleValue`

Файл `config.toml`:
```toml
[server]
host = "localhost"
port = 8080

[server.database]
host = "db.local"
port = 5432
```

Настройки активности:
```
Путь к файлу:  "C:\configs\config.toml"
Режим чтения:  SingleValue
Ключ:          "server.database.host"
```

Результат → `Строковое значение` = `"db.local"`, `Ключ найден` = `true`

---

### Режим `SectionToDictionary`

Файл `config.toml`:
```toml
[database]
host    = "localhost"
port    = 5432
name    = "mydb"
timeout = 30
```

Настройки активности:
```
Путь к файлу:  "C:\configs\config.toml"
Режим чтения:  SectionToDictionary
Секция:        "database"
```

Результат → `Словарь значений`:
```
host    → "localhost"
port    → "5432"
name    → "mydb"
timeout → "30"
```

---

### Режим `FullFileToDictionary`

Файл `config.toml`:
```toml
app_name = "MyApp"

[server]
host = "localhost"
port = 8080

[server.ssl]
enabled = true
cert    = "/etc/ssl/cert.pem"
```

Результат → `Словарь значений`:
```
app_name       → "MyApp"
server.host    → "localhost"
server.port    → "8080"
server.ssl.enabled → "true"
server.ssl.cert    → "/etc/ssl/cert.pem"
```

---

### Режим `ReadProfile`

Файл `config.toml`:
```toml
[default]
host        = "localhost"
port        = 5432
timeout     = 30
retry_count = 3
log_level   = "info"

[default.cache]
ttl       = 300
max_items = 1000

[production]
host      = "prod-db.company.com"
timeout   = 60
log_level = "warning"

[production.cache]
ttl = 60

[staging]
host = "staging-db.company.com"
```

#### Стратегия `DefaultThenProfile`, профиль `production`:

```
default:     host=localhost, port=5432, timeout=30, retry_count=3, log_level=info
production:  host=prod-db,             timeout=60,                 log_level=warning
─────────────────────────────────────────────────────────────────────────────────
Итог:        host=prod-db,  port=5432, timeout=60, retry_count=3, log_level=warning
             cache.ttl=60,  cache.max_items=1000
```

Результат → `Словарь значений`:
```
host             → "prod-db.company.com"
port             → "5432"              ← из default (нет в production)
timeout          → "60"                ← из production (перекрыл default)
retry_count      → "3"                 ← из default (нет в production)
log_level        → "warning"           ← из production (перекрыл default)
cache.ttl        → "60"               ← из production.cache
cache.max_items  → "1000"             ← из default.cache (нет в production.cache)
```

Статистика:
```
Ключей из профиля → 5  (host, timeout, log_level, cache.ttl + сама секция cache)
Ключей из default → 3  (port, retry_count, cache.max_items)
Доступные профили → ["default", "production", "staging"]
```

---

#### Стратегия `ProfileOnly`, профиль `staging`:

```
Итог: только то, что есть в [staging]
```

Результат → `Словарь значений`:
```
host → "staging-db.company.com"
```

---

## Поддерживаемые типы TOML

Все типы значений TOML корректно конвертируются в `string`:

| Тип TOML | Пример в файле | Результат в строке |
|----------|---------------|---------------------|
| Строка | `name = "MyApp"` | `MyApp` |
| Целое | `port = 5432` | `5432` |
| Дробное | `ratio = 1.5` | `1.5` |
| Булево | `enabled = true` | `true` |
| Дата-время | `created = 2025-01-15T10:30:00` | `2025-01-15T10:30:00.0000000` |
| Массив | `tags = ["a", "b", "c"]` | `[a, b, c]` |
| Inline-таблица | `point = {x = 1, y = 2}` | `{x=1, y=2}` |

---

## Обработка ошибок

| Ситуация | `Ошибка если не найдено = false` | `Ошибка если не найдено = true` |
|----------|----------------------------------|----------------------------------|
| Файл не найден | Исключение всегда | Исключение всегда |
| Ключ не найден (`SingleValue`) | Возвращает **Значение по умолчанию** | `KeyNotFoundException` |
| Секция не найдена (`SectionToDictionary`) | Возвращает пустой словарь | `KeyNotFoundException` |
| Профиль не найден (`ReadProfile`) | Возвращает словарь из `[default]` | `KeyNotFoundException` с перечислением доступных профилей |
| Ошибка синтаксиса TOML | `TomlException` с описанием строки ошибки | `TomlException` с описанием строки ошибки |

---

## Группа компонента

`MIA`