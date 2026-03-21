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
        public string Prop_DataTable
        {
            get => _propDataTable;
            set { _propDataTable = value; InvokePropertyChanged(this, nameof(Prop_DataTable)); }
        }

        private string _propProviderInvariantName = "\"" + DatabaseHelper.DefaultProviderInvariantName + "\"";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_DbProviderInvariantName)]
        public string Prop_ProviderInvariantName
        {
            get => _propProviderInvariantName;
            set { _propProviderInvariantName = value; InvokePropertyChanged(this, nameof(Prop_ProviderInvariantName)); }
        }

        private string _propConnectionString;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_ConnectionString)]
        public string Prop_ConnectionString
        {
            get => _propConnectionString;
            set { _propConnectionString = value; InvokePropertyChanged(this, nameof(Prop_ConnectionString)); }
        }

        private string _propTransactionId;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_TransactionId)]
        public string Prop_TransactionId
        {
            get => _propTransactionId;
            set { _propTransactionId = value; InvokePropertyChanged(this, nameof(Prop_TransactionId)); }
        }

        private string _propDestinationTable;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_DestinationTable)]
        public string Prop_DestinationTable
        {
            get => _propDestinationTable;
            set { _propDestinationTable = value; InvokePropertyChanged(this, nameof(Prop_DestinationTable)); }
        }

        private string _propKeyColumns;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_KeyColumns)]
        public string Prop_KeyColumns
        {
            get => _propKeyColumns;
            set { _propKeyColumns = value; InvokePropertyChanged(this, nameof(Prop_KeyColumns)); }
        }

        private string _propUpdateColumns;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Optional), System.ComponentModel.DisplayName(ActivityStrings.Field_UpdateColumns)]
        public string Prop_UpdateColumns
        {
            get => _propUpdateColumns;
            set { _propUpdateColumns = value; InvokePropertyChanged(this, nameof(Prop_UpdateColumns)); }
        }

        private string _propColumnMappings;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters), System.ComponentModel.DisplayName(ActivityStrings.Field_ColumnMappings)]
        public string Prop_ColumnMappings
        {
            get => _propColumnMappings;
            set { _propColumnMappings = value; InvokePropertyChanged(this, nameof(Prop_ColumnMappings)); }
        }

        private string _propCommandTimeoutSeconds = "60";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CommandTimeoutSeconds)]
        public string Prop_CommandTimeoutSeconds
        {
            get => _propCommandTimeoutSeconds;
            set { _propCommandTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_CommandTimeoutSeconds)); }
        }

        private string _propInsertedCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_InsertedCount)]
        public string Prop_InsertedCount
        {
            get => _propInsertedCount;
            set { _propInsertedCount = value; InvokePropertyChanged(this, nameof(Prop_InsertedCount)); }
        }

        private string _propUpdatedCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_UpdatedCount)]
        public string Prop_UpdatedCount
        {
            get => _propUpdatedCount;
            set { _propUpdatedCount = value; InvokePropertyChanged(this, nameof(Prop_UpdatedCount)); }
        }

        public DatabaseUpsertBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DatabaseUpsert;
            sdkComponentHelp =
                "Выполняет key-based upsert из DataTable.\n" +
                "Сначала пытается UPDATE по ключевым колонкам, если строка не найдена — выполняет INSERT.";
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
                var transactionHandle = DatabaseTransactionManager.Get(transactionId);
                var result = transactionHandle != null
                    ? DatabaseHelper.ExecuteUpsert(transactionHandle, dataTable, destinationTable, keyColumns, updateColumns, columnMappings, timeout)
                    : DatabaseHelper.ExecuteUpsert(provider, connectionString, dataTable, destinationTable, keyColumns, updateColumns, columnMappings, timeout);

                SetVariableValue(Prop_InsertedCount, result.InsertedCount, sd);
                SetVariableValue(Prop_UpdatedCount, result.UpdatedCount, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Upsert завершён. Inserted: {result.InsertedCount}, Updated: {result.UpdatedCount}"
                };
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
