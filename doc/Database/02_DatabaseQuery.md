# БД: Запрос

> **Группа:** MIA → База данных
> **Класс:** `DatabaseQueryBack`
> **Тип результата:** `DataTable`

## Назначение

Выполняет SQL-запрос или stored procedure и возвращает табличный результат в `DataTable`.

## Входные параметры

| Параметр | Тип | Описание |
|---|---|---|
| `Provider invariant name` | `string` | ADO.NET provider |
| `Строка подключения` | `string` | Connection string |
| `Тип команды` | `DatabaseCommandType` | `Text` или `StoredProcedure` |
| `SQL / имя процедуры` | `string` | Текст запроса или имя процедуры |
| `Параметры` | `Dictionary<string,string>` | Необязательные параметры |
| `Таймаут команды (сек)` | `int` | Таймаут выполнения |

## Выходные параметры

| Параметр | Тип | Описание |
|---|---|---|
| `Таблица-результат` | `DataTable` | Результат выборки |
| `RowCount` | `int` | Количество строк |
| `ColumnCount` | `int` | Количество столбцов |
| `Список колонок` | `List<string>` | Имена колонок в порядке результата |
| `Есть строки` | `bool` | Есть ли в результате хотя бы одна строка |

## Пример

```text
Provider invariant name: "System.Data.SqlClient"
Строка подключения: dbConn
Тип команды: Text
SQL / имя процедуры: "select top 100 * from dbo.Users where Status = @Status"
Параметры: dbParams
Таблица-результат: usersTable
```
