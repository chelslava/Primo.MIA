// =============================================================================
// DatabaseQueryBack.cs — активность «Database: Запрос данных (Query)».
// Выполняет SQL-запрос или хранимую процедуру и возвращает результат в DataTable.
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>Активность для чтения данных из БД в DataTable.</summary>
    public class DatabaseQueryBack : PrimoComponentTO<DatabaseQuery>
    {
        public override string GroupName { get => ActivityCategories.Database; protected set { } }

        protected override int sdkTimeOut
        {
            get
            {
                int seconds;
                return int.TryParse(Prop_CommandTimeoutSeconds, out seconds) && seconds > 0
                    ? seconds * 1000
                    : 30000;
            }
            set { }
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

        private DatabaseCommandType _propCommandType = DatabaseCommandType.Text;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_CommandType)]
        /// <summary>Тип команды: Text (SQL-запрос) или StoredProcedure.</summary>
        public DatabaseCommandType Prop_CommandType
        {
            get => _propCommandType;
            set { _propCommandType = value; InvokePropertyChanged(this, nameof(Prop_CommandType)); }
        }

        private string _propCommandText;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_CommandText)]
        /// <summary>Текст SQL-запроса или имя хранимой процедуры.</summary>
        public string Prop_CommandText
        {
            get => _propCommandText;
            set { _propCommandText = value; InvokePropertyChanged(this, nameof(Prop_CommandText)); }
        }

        private string _propParameters;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters), System.ComponentModel.DisplayName(ActivityStrings.Field_ParametersDictionary)]
        /// <summary>Словарь параметров команды (имя → значение).</summary>
        public string Prop_Parameters
        {
            get => _propParameters;
            set { _propParameters = value; InvokePropertyChanged(this, nameof(Prop_Parameters)); }
        }

        private string _propCommandTimeoutSeconds = "30";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CommandTimeoutSeconds)]
        /// <summary>Таймаут выполнения команды в секундах.</summary>
        public string Prop_CommandTimeoutSeconds
        {
            get => _propCommandTimeoutSeconds;
            set { _propCommandTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_CommandTimeoutSeconds)); }
        }

        private string _propResultTable;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ResultTable)]
        /// <summary>Результат запроса в виде DataTable.</summary>
        public string Prop_ResultTable
        {
            get => _propResultTable;
            set { _propResultTable = value; InvokePropertyChanged(this, nameof(Prop_ResultTable)); }
        }

        private string _propRowCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_RowCount)]
        /// <summary>Количество строк в результирующей таблице.</summary>
        public string Prop_RowCount
        {
            get => _propRowCount;
            set { _propRowCount = value; InvokePropertyChanged(this, nameof(Prop_RowCount)); }
        }

        private string _propColumnCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ColumnCount)]
        /// <summary>Количество столбцов в результирующей таблице.</summary>
        public string Prop_ColumnCount
        {
            get => _propColumnCount;
            set { _propColumnCount = value; InvokePropertyChanged(this, nameof(Prop_ColumnCount)); }
        }

        private string _propColumnNames;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ColumnNames)]
        /// <summary>Список имён столбцов результирующей таблицы.</summary>
        public string Prop_ColumnNames
        {
            get => _propColumnNames;
            set { _propColumnNames = value; InvokePropertyChanged(this, nameof(Prop_ColumnNames)); }
        }

        private string _propHasRows;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_HasRows)]
        /// <summary>Признак наличия строк в результирующей таблице.</summary>
        public string Prop_HasRows
        {
            get => _propHasRows;
            set { _propHasRows = value; InvokePropertyChanged(this, nameof(Prop_HasRows)); }
        }

        public DatabaseQueryBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DatabaseQuery;
            sdkComponentHelp =
                "Выполняет SQL-запрос или stored procedure и возвращает результат в DataTable.\n" +
                "Параметры передаются через Dictionary<string, string>.";
            sdkComponentIcon = ActivityIcons.Base;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ProviderInvariantName", "ADO.NET provider invariant name"),
                PropertyBuilder.String("Prop_ConnectionString", "Строка подключения к БД"),
                PropertyBuilder.String("Prop_TransactionId", "ID транзакции (опционально)"),
                PropertyBuilder.Enum<DatabaseCommandType>("Prop_CommandType", "Тип команды: Text или StoredProcedure"),
                PropertyBuilder.String("Prop_CommandText", "SQL текст или имя stored procedure"),
                PropertyBuilder.Script<Dictionary<string, string>>("Prop_Parameters", "Параметры команды"),
                PropertyBuilder.Int("Prop_CommandTimeoutSeconds", "Таймаут выполнения в секундах"),
                PropertyBuilder.Variable<DataTable>("Prop_ResultTable", "Результат запроса в виде DataTable"),
                PropertyBuilder.Variable<int>("Prop_RowCount", "Количество строк"),
                PropertyBuilder.Variable<int>("Prop_ColumnCount", "Количество столбцов"),
                PropertyBuilder.Variable<List<string>>("Prop_ColumnNames", "Список имён колонок"),
                PropertyBuilder.Variable<bool>("Prop_HasRows", "Есть ли строки в результате")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var provider = GetPropertyValue<string>(Prop_ProviderInvariantName, nameof(Prop_ProviderInvariantName), sd);
                var connectionString = GetPropertyValue<string>(Prop_ConnectionString, nameof(Prop_ConnectionString), sd);
                var explicitTransactionId = GetPropertyValue<string>(Prop_TransactionId, nameof(Prop_TransactionId), sd);
                var commandText = GetPropertyValue<string>(Prop_CommandText, nameof(Prop_CommandText), sd);
                var parameters = GetPropertyValue<Dictionary<string, string>>(Prop_Parameters, nameof(Prop_Parameters), sd);
                var timeoutText = GetPropertyValue<string>(Prop_CommandTimeoutSeconds, nameof(Prop_CommandTimeoutSeconds), sd);
                int timeout;
                if (!int.TryParse(timeoutText, out timeout))
                    timeout = 30;

                var transactionId = DatabaseTransactionResolver.ResolveOptional(explicitTransactionId);

                var logic = new DatabaseQueryLogic();
                var result = logic.Execute(provider, connectionString, Prop_CommandType, commandText, parameters, timeout, transactionId);

                SetVariableValue(Prop_ResultTable, result.Table, sd);
                SetVariableValue(Prop_RowCount, result.RowCount, sd);
                SetVariableValue(Prop_ColumnCount, result.ColumnCount, sd);
                SetVariableValue(Prop_ColumnNames, result.ColumnNames, sd);
                SetVariableValue(Prop_HasRows, result.HasRows, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Получено строк: {result.RowCount}, столбцов: {result.ColumnCount}"
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
                    ErrorMessage = $"Ошибка выполнения запроса к БД: {ex.Message}"
                };
            }
        }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            result.ValidateRequired(Prop_CommandText, ActivityStrings.Field_CommandText, ActivityStrings.Error_CommandTextRequired);
            if (string.IsNullOrWhiteSpace(Prop_TransactionId) && string.IsNullOrWhiteSpace(DatabaseTransactionResolver.ResolveOptional(null)))
                result.ValidateRequired(Prop_ConnectionString, ActivityStrings.Field_ConnectionString, ActivityStrings.Error_ConnectionStringRequired);
            return result;
        }
    }
}
