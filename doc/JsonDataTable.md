# JSON: DataTable конвертер

> **Группа:** MIA → Утилиты
> **Класс:** `JsonDataTableBack`
> **Без зависимостей:** использует только `System.Text.Json` (.NET 5+)

---

## Назначение

Конвертирует `DataTable` ↔ `JSON` в обе стороны без сторонних библиотек. Поддерживает выборку колонок, форматирование дат, автоматическое определение типов и три формата вывода.

---

## Направления конвертации

| Направление | Описание |
|-------------|----------|
| `DataTable → JSON` | Сериализация DataTable в JSON-строку |
| `JSON → DataTable` | Десериализация JSON в DataTable |

---

## Входные параметры

### Основные

| Параметр | Тип | Описание |
|----------|-----|----------|
| **Направление** | `JsonConvertDirection` | Направление конвертации |
| **DataTable (входная)** | `DataTable` | Исходная таблица (для DataTable → JSON) |
| **JSON (входной)** | `string` | JSON-строка (для JSON → DataTable) |

### Настройки (только DataTable → JSON)

| Параметр | Тип | По умолч. | Описание |
|----------|-----|-----------|----------|
| **Формат JSON** | `JsonTableFormat` | `ArrayOfObjects` | Формат вывода |
| **Колонки** | `string` | все | Список через запятую. Пример: `Id,Name,Amount` |
| **Формат дат** | `string` | `yyyy-MM-dd` | Формат DateTime-значений. Пример: `dd.MM.yyyy` |
| **NULL как** | `JsonNullMode` | `null` | Представление NULL-значений |
| **Форматировать** | `bool` | `false` | Добавить отступы в JSON (human-readable) |

### Настройки (только JSON → DataTable)

| Параметр | Тип | По умолч. | Описание |
|----------|-----|-----------|----------|
| **Колонки** | `string` | все | Какие колонки читать из JSON |
| **Определять типы колонок** | `bool` | `true` | Автоматически определять типы (int, double, bool, DateTime) |

---

## Выходные параметры

| Параметр | Тип | Направление | Описание |
|----------|-----|-------------|----------|
| **Результат (JSON)** | `string` | DataTable → JSON | Сериализованная таблица |
| **Результат (DataTable)** | `DataTable` | JSON → DataTable | Десериализованная таблица |
| **Строк** | `int` | оба | Количество строк в результате |

---

## Форматы JSON

### ArrayOfObjects (по умолчанию)

Совместим с большинством REST API.

```json
[
  {"Id": 1, "Name": "Иван", "Amount": 1500.00},
  {"Id": 2, "Name": "Мария", "Amount": 2300.50}
]
```

### ArrayOfArrays

Компактный формат без имён колонок. Подходит для Chart.js и других библиотек визуализации.

```json
[[1, "Иван", 1500.00], [2, "Мария", 2300.50]]
```

### WithHeaders

Явная структура с отдельным описанием колонок.

```json
{
  "columns": ["Id", "Name", "Amount"],
  "rows": [
    [1, "Иван", 1500.00],
    [2, "Мария", 2300.50]
  ]
}
```

---

## NULL-значения

| Режим | Результат для NULL/DBNull |
|-------|--------------------------|
| `null` | `null` |
| `""` | Пустая строка |
| `"0"` | Строка "0" |
| `Skip` | Поле пропускается |

---

## Примеры использования

### Пример 1: DataTable → JSON для REST API

```
Входные данные:
  - DataTable: myDataTable (Id, Name, Email)
  - Направление: DataTableToJson
  - Формат: ArrayOfObjects

Результат:
  [
    {"Id": 1, "Name": "Иван", "Email": "ivan@example.com"},
    {"Id": 2, "Name": "Мария", "Email": "maria@example.com"}
  ]
```

### Пример 2: DataTable → JSON для Chart.js

```
Входные данные:
  - DataTable: myDataTable
  - Направление: DataTableToJson
  - Формат: ArrayOfArrays
  - NULL как: Skip

Результат:
  [[1, "Иван"], [2, "Мария"]]
```

### Пример 3: JSON → DataTable с автоопределением типов

```
Входные данные:
  - JSON: '[{"Id":1,"Name":"Иван","Active":true},{"Id":2,"Name":"Мария","Active":false}]'
  - Направление: JsonToDataTable
  - Определять типы колонок: true

Результат:
  DataTable с колонками:
    Id (Int32)     — 1, 2
    Name (String)  — "Иван", "Мария"
    Active (Boolean) — true, false
```

### Пример 4: JSON → DataTable из WithHeaders

```
Входные данные:
  - JSON: '{"columns":["Name","Score"],"rows":[["Иван",95],["Мария",87]]}'
  - Направление: JsonToDataTable
  - Формат: WithHeaders
  - Определять типы колонок: false

Результат:
  DataTable с колонками Name, Score (все тип string)
```

### Пример 5: Выборка колонок

```
Входные данные:
  - DataTable: fullTable (Id, Name, Email, Password, CreatedAt)
  - Направление: DataTableToJson
  - Колонки: "Id,Name,Email"  (Password и CreatedAt исключены)

Результат:
  [{"Id":1,"Name":"Иван","Email":"..."}, ...]
```

---

## Поддерживаемые типы колонок

### Сериализация (DataTable → JSON)

| Тип C# | JSON-результат |
|--------|----------------|
| `int`, `long` | Число |
| `double`, `float`, `decimal` | Число |
| `bool` | `true` / `false` |
| `DateTime` | Строка в указанном формате |
| `byte[]` | Base64-строка |
| `string` | Строка (экранирование) |
| `null` / `DBNull` | По выбору пользователя |

### Десериализация (JSON → DataTable)

Автоопределение типов (когда включено):

| JSON-значение | Тип колонки |
|---------------|-------------|
| `1`, `2` (целые) | `Int32` |
| `9223372036854775807` | `Int64` |
| `3.14` | `Double` / `Decimal` |
| `true`, `false` | `Boolean` |
| `"2025-01-15"` | `DateTime` |
| Любая строка | `String` |

---

## Алгоритм работы

### DataTable → JSON

```
1. Получить DataTable из переменной
2. Определить рабочий набор колонок (фильтр по списку)
3. Выбрать формат сериализации
4. Записать данные через Utf8JsonWriter в MemoryStream
5. Вернуть JSON-строку
```

### JSON → DataTable

```
1. Получить JSON из переменной
2. Распарсить через JsonDocument
3. Определить формат (WithHeaders / ArrayOf...)
4. Создать колонки с типами (если TypeInference включён)
5. Заполнить строки
6. Вернуть DataTable
```

---

## Особенности реализации

- **Без зависимостей:** использует только `System.Text.Json`
- **Utf8JsonWriter:** эффективная запись без промежуточных строк
- **HashSet для колонок:** O(1) проверка включения колонки
- **LINQ:** вся фильтрация через LINQ-выражения
- **TypeInference:** приоритет типов: `int → long → double → bool → DateTime → string`

---

## Обработка ошибок

| Ситуация | Результат |
|----------|-----------|
| DataTable не задана (DataTable → JSON) | Ошибка: "DataTable не задана" |
| JSON не задан (JSON → DataTable) | Ошибка: "JSON не задан" |
| Некорректный JSON | Ошибка: "Некорректный JSON: ..." |
| Колонки не найдены | Ошибка с перечислением доступных колонок |
| Неверный аргумент | Ошибка: "Неверный аргумент: ..." |

---

## Типовые сценарии

### 📡 Отправка DataTable в REST API

```
1. [База данных: Запрос] → dataTable
2. JSON: DataTable конвертер
   - Направление: DataTable → JSON
   - DataTable: dataTable
   - Формат: ArrayOfObjects
   → resultJson
3. HTTP: Запрос
   - URL: https://api.example.com/sync
   - Метод: POST
   - Тело: resultJson
```

### 📥 Получение JSON от API → DataTable

```
1. HTTP: Запрос
   - URL: https://api.example.com/users
   → responseJson
2. JSON: DataTable конвертер
   - Направление: JSON → DataTable
   - JSON: responseJson
   - Определять типы: true
   → resultTable
3. [Цикл по строкам] resultTable
```

### 📊 Подготовка данных для Chart.js

```
1. [Источник данных] → dataTable
2. JSON: DataTable конвертер
   - Направление: DataTable → JSON
   - DataTable: dataTable
   - Формат: ArrayOfArrays
   - NULL как: Skip
   → chartData
```

---

## Changelog

### Версия 1.0
- ✅ DataTable → JSON (ArrayOfObjects, ArrayOfArrays, WithHeaders)
- ✅ JSON → DataTable с автоопределением типов
- ✅ Выборка колонок
- ✅ Форматирование дат
- ✅ Настройка NULL-значений
- ✅ Форматированный вывод (отступы)
- ✅ Без сторонних зависимостей

---

**Автор:** ChelSlava
**Дата:** 2026-05-28
**Версия:** 1.0
