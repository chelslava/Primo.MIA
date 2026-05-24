// =============================================================================
// DatabaseUpsertBack.cs — активность «Database: Upsert по ключу (Upsert)».
// Выполняет UPDATE или INSERT для каждой строки DataTable по заданным ключевым колонкам.
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;

namespace Primo.MIA
{
    /// <summary>Активность для key-based upsert из DataTable.</summary>
    public class DatabaseUpsertBack : PrimoComponentTO<DatabaseUpsert>
    {
        public override string GroupName { get => ActivityCategories.Database; protected set { } }

        protected override int sdkTimeOut
        {
            get
            {
                int seconds;
                return int.TryParse(Prop_CommandTimeoutSeconds, out seconds) && seconds > 0
                    ? seconds * 1000
                    : 60000;
            }
            set { }
        }

        private string _propDataTable;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_DataTable)]
        /// <summary>Исходная таблица данных DataTable, строки которой будут обработаны операцией upsert.</summary>
        public string Prop_DataTable
        {
            get => _propDataTable;
            set { _propDataTable = value; InvokePropertyChanged(this, nameof(Prop_DataTable)); }
        }

        private string _propProviderInvariantName = "\"" + DatabaseHelper.DefaultProviderInvariantName + "\"";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_DbProviderInvariantName)]
        /// <summary>Инвариантное имя ADO.NET провайдера (например, System.Data.SqlClient).</summary>
        public string Prop_ProviderInvariantName
        {
            get => _propProviderInvariantName;
            set { _propProviderInvariantName = value; InvokePropertyChanged(this, nameof(Prop_ProviderInvariantName)); }
        }

        private string _propConnectionString;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_ConnectionString)]
        /// <summary>Строка подключения к базе данных.</summary>
        public string Prop_ConnectionString
        {
            get => _propConnectionString;
            set { _propConnectionString = value; InvokePropertyChanged(this, nameof(Prop_ConnectionString)); }
        }

        private string _propTransactionId;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_TransactionId)]
        /// <summary>Идентификатор активной транзакции; если не задан, используется ambient-контекст или прямое соединение.</summary>
        public string Prop_TransactionId
        {
            get => _propTransactionId;
            set { _propTransactionId = value; InvokePropertyChanged(this, nameof(Prop_TransactionId)); }
        }

        private string _propDestinationTable;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_DestinationTable)]
        /// <summary>Имя целевой таблицы в базе данных для операции upsert.</summary>
        public string Prop_DestinationTable
        {
            get => _propDestinationTable;
            set { _propDestinationTable = value; InvokePropertyChanged(this, nameof(Prop_DestinationTable)); }
        }

        private string _propKeyColumns;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_KeyColumns)]
        /// <summary>Список ключевых колонок, по которым определяется существование строки (используются в условии WHERE).</summary>
        public string Prop_KeyColumns
        {
            get => _propKeyColumns;
            set { _propKeyColumns = value; InvokePropertyChanged(this, nameof(Prop_KeyColumns)); }
        }

        private string _propUpdateColumns;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Optional), System.ComponentModel.DisplayName(ActivityStrings.Field_UpdateColumns)]
        /// <summary>Список колонок, обновляемых при UPDATE. Если не задан, обновляются все не-ключевые колонки.</summary>
        public string Prop_UpdateColumns
        {
            get => _propUpdateColumns;
            set { _propUpdateColumns = value; InvokePropertyChanged(this, nameof(Prop_UpdateColumns)); }
        }

        private string _propColumnMappings;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters), System.ComponentModel.DisplayName(ActivityStrings.Field_ColumnMappings)]
        /// <summary>Словарь маппинга колонок: SourceColumn → DestinationColumn.</summary>
        public string Prop_ColumnMappings
        {
            get => _propColumnMappings;
            set { _propColumnMappings = value; InvokePropertyChanged(this, nameof(Prop_ColumnMappings)); }
        }

        private string _propCommandTimeoutSeconds = "60";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CommandTimeoutSeconds)]
        /// <summary>Таймаут выполнения каждой команды в секундах.</summary>
        public string Prop_CommandTimeoutSeconds
        {
            get => _propCommandTimeoutSeconds;
            set { _propCommandTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_CommandTimeoutSeconds)); }
        }

        private string _propInsertedCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_InsertedCount)]
        /// <summary>Количество строк, вставленных операцией INSERT.</summary>
        public string Prop_InsertedCount
        {
            get => _propInsertedCount;
            set { _propInsertedCount = value; InvokePropertyChanged(this, nameof(Prop_InsertedCount)); }
        }

        private string _propUpdatedCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_UpdatedCount)]
        /// <summary>Количество строк, обновлённых операцией UPDATE.</summary>
        public string Prop_UpdatedCount
        {
            get => _propUpdatedCount;
            set { _propUpdatedCount = value; InvokePropertyChanged(this, nameof(Prop_UpdatedCount)); }
        }

        public DatabaseUpsertBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DatabaseUpsert;
            sdkComponentHelp = @"Компонент ""Database: Upsert по ключу""
Для каждой строки DataTable выполняет UPDATE по ключевым колонкам, а если строка не найдена — INSERT. Работает построчно с поддержкой транзакций и маппинга колонок.

Основные:
DataTable*: [DataTable] Исходная таблица данных, строки которой обрабатываются операцией upsert.
Провайдер*: [String] Инвариантное имя ADO.NET провайдера (например, System.Data.SqlClient).
Строка подключения: [String] Строка подключения к БД. Обязательна, если не задан ID транзакции.
ID транзакции: [String] Идентификатор активной транзакции.
Таблица-приёмник*: [String] Имя целевой таблицы в базе данных.
Ключевые колонки*: [List<String>] Список колонок, по которым определяется существование строки (условие WHERE в UPDATE).

Дополнительные:
Колонки для обновления: [List<String>] Список колонок, обновляемых при UPDATE. Если не задан, обновляются все не-ключевые колонки.

Параметры:
Маппинг колонок: [Dictionary<String, String>] Соответствие колонок DataTable колонкам таблицы БД (SourceColumn → DestinationColumn).

Настройки:
Таймаут: [Int32] Таймаут выполнения каждой команды в секундах. По умолчанию 60.

Выходные данные:
Вставлено строк: [Int32] Количество строк, добавленных операцией INSERT.
Обновлено строк: [Int32] Количество строк, изменённых операцией UPDATE.";
            sdkComponentIcon = ActivityIcons.Base;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<DataTable>("Prop_DataTable", "Исходная таблица данных"),
                PropertyBuilder.String("Prop_ProviderInvariantName", "ADO.NET provider invariant name"),
                PropertyBuilder.String("Prop_ConnectionString", "Строка подключения к БД"),
                PropertyBuilder.String("Prop_TransactionId", "ID транзакции (опционально)"),
                PropertyBuilder.String("Prop_DestinationTable", "Имя таблицы-приёмника"),
                PropertyBuilder.Script<List<string>>("Prop_KeyColumns", "Ключевые колонки"),
                PropertyBuilder.Script<List<string>>("Prop_UpdateColumns", "Колонки для обновления (опционально)"),
                PropertyBuilder.Script<Dictionary<string, string>>("Prop_ColumnMappings", "Маппинг SourceColumn -> DestinationColumn"),
                PropertyBuilder.Int("Prop_CommandTimeoutSeconds", "Таймаут выполнения в секундах"),
                PropertyBuilder.Variable<int>("Prop_InsertedCount", "Сколько строк было вставлено"),
                PropertyBuilder.Variable<int>("Prop_UpdatedCount", "Сколько строк было обновлено")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var dataTable = (DataTable)GetPropertyValue<object>(Prop_DataTable, nameof(Prop_DataTable), sd);
                if (dataTable == null)
                    throw new InvalidOperationException(ActivityStrings.Error_DataTableRequired);

                var provider = GetPropertyValue<string>(Prop_ProviderInvariantName, nameof(Prop_ProviderInvariantName), sd);
                var connectionString = GetPropertyValue<string>(Prop_ConnectionString, nameof(Prop_ConnectionString), sd);
                var explicitTransactionId = GetPropertyValue<string>(Prop_TransactionId, nameof(Prop_TransactionId), sd);
                var destinationTable = GetPropertyValue<string>(Prop_DestinationTable, nameof(Prop_DestinationTable), sd);
                var keyColumns = GetPropertyValue<List<string>>(Prop_KeyColumns, nameof(Prop_KeyColumns), sd);
                var updateColumns = GetPropertyValue<List<string>>(Prop_UpdateColumns, nameof(Prop_UpdateColumns), sd);
                var columnMappings = GetPropertyValue<Dictionary<string, string>>(Prop_ColumnMappings, nameof(Prop_ColumnMappings), sd);
                var timeout = ParseIntOrDefault(GetPropertyValue<string>(Prop_CommandTimeoutSeconds, nameof(Prop_CommandTimeoutSeconds), sd), 60);

                var transactionId = DatabaseTransactionResolver.ResolveOptional(explicitTransactionId);

                var logic = new DatabaseBulkLogic();
                var upsertResult = logic.Upsert(provider, connectionString, transactionId, dataTable, destinationTable, keyColumns, updateColumns, columnMappings, timeout);

                SetVariableValue(Prop_InsertedCount, upsertResult.InsertedCount, sd);
                SetVariableValue(Prop_UpdatedCount, upsertResult.UpdatedCount, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Upsert завершён. Inserted: {upsertResult.InsertedCount}, Updated: {upsertResult.UpdatedCount}"
                };
            }
            catch (System.Data.Common.DbException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка БД: {ex.Message}" };
            }
            catch (InvalidOperationException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Недопустимая операция: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка upsert в БД: {ex.Message}"
                };
            }
        }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            result.ValidateRequired(Prop_DataTable, ActivityStrings.Field_DataTable, ActivityStrings.Error_DataTableRequired);
            result.ValidateRequired(Prop_DestinationTable, ActivityStrings.Field_DestinationTable, ActivityStrings.Error_DestinationTableRequired);
            result.ValidateRequired(Prop_KeyColumns, ActivityStrings.Field_KeyColumns, "Список ключевых колонок обязателен");
            if (string.IsNullOrWhiteSpace(Prop_TransactionId) && string.IsNullOrWhiteSpace(DatabaseTransactionResolver.ResolveOptional(null)))
                result.ValidateRequired(Prop_ConnectionString, ActivityStrings.Field_ConnectionString, ActivityStrings.Error_ConnectionStringRequired);
            return result;
        }

        private static int ParseIntOrDefault(string value, int defaultValue)
        {
            int parsed;
            return int.TryParse(value, out parsed) && parsed > 0 ? parsed : defaultValue;
        }
    }
}
