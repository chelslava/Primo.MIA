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

        private DatabaseCommandType _propCommandType = DatabaseCommandType.Text;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_CommandType)]
        public DatabaseCommandType Prop_CommandType
        {
            get => _propCommandType;
            set { _propCommandType = value; InvokePropertyChanged(this, nameof(Prop_CommandType)); }
        }

        private string _propCommandText;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_CommandText)]
        public string Prop_CommandText
        {
            get => _propCommandText;
            set { _propCommandText = value; InvokePropertyChanged(this, nameof(Prop_CommandText)); }
        }

        private string _propParameters;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters), System.ComponentModel.DisplayName(ActivityStrings.Field_ParametersDictionary)]
        public string Prop_Parameters
        {
            get => _propParameters;
            set { _propParameters = value; InvokePropertyChanged(this, nameof(Prop_Parameters)); }
        }

        private string _propCommandTimeoutSeconds = "30";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CommandTimeoutSeconds)]
        public string Prop_CommandTimeoutSeconds
        {
            get => _propCommandTimeoutSeconds;
            set { _propCommandTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_CommandTimeoutSeconds)); }
        }

        private bool _propSplitByGoBatches;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_SplitByGoBatches)]
        public bool Prop_SplitByGoBatches
        {
            get => _propSplitByGoBatches;
            set { _propSplitByGoBatches = value; InvokePropertyChanged(this, nameof(Prop_SplitByGoBatches)); }
        }

        private string _propAffectedRows;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_AffectedRows)]
        public string Prop_AffectedRows
        {
            get => _propAffectedRows;
            set { _propAffectedRows = value; InvokePropertyChanged(this, nameof(Prop_AffectedRows)); }
        }

        private string _propHasAffectedRows;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_HasAffectedRows)]
        public string Prop_HasAffectedRows
        {
            get => _propHasAffectedRows;
            set { _propHasAffectedRows = value; InvokePropertyChanged(this, nameof(Prop_HasAffectedRows)); }
        }

        private string _propBatchCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_BatchCount)]
        public string Prop_BatchCount
        {
            get => _propBatchCount;
            set { _propBatchCount = value; InvokePropertyChanged(this, nameof(Prop_BatchCount)); }
        }

        public DatabaseNonQueryBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DatabaseNonQuery;
            sdkComponentHelp =
                "Выполняет INSERT/UPDATE/DELETE или stored procedure без табличного результата.\n" +
                "Возвращает количество затронутых строк.";
            sdkComponentIcon = ActivityIcons.Table;
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
                PropertyBuilder.Variable<int>("Prop_AffectedRows", "Количество затронутых строк"),
                PropertyBuilder.Variable<bool>("Prop_HasAffectedRows", "Количество затронутых строк больше нуля"),
                PropertyBuilder.Variable<int>("Prop_BatchCount", "Количество выполненных batch")
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
                var transactionHandle = DatabaseTransactionManager.Get(transactionId);
                var executionResult = transactionHandle != null
                    ? DatabaseHelper.ExecuteNonQuery(transactionHandle, commandText, Prop_CommandType, timeout, Prop_SplitByGoBatches, parameters)
                    : DatabaseHelper.ExecuteNonQuery(provider, connectionString, commandText, Prop_CommandType, timeout, Prop_SplitByGoBatches, parameters);

                SetVariableValue(Prop_AffectedRows, executionResult.AffectedRows, sd);
                SetVariableValue(Prop_HasAffectedRows, executionResult.AffectedRows > 0, sd);
                SetVariableValue(Prop_BatchCount, executionResult.BatchCount, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Команда выполнена. Затронуто строк: {executionResult.AffectedRows}. Batch: {executionResult.BatchCount}"
                };
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
            if (string.IsNullOrWhiteSpace(Prop_TransactionId) && string.IsNullOrWhiteSpace(DatabaseTransactionResolver.ResolveOptional(null)))
                result.ValidateRequired(Prop_ConnectionString, ActivityStrings.Field_ConnectionString, ActivityStrings.Error_ConnectionStringRequired);
            return result;
        }
    }
}
