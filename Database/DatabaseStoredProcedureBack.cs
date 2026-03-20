using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность для вызова stored procedure с output-параметрами.</summary>
    public class DatabaseStoredProcedureBack : PrimoComponentTO<DatabaseStoredProcedure>
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

        private string _propProcedureName;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_CommandText)]
        public string Prop_ProcedureName
        {
            get => _propProcedureName;
            set { _propProcedureName = value; InvokePropertyChanged(this, nameof(Prop_ProcedureName)); }
        }

        private string _propInputParameters;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters), System.ComponentModel.DisplayName(ActivityStrings.Field_ParametersDictionary)]
        public string Prop_InputParameters
        {
            get => _propInputParameters;
            set { _propInputParameters = value; InvokePropertyChanged(this, nameof(Prop_InputParameters)); }
        }

        private string _propOutputParameterNames;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters), System.ComponentModel.DisplayName(ActivityStrings.Field_OutputParameterNames)]
        public string Prop_OutputParameterNames
        {
            get => _propOutputParameterNames;
            set { _propOutputParameterNames = value; InvokePropertyChanged(this, nameof(Prop_OutputParameterNames)); }
        }

        private string _propInputOutputParameters;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters), System.ComponentModel.DisplayName(ActivityStrings.Field_InputOutputParameters)]
        public string Prop_InputOutputParameters
        {
            get => _propInputOutputParameters;
            set { _propInputOutputParameters = value; InvokePropertyChanged(this, nameof(Prop_InputOutputParameters)); }
        }

        private bool _propIncludeReturnValue = true;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Optional), System.ComponentModel.DisplayName(ActivityStrings.Field_IncludeReturnValue)]
        public bool Prop_IncludeReturnValue
        {
            get => _propIncludeReturnValue;
            set { _propIncludeReturnValue = value; InvokePropertyChanged(this, nameof(Prop_IncludeReturnValue)); }
        }

        private string _propOutputParameterSize = "4000";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_OutputParameterSize)]
        public string Prop_OutputParameterSize
        {
            get => _propOutputParameterSize;
            set { _propOutputParameterSize = value; InvokePropertyChanged(this, nameof(Prop_OutputParameterSize)); }
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

        private string _propOutputParameters;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_OutputParameters)]
        public string Prop_OutputParameters
        {
            get => _propOutputParameters;
            set { _propOutputParameters = value; InvokePropertyChanged(this, nameof(Prop_OutputParameters)); }
        }

        private string _propReturnValue;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ReturnValue)]
        public string Prop_ReturnValue
        {
            get => _propReturnValue;
            set { _propReturnValue = value; InvokePropertyChanged(this, nameof(Prop_ReturnValue)); }
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

        public DatabaseStoredProcedureBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DatabaseStoredProcedure;
            sdkComponentHelp =
                "Вызывает stored procedure через ADO.NET.\n" +
                "Поддерживает input-параметры, output-параметры и получение return value.";
            sdkComponentIcon = ActivityIcons.Table;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ProviderInvariantName", "ADO.NET provider invariant name"),
                PropertyBuilder.String("Prop_ConnectionString", "Строка подключения к БД"),
                PropertyBuilder.String("Prop_TransactionId", "ID транзакции (опционально)"),
                PropertyBuilder.String("Prop_ProcedureName", "Имя stored procedure"),
                PropertyBuilder.Script<Dictionary<string, string>>("Prop_InputParameters", "Входные параметры"),
                PropertyBuilder.Script<Dictionary<string, string>>("Prop_InputOutputParameters", "InputOutput параметры"),
                PropertyBuilder.Script<List<string>>("Prop_OutputParameterNames", "Список output-параметров"),
                PropertyBuilder.BooleanObject("Prop_IncludeReturnValue", "Получать return value"),
                PropertyBuilder.Int("Prop_OutputParameterSize", "Размер output/inputoutput параметров"),
                PropertyBuilder.Int("Prop_CommandTimeoutSeconds", "Таймаут выполнения в секундах"),
                PropertyBuilder.Variable<Dictionary<string, string>>("Prop_OutputParameters", "Словарь выходных параметров"),
                PropertyBuilder.Variable<string>("Prop_ReturnValue", "Return value stored procedure"),
                PropertyBuilder.Variable<int>("Prop_AffectedRows", "Количество затронутых строк")
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
                var procedureName = GetPropertyValue<string>(Prop_ProcedureName, nameof(Prop_ProcedureName), sd);
                var inputParameters = GetPropertyValue<Dictionary<string, string>>(Prop_InputParameters, nameof(Prop_InputParameters), sd);
                var inputOutputParameters = GetPropertyValue<Dictionary<string, string>>(Prop_InputOutputParameters, nameof(Prop_InputOutputParameters), sd);
                var outputParameterNames = GetPropertyValue<List<string>>(Prop_OutputParameterNames, nameof(Prop_OutputParameterNames), sd);
                var timeoutText = GetPropertyValue<string>(Prop_CommandTimeoutSeconds, nameof(Prop_CommandTimeoutSeconds), sd);
                var outputParameterSizeText = GetPropertyValue<string>(Prop_OutputParameterSize, nameof(Prop_OutputParameterSize), sd);

                int timeout;
                if (!int.TryParse(timeoutText, out timeout))
                    timeout = 30;
                int outputParameterSize;
                if (!int.TryParse(outputParameterSizeText, out outputParameterSize))
                    outputParameterSize = 4000;

                var transactionId = DatabaseTransactionResolver.ResolveOptional(explicitTransactionId);
                var transactionHandle = DatabaseTransactionManager.Get(transactionId);
                var executionResult = transactionHandle != null
                    ? DatabaseHelper.ExecuteStoredProcedure(transactionHandle, procedureName, timeout, inputParameters, inputOutputParameters, outputParameterNames, outputParameterSize, Prop_IncludeReturnValue)
                    : DatabaseHelper.ExecuteStoredProcedure(provider, connectionString, procedureName, timeout, inputParameters, inputOutputParameters, outputParameterNames, outputParameterSize, Prop_IncludeReturnValue);

                SetVariableValue(Prop_OutputParameters, executionResult.OutputParameters ?? new Dictionary<string, string>(), sd);
                SetVariableValue(Prop_ReturnValue, executionResult.ReturnValue ?? string.Empty, sd);
                SetVariableValue(Prop_AffectedRows, executionResult.AffectedRows, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Stored procedure выполнена. Output: {(executionResult.OutputParameters != null ? executionResult.OutputParameters.Count : 0)}, affected rows: {executionResult.AffectedRows}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка вызова stored procedure: {ex.Message}"
                };
            }
        }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            result.ValidateRequired(Prop_ProcedureName, ActivityStrings.Field_CommandText, ActivityStrings.Error_CommandTextRequired);
            if (string.IsNullOrWhiteSpace(Prop_TransactionId) && string.IsNullOrWhiteSpace(DatabaseTransactionResolver.ResolveOptional(null)))
                result.ValidateRequired(Prop_ConnectionString, ActivityStrings.Field_ConnectionString, ActivityStrings.Error_ConnectionStringRequired);
            return result;
        }
    }
}
