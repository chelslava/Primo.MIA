using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    public class RedisExistsBack : PrimoComponentTO<RedisExists>
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

        private string _propDatabaseIndex = "0";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisDatabaseIndex)]
        public string Prop_DatabaseIndex { get => _propDatabaseIndex; set { _propDatabaseIndex = value; InvokePropertyChanged(this, nameof(Prop_DatabaseIndex)); } }

        private string _propExists;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisExists)]
        public string Prop_Exists { get => _propExists; set { _propExists = value; InvokePropertyChanged(this, nameof(Prop_Exists)); } }

        public RedisExistsBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_RedisExists;
            sdkComponentHelp = @"Компонент ""Redis: Проверить ключ""
Проверяет наличие ключа в Redis без получения его значения.

Основные:
Строка подключения*: [String] Redis connection string.
Ключ*: [String] Имя ключа для проверки.

Настройки:
Индекс БД: [Int32] Индекс базы данных Redis (0–15). По умолчанию 0.

Выходные данные:
Ключ существует: [Boolean] True, если ключ присутствует в Redis.";
            sdkComponentIcon = ActivityIcons.Redis;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ConnectionString", "Redis connection string"),
                PropertyBuilder.String("Prop_Key", "Ключ для проверки"),
                PropertyBuilder.Int("Prop_DatabaseIndex", "Индекс базы данных (0–15)"),
                PropertyBuilder.Variable<bool>("Prop_Exists", "Ключ существует")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var connectionString = GetPropertyValue<string>(Prop_ConnectionString, nameof(Prop_ConnectionString), sd);
                var key = GetPropertyValue<string>(Prop_Key, nameof(Prop_Key), sd);
                int dbIndex; int.TryParse(GetPropertyValue<string>(Prop_DatabaseIndex, nameof(Prop_DatabaseIndex), sd), out dbIndex);

                var db = RedisHelper.GetDatabase(connectionString, dbIndex);
                var exists = db.KeyExists(key);

                SetVariableValue(Prop_Exists, exists, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = exists ? $"Ключ существует: {key}" : $"Ключ отсутствует: {key}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка Redis EXISTS: {ex.Message}" };
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
