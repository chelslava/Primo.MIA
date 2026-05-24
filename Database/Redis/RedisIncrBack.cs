using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    public class RedisIncrBack : PrimoComponentTO<RedisIncr>
    {
        public override string GroupName { get => ActivityCategories.Redis; protected set { } }

        protected override int sdkTimeOut { get => 30000; set { } }

        private string _propConnectionString;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisConnectionString)]
        public string Prop_ConnectionString { get => _propConnectionString; set { _propConnectionString = value; InvokePropertyChanged(this, nameof(Prop_ConnectionString)); } }

        private string _propKey;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisKey)]
        public string Prop_Key { get => _propKey; set { _propKey = value; InvokePropertyChanged(this, nameof(Prop_Key)); } }

        private string _propIncrement = "1";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(long))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisIncrement)]
        public string Prop_Increment { get => _propIncrement; set { _propIncrement = value; InvokePropertyChanged(this, nameof(Prop_Increment)); } }

        private string _propDatabaseIndex = "0";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisDatabaseIndex)]
        public string Prop_DatabaseIndex { get => _propDatabaseIndex; set { _propDatabaseIndex = value; InvokePropertyChanged(this, nameof(Prop_DatabaseIndex)); } }

        private string _propNewValue;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisNewValue)]
        public string Prop_NewValue { get => _propNewValue; set { _propNewValue = value; InvokePropertyChanged(this, nameof(Prop_NewValue)); } }

        public RedisIncrBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_RedisIncr;
            sdkComponentHelp = @"Компонент ""Redis: Инкремент""
Увеличивает числовое значение ключа на заданный шаг (INCRBY). Если ключ не существует — создаётся со значением 0, затем применяется шаг.

Основные:
Строка подключения*: [String] Redis connection string.
Ключ*: [String] Ключ для инкремента.

Настройки:
Шаг инкремента: [Int64] Величина приращения. По умолчанию 1.
Индекс БД: [Int32] Индекс базы данных Redis (0–15). По умолчанию 0.

Выходные данные:
Новое значение: [String] Результирующее значение ключа после инкремента.";
            sdkComponentIcon = ActivityIcons.Redis;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ConnectionString", "Redis connection string"),
                PropertyBuilder.String("Prop_Key", "Ключ для инкремента"),
                PropertyBuilder.Int("Prop_Increment", "Шаг инкремента (по умолчанию 1)"),
                PropertyBuilder.Int("Prop_DatabaseIndex", "Индекс базы данных (0–15)"),
                PropertyBuilder.Variable<string>("Prop_NewValue", "Новое значение после инкремента")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var connectionString = GetPropertyValue<string>(Prop_ConnectionString, nameof(Prop_ConnectionString), sd);
                var key = GetPropertyValue<string>(Prop_Key, nameof(Prop_Key), sd);
                long increment = 1; long.TryParse(GetPropertyValue<string>(Prop_Increment, nameof(Prop_Increment), sd), out increment);
                if (increment == 0) increment = 1;
                int dbIndex; int.TryParse(GetPropertyValue<string>(Prop_DatabaseIndex, nameof(Prop_DatabaseIndex), sd), out dbIndex);

                var db = RedisHelper.GetDatabase(connectionString, dbIndex);
                var newValue = db.StringIncrement(key, increment);

                SetVariableValue(Prop_NewValue, newValue.ToString(), sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Инкремент выполнен: {key} = {newValue}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка Redis INCRBY: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            result.ValidateRequired(Prop_ConnectionString, ActivityStrings.Field_RedisConnectionString, ActivityStrings.Error_RedisConnectionRequired);
            result.ValidateRequired(Prop_Key, ActivityStrings.Field_RedisKey, ActivityStrings.Error_RedisKeyRequired);
            return result;
        }
    }
}
