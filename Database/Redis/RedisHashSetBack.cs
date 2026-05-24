using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    public class RedisHashSetBack : PrimoComponentTO<RedisHashSet>
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

        private string _propField;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisField)]
        public string Prop_Field { get => _propField; set { _propField = value; InvokePropertyChanged(this, nameof(Prop_Field)); } }

        private string _propValue;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisValue)]
        public string Prop_Value { get => _propValue; set { _propValue = value; InvokePropertyChanged(this, nameof(Prop_Value)); } }

        private string _propDatabaseIndex = "0";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisDatabaseIndex)]
        public string Prop_DatabaseIndex { get => _propDatabaseIndex; set { _propDatabaseIndex = value; InvokePropertyChanged(this, nameof(Prop_DatabaseIndex)); } }

        public RedisHashSetBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_RedisHashSet;
            sdkComponentHelp = @"Компонент ""Redis: Установить поле хэша""
Устанавливает значение одного поля в хэше Redis (HSET).

Основные:
Строка подключения*: [String] Redis connection string.
Ключ*: [String] Ключ хэша.
Поле*: [String] Имя поля.
Значение*: [String] Значение поля.

Настройки:
Индекс БД: [Int32] Индекс базы данных Redis (0–15). По умолчанию 0.";
            sdkComponentIcon = ActivityIcons.Redis;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ConnectionString", "Redis connection string"),
                PropertyBuilder.String("Prop_Key", "Ключ хэша"),
                PropertyBuilder.String("Prop_Field", "Поле хэша"),
                PropertyBuilder.String("Prop_Value", "Значение поля"),
                PropertyBuilder.Int("Prop_DatabaseIndex", "Индекс базы данных (0–15)")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var connectionString = GetPropertyValue<string>(Prop_ConnectionString, nameof(Prop_ConnectionString), sd);
                var key = GetPropertyValue<string>(Prop_Key, nameof(Prop_Key), sd);
                var field = GetPropertyValue<string>(Prop_Field, nameof(Prop_Field), sd);
                var value = GetPropertyValue<string>(Prop_Value, nameof(Prop_Value), sd);
                int dbIndex; int.TryParse(GetPropertyValue<string>(Prop_DatabaseIndex, nameof(Prop_DatabaseIndex), sd), out dbIndex);

                var db = RedisHelper.GetDatabase(connectionString, dbIndex);
                db.HashSet(key, field, value);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Поле установлено: {key}.{field}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка Redis HSET: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            result.ValidateRequired(Prop_ConnectionString, ActivityStrings.Field_RedisConnectionString, ActivityStrings.Error_RedisConnectionRequired);
            result.ValidateRequired(Prop_Key, ActivityStrings.Field_RedisKey, ActivityStrings.Error_RedisKeyRequired);
            result.ValidateRequired(Prop_Field, ActivityStrings.Field_RedisField, ActivityStrings.Error_RedisFieldRequired);
            result.ValidateRequired(Prop_Value, ActivityStrings.Field_RedisValue, ActivityStrings.Error_RedisValueRequired);
            return result;
        }
    }
}
