# Database Module

**Generated:** 2026-05-23  
**Commit:** cb1269a2  
**Branch:** develop

## OVERVIEW

ADO.NET abstraction with provider-agnostic operations, transaction context pattern using ambient RepoDict stack, and bulk operations (SqlBulkCopy + generic fallback).

## STRUCTURE

```
Database/
├── *QueryBack.cs, *NonQueryBack.cs, *ScalarBack.cs, *StoredProcedureBack.cs
├── DatabaseBulkInsertBack.cs, DatabaseUpsertBack.cs
├── DatabaseTransaction{Begin,Commit,Rollback}Back.cs
├── DatabaseListTablesBack.cs, DatabaseListColumnsBack.cs, DatabaseGetTableSchemaBack.cs, DatabaseTableExistsBack.cs, DatabaseCheckConnectionBack.cs
├── DatabaseHelper.cs (2010 lines)
├── DatabaseTransactionManager.cs
├── DatabaseTransactionContext.cs
├── DatabaseTransactionHandle.cs
├── DatabaseTransactionResolver.cs
└── *.xaml + *.xaml.cs (15 XAML files)
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Core ADO.NET logic | `DatabaseHelper.cs` | Factory, connection, command, parameter normalization (@, :, ?) |
| Bulk operations | `DatabaseHelper.cs` + `DatabaseBulkInsertBack.cs` | SqlBulkCopy for SQL Server, batched insert fallback |
| Upsert logic | `DatabaseHelper.cs` | Key-based UPDATE/INSERT from DataTable |
| Transaction manager | `DatabaseTransactionManager.cs` | Thread-safe registry of open transactions |
| Ambient context | `DatabaseTransactionContext.cs` | RepoDict stack for implicit transaction propagation |
| Transaction resolution | `DatabaseTransactionResolver.cs` | Resolves explicit vs ambient transaction IDs |
| Activity backend | `*Back.cs` | Overrides `TimedAction()`, uses `Guard.*` validation |

## CONVENTIONS

- **Base class**: All activities inherit `PrimoComponentTO<T>`.
- **Provider-agnostic**: Use `DbProviderFactory` via `DatabaseHelper.GetFactory()`; support SQL Server, Oracle, ODBC, OleDB.
- **Property pattern**: `Prop_X` with `[StoringProperty]` and `InvokePropertyChanged()`.
- **Parameter normalization**: Handles `@param`, `:param`, `?param` prefixes automatically.
- **Transaction handling**: Activities accept optional `transactionId`; if empty, resolves ambient context via `DatabaseTransactionResolver`.
- **Validation**: `DatabaseHelper.OpenConnection()` checks connection string; `Guard.NotNullOrWhiteSpace()` for inputs.
- **Timeouts**: Custom `sdkTimeOut` overrides default; parse with `int.TryParse()` fallback.
- **Output properties**: Use variable storage (`Prop_*` naming) for results.

## ANTI-PATTERNS (THIS MODULE)

- **Exception re-throw**: Wrap ADO.NET exceptions in `InvalidOperationException` with provider name context (no bare `throw;`).
- **Mixed provider names**: Use `DatabaseHelper.DefaultProviderInvariantName` constant, not hard-coded strings.
- **Direct transaction disposal**: Always use `DatabaseTransactionManager.Commit/Rollback()` to ensure cleanup.
- **Ambiguous isolation levels**: Map via `DatabaseTransactionManager.MapIsolationLevel()` enum switch.
- **Leaked connections**: `DatabaseTransactionManager` tracks `DbConnection` in `DatabaseTransactionHandle`; dispose only via manager.
- **Bulk mode confusion**: `DatabaseBulkInsertBack` auto-selects write mode (SqlBulkCopy vs fallback); do not guess mode manually.
- **Missing parameter cleanup**: `DatabaseHelper.AddParameters()` reuses `DbCommand.CreateParameter()`; do not cache parameters across calls.
