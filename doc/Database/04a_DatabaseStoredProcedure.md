# БД: Stored procedure

> **Группа:** MIA → База данных
> **Класс:** `DatabaseStoredProcedureBack`

## Назначение

Вызывает stored procedure через ADO.NET с поддержкой:

- input-параметров
- inputoutput-параметров
- output-параметров
- `return value`
- выполнения внутри DB-транзакции

## Входные параметры

| Параметр | Тип | Описание |
|---|---|---|
| `Provider invariant name` | `string` | ADO.NET provider |
| `Строка подключения` | `string` | Connection string |
| `ID транзакции` | `string` | Необязательный transactionId |
| `SQL / имя процедуры` | `string` | Имя stored procedure |
| `Параметры` | `Dictionary<string,string>` | Входные параметры |
| `InputOutput параметры` | `Dictionary<string,string>` | Параметры `InputOutput` с начальными значениями |
| `Выходные параметры` | `List<string>` | Имена output-параметров |
| `Получать return value` | `bool` | Добавлять параметр `ReturnValue` |
| `Размер output-параметров` | `int` | Размер для `Output` и `InputOutput` параметров |
| `Таймаут команды (сек)` | `int` | Таймаут выполнения |

## Выходные параметры

| Параметр | Тип | Описание |
|---|---|---|
| `Выходные значения` | `Dictionary<string,string>` | Значения output-параметров после выполнения |
| `Return value` | `string` | Значение `return` из stored procedure |
| `Затронуто строк` | `int` | Значение `ExecuteNonQuery()` |

## Примечания

- `InputOutput параметры` после выполнения попадают в общий словарь `Выходные значения`
- если имя параметра указано без префикса, он добавляется автоматически: `@` или `:`

## Пример

```text
Provider invariant name: "System.Data.SqlClient"
SQL / имя процедуры: "dbo.ProcessDocument"
Параметры:
{
  "DocumentId": "1001",
  "UserName": "robot"
}
Выходные параметры:
new List<string> { "StatusCode", "StatusMessage" }
```
