# БД: Выполнить команду

> **Группа:** MIA → База данных
> **Класс:** `DatabaseNonQueryBack`

## Назначение

Выполняет команды без табличного результата: `INSERT`, `UPDATE`, `DELETE`, DDL или stored procedure.

Для SQL-скриптов из нескольких batch активность может разбивать текст по строкам `GO` и выполнять части последовательно в одной транзакции.
Для обычного `INSERT` активность также может вернуть `last inserted id` через provider-specific SQL.

## Выходные параметры

| Параметр | Тип | Описание |
|---|---|---|
| `Затронуто строк` | `int` | Значение `ExecuteNonQuery()` |
| `Есть затронутые строки` | `bool` | Признак `affectedRows > 0` |
| `Количество batch` | `int` | Сколько частей SQL было выполнено |
| `Last inserted id` | `string` | Значение идентификатора после `INSERT`, если режим включён |

## Batch-режим

- Работает только для `CommandType = Text`
- Разделитель batch: строка, содержащая только `GO`
- Все batch выполняются последовательно в одной транзакции

## Last Inserted Id

- SQL Server: `SELECT SCOPE_IDENTITY()`
- PostgreSQL: `SELECT LASTVAL()`
- MySQL: `SELECT LAST_INSERT_ID()`
- SQLite: `SELECT last_insert_rowid()`
- batch-режим `GO` нельзя включать одновременно с получением identity
