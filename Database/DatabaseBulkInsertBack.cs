using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;

namespace Primo.MIA
{
    /// <summary>Активность для массовой записи DataTable в БД.</summary>
    public class DatabaseBulkInsertBack : PrimoComponentTO<DatabaseBulkInsert>
    {
        public override string GroupName { get => ActivityCategories.Database; protected set { } }

        protected override int sdkTimeOut
        {
            get
            {
                int seconds;
                return int.TryParse(Prop_BulkCopyTimeoutSeconds, out seconds) && seconds > 0
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

        private string _propColumnMappings;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters), System.ComponentModel.DisplayName(ActivityStrings.Field_ColumnMappings)]
        public string Prop_ColumnMappings
        {
            get => _propColumnMappings;
            set { _propColumnMappings = value; InvokePropertyChanged(this, nameof(Prop_ColumnMappings)); }
        }

        private string _propBatchSize = "1000";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_BatchSize)]
        public string Prop_BatchSize
        {
            get => _propBatchSize;
            set { _propBatchSize = value; InvokePropertyChanged(this, nameof(Prop_BatchSize)); }
        }

        private string _propBulkCopyTimeoutSeconds = "60";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_BulkCopyTimeoutSeconds)]
        public string Prop_BulkCopyTimeoutSeconds
        {
            get => _propBulkCopyTimeoutSeconds;
            set { _propBulkCopyTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_BulkCopyTimeoutSeconds)); }
        }

        private bool _propUseTableLock = true;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_UseTableLock)]
        public bool Prop_UseTableLock
        {
            get => _propUseTableLock;
            set { _propUseTableLock = value; InvokePropertyChanged(this, nameof(Prop_UseTableLock)); }
        }

        private bool _propKeepIdentity = false;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_KeepIdentity)]
        public bool Prop_KeepIdentity
        {
            get => _propKeepIdentity;
            set { _propKeepIdentity = value; InvokePropertyChanged(this, nameof(Prop_KeepIdentity)); }
        }

        private DatabaseBulkPreloadMode _propPreloadMode = DatabaseBulkPreloadMode.None;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_PreloadMode)]
        public DatabaseBulkPreloadMode Prop_PreloadMode
        {
            get => _propPreloadMode;
            set { _propPreloadMode = value; InvokePropertyChanged(this, nameof(Prop_PreloadMode)); }
        }

        private string _propRowsWritten;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_RowsWritten)]
        public string Prop_RowsWritten
        {
            get => _propRowsWritten;
            set { _propRowsWritten = value; InvokePropertyChanged(this, nameof(Prop_RowsWritten)); }
        }

        private string _propMappingCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MappingCount)]
        public string Prop_MappingCount
        {
            get => _propMappingCount;
            set { _propMappingCount = value; InvokePropertyChanged(this, nameof(Prop_MappingCount)); }
        }

        private string _propWriteMode;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_WriteMode)]
        public string Prop_WriteMode
        {
            get => _propWriteMode;
            set { _propWriteMode = value; InvokePropertyChanged(this, nameof(Prop_WriteMode)); }
        }

        public DatabaseBulkInsertBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DatabaseBulkInsert;
            sdkComponentHelp =
                "Массово записывает DataTable в БД.\n\n" +
                "Режимы:\n" +
                "  SQL Server (System.Data.SqlClient) -> SqlBulkCopy\n" +
                "  Остальные провайдеры              -> batched insert через DbProviderFactory\n\n" +
                "Если ColumnMappings не указаны, колонки маппятся по одинаковым именам.";
            sdkComponentIcon = ActivityIcons.Base;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<DataTable>("Prop_DataTable", "Исходная таблица данных для массовой записи"),
                PropertyBuilder.String("Prop_ProviderInvariantName", "ADO.NET provider invariant name"),
                PropertyBuilder.String("Prop_ConnectionString", "Строка подключения к БД"),
                PropertyBuilder.String("Prop_TransactionId", "ID транзакции (опционально)"),
                PropertyBuilder.String("Prop_DestinationTable", "Имя таблицы-приёмника"),
                PropertyBuilder.Script<Dictionary<string, string>>("Prop_ColumnMappings", "Словарь SourceColumn -> DestinationColumn"),
                PropertyBuilder.Int("Prop_BatchSize", "Размер пакета записи"),
                PropertyBuilder.Int("Prop_BulkCopyTimeoutSeconds", "Таймаут массовой записи в секундах"),
                PropertyBuilder.BooleanObject("Prop_UseTableLock", "Использовать table lock (актуально для SqlBulkCopy)"),
                PropertyBuilder.BooleanObject("Prop_KeepIdentity", "Сохранять значения identity (актуально для SqlBulkCopy)"),
                PropertyBuilder.Enum<DatabaseBulkPreloadMode>("Prop_PreloadMode", "Предварительная очистка таблицы перед загрузкой"),
                PropertyBuilder.Variable<int>("Prop_RowsWritten", "Количество записанных строк"),
                PropertyBuilder.Variable<int>("Prop_MappingCount", "Количество использованных маппингов колонок"),
                PropertyBuilder.Variable<string>("Prop_WriteMode", "Фактически использованный режим записи")
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
                var columnMappings = GetPropertyValue<Dictionary<string, string>>(Prop_ColumnMappings, nameof(Prop_ColumnMappings), sd);
                var batchSize = ParseIntOrDefault(GetPropertyValue<string>(Prop_BatchSize, nameof(Prop_BatchSize), sd), 1000);
                var timeout = ParseIntOrDefault(GetPropertyValue<string>(Prop_BulkCopyTimeoutSeconds, nameof(Prop_BulkCopyTimeoutSeconds), sd), 60);

                var transactionId = DatabaseTransactionResolver.ResolveOptional(explicitTransactionId);
                var transactionHandle = DatabaseTransactionManager.Get(transactionId);
                var writeResult = transactionHandle != null
                    ? DatabaseHelper.ExecuteBulkInsert(transactionHandle, dataTable, destinationTable, batchSize, timeout, Prop_UseTableLock, Prop_KeepIdentity, Prop_PreloadMode, columnMappings)
                    : DatabaseHelper.ExecuteBulkInsert(provider, connectionString, dataTable, destinationTable, batchSize, timeout, Prop_UseTableLock, Prop_KeepIdentity, Prop_PreloadMode, columnMappings);

                var mappingCount = columnMappings != null && columnMappings.Count > 0
                    ? columnMappings.Count
                    : dataTable.Columns.Count;

                SetVariableValue(Prop_RowsWritten, writeResult.RowsWritten, sd);
                SetVariableValue(Prop_MappingCount, mappingCount, sd);
                SetVariableValue(Prop_WriteMode, writeResult.Mode ?? string.Empty, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Массовая запись завершена. Записано строк: {writeResult.RowsWritten}. Режим: {writeResult.Mode}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка массовой записи в БД: {ex.Message}"
                };
            }
        }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            result.ValidateRequired(Prop_DataTable, ActivityStrings.Field_DataTable, ActivityStrings.Error_DataTableRequired);
            if (string.IsNullOrWhiteSpace(Prop_TransactionId) && string.IsNullOrWhiteSpace(DatabaseTransactionResolver.ResolveOptional(null)))
                result.ValidateRequired(Prop_ConnectionString, ActivityStrings.Field_ConnectionString, ActivityStrings.Error_ConnectionStringRequired);
            result.ValidateRequired(Prop_DestinationTable, ActivityStrings.Field_DestinationTable, ActivityStrings.Error_DestinationTableRequired);
            return result;
        }

        private static int ParseIntOrDefault(string value, int defaultValue)
        {
            int parsed;
            return int.TryParse(value, out parsed) && parsed > 0 ? parsed : defaultValue;
        }
    }
}
