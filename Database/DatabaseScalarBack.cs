using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность для получения одного скалярного значения из БД.</summary>
    public class DatabaseScalarBack : PrimoComponentTO<DatabaseScalar>
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

        private string _propValue;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Value)]
        public string Prop_Value
        {
            get => _propValue;
            set { _propValue = value; InvokePropertyChanged(this, nameof(Prop_Value)); }
        }

        private string _propDefaultValue;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Optional), System.ComponentModel.DisplayName(ActivityStrings.Field_DefaultValue)]
        public string Prop_DefaultValue
        {
            get => _propDefaultValue;
            set { _propDefaultValue = value; InvokePropertyChanged(this, nameof(Prop_DefaultValue)); }
        }

        private string _propHasValue;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_HasValue)]
        public string Prop_HasValue
        {
            get => _propHasValue;
            set { _propHasValue = value; InvokePropertyChanged(this, nameof(Prop_HasValue)); }
        }

        private string _propValueType;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ValueType)]
        public string Prop_ValueType
        {
            get => _propValueType;
            set { _propValueType = value; InvokePropertyChanged(this, nameof(Prop_ValueType)); }
        }

        private string _propUsedDefaultValue;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_UsedDefaultValue)]
        public string Prop_UsedDefaultValue
        {
            get => _propUsedDefaultValue;
            set { _propUsedDefaultValue = value; InvokePropertyChanged(this, nameof(Prop_UsedDefaultValue)); }
        }

        private string _propIntValue;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_IntValue)]
        public string Prop_IntValue
        {
            get => _propIntValue;
            set { _propIntValue = value; InvokePropertyChanged(this, nameof(Prop_IntValue)); }
        }

        private string _propDecimalValue;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(decimal))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_DecimalValue)]
        public string Prop_DecimalValue
        {
            get => _propDecimalValue;
            set { _propDecimalValue = value; InvokePropertyChanged(this, nameof(Prop_DecimalValue)); }
        }

        private string _propBoolValue;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_BoolValue)]
        public string Prop_BoolValue
        {
            get => _propBoolValue;
            set { _propBoolValue = value; InvokePropertyChanged(this, nameof(Prop_BoolValue)); }
        }

        private string _propDateTimeValue;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DateTime))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_DateTimeValue)]
        public string Prop_DateTimeValue
        {
            get => _propDateTimeValue;
            set { _propDateTimeValue = value; InvokePropertyChanged(this, nameof(Prop_DateTimeValue)); }
        }

        public DatabaseScalarBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DatabaseScalar;
            sdkComponentHelp =
                "Выполняет SQL-запрос, возвращающий одно значение.\n" +
                "Результат сохраняется как строка в invariant-культуре.";
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
                PropertyBuilder.String("Prop_DefaultValue", "Значение по умолчанию, если scalar вернул null/DBNull"),
                PropertyBuilder.Variable<string>("Prop_Value", "Скалярный результат в виде строки"),
                PropertyBuilder.Variable<bool>("Prop_HasValue", "Есть значение (не null/DBNull)"),
                PropertyBuilder.Variable<string>("Prop_ValueType", "Имя .NET-типа результата"),
                PropertyBuilder.Variable<bool>("Prop_UsedDefaultValue", "Подставлено ли значение по умолчанию"),
                PropertyBuilder.Variable<int>("Prop_IntValue", "Результат как Int32"),
                PropertyBuilder.Variable<decimal>("Prop_DecimalValue", "Результат как Decimal"),
                PropertyBuilder.Variable<bool>("Prop_BoolValue", "Результат как Boolean"),
                PropertyBuilder.Variable<DateTime>("Prop_DateTimeValue", "Результат как DateTime")
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
                var rawValue = transactionHandle != null
                    ? DatabaseHelper.ExecuteScalar(transactionHandle, commandText, Prop_CommandType, timeout, parameters)
                    : DatabaseHelper.ExecuteScalar(provider, connectionString, commandText, Prop_CommandType, timeout, parameters);
                var hasValue = rawValue != null && rawValue != DBNull.Value;
                var defaultValue = GetPropertyValue<string>(Prop_DefaultValue, nameof(Prop_DefaultValue), sd);
                var usedDefaultValue = !hasValue && !string.IsNullOrWhiteSpace(defaultValue);
                var effectiveValue = hasValue ? rawValue : (usedDefaultValue ? (object)defaultValue : null);
                var valueType = effectiveValue != null ? effectiveValue.GetType().FullName : string.Empty;
                var value = DatabaseHelper.ConvertScalarToString(effectiveValue) ?? string.Empty;
                var intValue = DatabaseHelper.ConvertScalarToInt32(effectiveValue);
                var decimalValue = DatabaseHelper.ConvertScalarToDecimal(effectiveValue);
                var boolValue = DatabaseHelper.ConvertScalarToBoolean(effectiveValue);
                var dateTimeValue = DatabaseHelper.ConvertScalarToDateTime(effectiveValue);

                SetVariableValue(Prop_Value, value, sd);
                SetVariableValue(Prop_HasValue, hasValue, sd);
                SetVariableValue(Prop_ValueType, valueType, sd);
                SetVariableValue(Prop_UsedDefaultValue, usedDefaultValue, sd);
                if (intValue.HasValue)
                    SetVariableValue(Prop_IntValue, intValue.Value, sd);
                if (decimalValue.HasValue)
                    SetVariableValue(Prop_DecimalValue, decimalValue.Value, sd);
                if (boolValue.HasValue)
                    SetVariableValue(Prop_BoolValue, boolValue.Value, sd);
                if (dateTimeValue.HasValue)
                    SetVariableValue(Prop_DateTimeValue, dateTimeValue.Value, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = hasValue
                        ? $"Получено значение типа {valueType}"
                        : (usedDefaultValue
                            ? "Скалярный запрос вернул null/DBNull, подставлено значение по умолчанию"
                            : "Скалярный запрос вернул null/DBNull")
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка выполнения scalar-запроса: {ex.Message}"
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
