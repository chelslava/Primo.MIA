// =============================================================================
// DatabaseNonQueryBack.cs — активность «Database: Выполнить команду (NonQuery)».
// Выполняет INSERT/UPDATE/DELETE или хранимую процедуру без табличного результата.
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность для выполнения INSERT/UPDATE/DELETE.</summary>
    public class DatabaseNonQueryBack : PrimoComponentTO<DatabaseNonQuery>
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
        /// <summary>Текст SQL-команды или имя хранимой процедуры.</summary>
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

        private bool _propSplitByGoBatches;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_SplitByGoBatches)]
        /// <summary>Разбивать SQL-текст по batch-разделителю GO перед выполнением.</summary>
        public bool Prop_SplitByGoBatches
        {
            get => _propSplitByGoBatches;
            set { _propSplitByGoBatches = value; InvokePropertyChanged(this, nameof(Prop_SplitByGoBatches)); }
        }

        private bool _propReturnIdentity;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_ReturnIdentity)]
        /// <summary>Возвращать значение последнего вставленного идентификатора (last inserted id).</summary>
        public bool Prop_ReturnIdentity
        {
            get => _propReturnIdentity;
            set { _propReturnIdentity = value; InvokePropertyChanged(this, nameof(Prop_ReturnIdentity)); }
        }

        private string _propAffectedRows;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_AffectedRows)]
        /// <summary>Количество строк, затронутых выполненной командой.</summary>
        public string Prop_AffectedRows
        {
            get => _propAffectedRows;
            set { _propAffectedRows = value; InvokePropertyChanged(this, nameof(Prop_AffectedRows)); }
        }

        private string _propHasAffectedRows;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_HasAffectedRows)]
        /// <summary>Признак того, что хотя бы одна строка была затронута командой.</summary>
        public string Prop_HasAffectedRows
        {
            get => _propHasAffectedRows;
            set { _propHasAffectedRows = value; InvokePropertyChanged(this, nameof(Prop_HasAffectedRows)); }
        }

        private string _propBatchCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_BatchCount)]
        /// <summary>Количество выполненных GO-batch блоков (актуально при Prop_SplitByGoBatches = true).</summary>
        public string Prop_BatchCount
        {
            get => _propBatchCount;
            set { _propBatchCount = value; InvokePropertyChanged(this, nameof(Prop_BatchCount)); }
        }

        private string _propIdentityValue;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_IdentityValue)]
        /// <summary>Значение последнего вставленного идентификатора (актуально при Prop_ReturnIdentity = true).</summary>
        public string Prop_IdentityValue
        {
            get => _propIdentityValue;
            set { _propIdentityValue = value; InvokePropertyChanged(this, nameof(Prop_IdentityValue)); }
        }

        public DatabaseNonQueryBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DatabaseNonQuery;
            sdkComponentHelp =
                "Выполняет INSERT/UPDATE/DELETE или stored procedure без табличного результата.\n" +
                "Возвращает количество затронутых строк.";
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
                PropertyBuilder.BooleanObject("Prop_SplitByGoBatches", "Разбивать SQL-текст по batch-границам GO"),
                PropertyBuilder.BooleanObject("Prop_ReturnIdentity", "Получить значение last inserted id после выполнения"),
                PropertyBuilder.Variable<int>("Prop_AffectedRows", "Количество затронутых строк"),
                PropertyBuilder.Variable<bool>("Prop_HasAffectedRows", "Количество затронутых строк больше нуля"),
                PropertyBuilder.Variable<int>("Prop_BatchCount", "Количество выполненных batch"),
                PropertyBuilder.Variable<string>("Prop_IdentityValue", "Значение last inserted id")
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

                var logic = new DatabaseCommandLogic();
                var executionResult = logic.ExecuteNonQuery(provider, connectionString, Prop_CommandType, commandText, parameters, timeout, transactionId, Prop_SplitByGoBatches, Prop_ReturnIdentity);

                SetVariableValue(Prop_AffectedRows, executionResult.RowsAffected, sd);
                SetVariableValue(Prop_HasAffectedRows, executionResult.RowsAffected > 0, sd);
                SetVariableValue(Prop_BatchCount, executionResult.BatchCount, sd);
                SetVariableValue(Prop_IdentityValue, executionResult.IdentityValue ?? string.Empty, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = string.IsNullOrWhiteSpace(executionResult.IdentityValue)
                        ? $"Команда выполнена. Затронуто строк: {executionResult.RowsAffected}. Batch: {executionResult.BatchCount}"
                        : $"Команда выполнена. Затронуто строк: {executionResult.RowsAffected}. Identity: {executionResult.IdentityValue}"
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
                    ErrorMessage = $"Ошибка выполнения non-query команды: {ex.Message}"
                };
            }
        }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            result.ValidateRequired(Prop_CommandText, ActivityStrings.Field_CommandText, ActivityStrings.Error_CommandTextRequired);
            result.ValidateCondition(
                Prop_SplitByGoBatches && Prop_ReturnIdentity,
                ActivityStrings.Field_ReturnIdentity,
                "Нельзя одновременно включить GO batch и получение last inserted id");
            if (string.IsNullOrWhiteSpace(Prop_TransactionId) && string.IsNullOrWhiteSpace(DatabaseTransactionResolver.ResolveOptional(null)))
                result.ValidateRequired(Prop_ConnectionString, ActivityStrings.Field_ConnectionString, ActivityStrings.Error_ConnectionStringRequired);
            return result;
        }
    }
}
