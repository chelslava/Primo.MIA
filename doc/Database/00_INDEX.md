# Документация: Библиотека активностей «База данных» (MIA)

> **Группа в студии:** MIA → База данных

Набор закрывает типовые RPA-сценарии работы с БД без удержания соединения между шагами.
Каждая активность сама открывает подключение, выполняет действие и закрывает его.

## Список активностей

| # | Активность | Класс | Назначение |
|---|---|---|---|
| 01 | [БД: Проверить подключение](01_DatabaseCheckConnection.md) | `DatabaseCheckConnectionBack` | Проверить доступность БД и получить версию сервера |
| 02 | [БД: Запрос](02_DatabaseQuery.md) | `DatabaseQueryBack` | Выполнить `SELECT` или stored procedure и вернуть `DataTable` |
| 03 | [БД: Скалярное значение](03_DatabaseScalar.md) | `DatabaseScalarBack` | Получить одно значение: `COUNT(*)`, `MAX(...)`, `SCOPE_IDENTITY()` и т.п. |
| 04 | [БД: Выполнить команду](04_DatabaseNonQuery.md) | `DatabaseNonQueryBack` | Выполнить `INSERT/UPDATE/DELETE` и получить число затронутых строк |
| 05 | [БД: Массовая запись из DataTable](05_DatabaseBulkInsert.md) | `DatabaseBulkInsertBack` | Массово записать `DataTable` в любую БД: `SqlBulkCopy` для SQL Server или batched insert для остальных |
| 06 | [БД: Начать транзакцию](06_DatabaseTransactionBegin.md) | `DatabaseTransactionBeginBack` | Открыть транзакцию и сделать её текущим DB-контекстом |
| 07 | [БД: Подтвердить транзакцию](07_DatabaseTransactionCommit.md) | `DatabaseTransactionCommitBack` | Зафиксировать текущую или указанную транзакцию |
| 08 | [БД: Откатить транзакцию](08_DatabaseTransactionRollback.md) | `DatabaseTransactionRollbackBack` | Откатить текущую или указанную транзакцию |
| 09 | [БД: Проверить таблицу](09_DatabaseTableExists.md) | `DatabaseTableExistsBack` | Проверить, существует ли таблица или view |
| 10 | [БД: Список таблиц](10_DatabaseListTables.md) | `DatabaseListTablesBack` | Получить список таблиц базы данных |
| 11 | [БД: Список колонок](11_DatabaseListColumns.md) | `DatabaseListColumnsBack` | Получить список колонок выбранной таблицы |
| 12 | [БД: Схема таблицы](12_DatabaseGetTableSchema.md) | `DatabaseGetTableSchemaBack` | Вернуть metadata-таблицу по колонкам выбранной таблицы |

## Общие принципы

1. Провайдер задаётся через `Provider invariant name`, по умолчанию используется `System.Data.SqlClient`.
2. Все команды поддерживают `Text` и `StoredProcedure`.
3. Параметры передаются через `Dictionary<string, string>`.
4. Если имя параметра указано без префикса, код автоматически добавляет `@`.
5. Пустое значение параметра преобразуется в `DBNull.Value`.
6. Массовая запись автоматически выбирает оптимальный режим: `SqlBulkCopy` для SQL Server и универсальный batched insert для остальных провайдеров.
7. `Query`, `Scalar`, `NonQuery` и `BulkInsert` могут выполняться внутри открытой транзакции через `transactionId` или ambient-контекст.
8. `Scalar` умеет отдавать типизированные выходы, `Query` возвращает список колонок, `NonQuery` умеет исполнять SQL-скрипты по `GO` batch, а `BulkInsert` поддерживает предварительную очистку таблицы.
