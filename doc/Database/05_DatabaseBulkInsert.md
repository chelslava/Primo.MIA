# БД: Массовая запись из DataTable

> **Группа:** MIA → База данных
> **Класс:** `DatabaseBulkInsertBack`

## Назначение

Массово записывает `DataTable` в БД.

Активность сама выбирает режим:
- `SqlBulkCopy` для `System.Data.SqlClient`
- batched insert через `DbProviderFactory` и транзакцию для остальных провайдеров

## Ограничения

- Таблица-приёмник должна уже существовать в БД
- Типы и имена колонок должны быть совместимы с таблицей-приёмником
- Для SQL Server опции `table lock` и `keep identity` работают только в fast path через `SqlBulkCopy`

## Входные параметры

| Параметр | Тип | Описание |
|---|---|---|
| `Таблица данных` | `DataTable` | Источник строк для загрузки |
| `Provider invariant name` | `string` | Например `System.Data.SqlClient`, `Npgsql`, `MySql.Data.MySqlClient`, `Oracle.ManagedDataAccess.Client` |
| `Строка подключения` | `string` | Connection string выбранной БД |
| `Таблица-приёмник` | `string` | Например `dbo.UsersImport` |
| `Маппинг колонок` | `Dictionary<string,string>` | `SourceColumn -> DestinationColumn`, необязательно |
| `Размер пакета` | `int` | Batch size, по умолчанию `1000` |
| `Таймаут bulk copy (сек)` | `int` | Таймаут операции записи |
| `Использовать table lock` | `bool` | Используется только в SQL Server fast path |
| `Сохранять identity` | `bool` | Используется только в SQL Server fast path |
| `Очистка перед загрузкой` | `DatabaseBulkPreloadMode` | `None`, `DeleteAll` или `Truncate` перед вставкой |

## Выходные параметры

| Параметр | Тип | Описание |
|---|---|---|
| `Записано строк` | `int` | Сколько строк отправлено в БД |
| `Количество маппингов` | `int` | Число применённых сопоставлений колонок |
| `Режим записи` | `string` | Например `SqlBulkCopy` или `BatchedInsert` |

## Очистка перед загрузкой

- `None` — загрузка поверх существующих данных
- `DeleteAll` — выполняется `DELETE FROM <таблица>`
- `Truncate` — выполняется `TRUNCATE TABLE <таблица>`

Если выбрана очистка, `Режим записи` будет содержать комбинированное значение, например `SqlBulkCopy+Truncate` или `BatchedInsert+DeleteAll`.

## Поведение маппинга

Если `Маппинг колонок` не указан, активность автоматически маппит колонки по одинаковым именам.

Пример:

```text
DataTable колонка: FullName
Таблица БД колонка: full_name

Маппинг:
{
  "FullName": "full_name"
}
```

## Замечания по совместимости

- Для SQL Server запись идёт максимально быстро через нативный bulk API.
- Для остальных БД используется параметризованный `INSERT` в одной транзакции с переиспользованием команды.
- Для `Odbc` и `OleDb` используются позиционные параметры `?`.
