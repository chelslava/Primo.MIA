# Primo.MIA.JsonHelpers

Ѓиблиотека длЯ преобразованиЯ JSON данных в объекты .NET, такие как DataTable, словари и списки. Џоддерживает как простые JSON структуры, так и структурированные данные с метаданными.

## ?? Џространство имен

```csharp
using Primo.MIA;
```

## ?? Њетоды

### 1. `JTokenToDataTable`
Џреобразует JToken в DataTable без метаданных о типах.

**Џараметры:**
- `token` - JToken длЯ конвертации

**‚озвращает:**
- `DataTable` - таблица с данными, все колонки типа `object`

**Џоддерживаемые структуры JSON:**
- Њассив объектов
- Ћдиночный объект

**Џример:**
```csharp
string json = @"[{""Id"":1,""Name"":""John""}]";
JToken token = JToken.Parse(json);
DataTable table = JsonHelpers.JTokenToDataTable(token);
```

### 2. `JTokenToStructuredDataTable`
Џреобразует структурированный JSON с метаданными в DataTable с правильными типами данных.

**Џараметры:**
- `token` - JToken с метаданными

**‚озвращает:**
- `DataTable` - таблица с правильными типами колонок

**‘труктура JSON:**
```json
{
  "TableName": "TableName",
  "Columns": [
    {"Name": "Column1", "Type": "Int32"},
    {"Name": "Column2", "Type": "String"}
  ],
  "Data": [
    {"Column1": 1, "Column2": "Value"}
  ]
}
```

**Џоддерживаемые типы данных:**
- `Int32`, `Int64`, `Long`
- `Decimal`, `Double`, `Float`
- `String`, `DateTime`, `Boolean`
- `Byte`, `Guid`

### 3. `JTokenToDictionary`
Џреобразует JToken в словарь string-object.

```json
{
  "name": "John",
  "age": 30,
  "isStudent": false,
  "hobbies": ["reading", "gaming", "swimming"],
  "address": {
    "city": "New York",
    "zip": "10001"
  }
}
```

**Џараметры:**
- `token` - JToken длЯ конвертации

**‚озвращает:**
- `Dictionary<string, object>` - словарь с ключами из свойств JSON

### 4. `JTokenToList`
Џреобразует JToken в список словарей.

**Џараметры:**
- `token` - JToken длЯ конвертации

**‚озвращает:**
- `List<Dictionary<string, object>>` - список словарей

## ?? Џримеры использованиЯ

### Џример 1: Џростой JSON массив
```csharp
string json = @"[
  {""Id"":1,""Name"":""John"",""Age"":30},
  {""Id"":2,""Name"":""Jane"",""Age"":25}
]";

JToken token = JToken.Parse(json);
DataTable table = JsonHelpers.JTokenToDataTable(table);
// ‚се колонки будут типа object
```

### Џример 2: ‘труктурированный JSON с метаданными
```csharp
string json = @"{
  ""TableName"": ""Employees"",
  ""Columns"": [
    {""Name"":""Id"",""Type"":""Int32""},
    {""Name"":""Name"",""Type"":""String""},
    {""Name"":""Age"",""Type"":""Int32""},
    {""Name"":""Salary"",""Type"":""Decimal""}
  ],
  ""Data"": [
    {""Id"":1,""Name"":""John"",""Age"":30,""Salary"":50000.50},
    {""Id"":2,""Name"":""Jane"",""Age"":25,""Salary"":45000.75}
  ]
}";

JToken token = JToken.Parse(json);
DataTable table = JsonHelpers.JTokenToStructuredDataTable(token);
// Љолонки будут иметь правильные типы: int, string, int, decimal
```

### Џример 3: ђабота со словарЯми
```csharp
string json = @"{""Id"":1,""Name"":""John"",""IsActive"":true}";
JToken token = JToken.Parse(json);

// ‚ словарь
Dictionary<string, object> dict = JsonHelpers.JTokenToDictionary(token);

// ‚ список словарей
List<Dictionary<string, object>> list = JsonHelpers.JTokenToList(token);
```

## ? Ћсобенности

### Ћбработка типов данных
- **‘ метаданными**: ‘трогаЯ типизациЯ на основе описаниЯ колонок
- **Ѓез метаданных**: ‚се значениЯ сохранЯютсЯ как `object`
- **Null значениЯ**: Љорректно обрабатываютсЯ как `DBNull.Value` или `null`

### Ћбработка ошибок
- Џроверка входных параметров на `null`
- ‡ащита от некорректных структур JSON
- ђезервное преобразование в строку при ошибках типизации

### Џроизводительность
- **Ѓез метаданных**: Ѓыстрее, минимальные преобразованиЯ
- **‘ метаданными**: Њедленнее из-за проверок и преобразований типов

## ?? ђекомендации по использованию

### €спользуйте `JTokenToDataTable` когда:
- Ќет информации о типах данных
- Ќужна максимальнаЯ производительность
- ђаботаете с простыми JSON структурами

### €спользуйте `JTokenToStructuredDataTable` когда:
- …сть метаданные о типах колонок
- Ќужна строгаЯ типизациЯ данных
- ’ребуетсЯ интеграциЯ с типизированными системами

### €спользуйте `JTokenToDictionary`/`JTokenToList` когда:
- Ќужна гибкаЯ работа с данными
- ’ребуетсЯ быстрый доступ к значениЯм по ключам
- ђаботаете с динамическими структурами

## ?? ‡ависимости

- .NET Framework 4.5+ или .NET Core 2.0+
- Newtonsoft.Json 12.0+
- System.Data

## ?? ЋбратнаЯ совместимость

‚се методы обратно совместимы и могут работать с JSON структурами разных версий. Ћбработка ошибок обеспечивает стабильную работу даже с частично корректными данными.

