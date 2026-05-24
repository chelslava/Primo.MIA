using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    public class RedisListPushBack : PrimoComponentTO<RedisListPush>
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

        private string _propValue;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisValue)]
        public string Prop_Value { get => _propValue; set { _propValue = value; InvokePropertyChanged(this, nameof(Prop_Value)); } }

        private bool _propPushLeft = true;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisDirection)]
        public bool Prop_PushLeft { get => _propPushLeft; set { _propPushLeft = value; InvokePropertyChanged(this, nameof(Prop_PushLeft)); } }

        private string _propDatabaseIndex = "0";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisDatabaseIndex)]
        public string Prop_DatabaseIndex { get => _propDatabaseIndex; set { _propDatabaseIndex = value; InvokePropertyChanged(this, nameof(Prop_DatabaseIndex)); } }

        private string _propListLength;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(long))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisListLength)]
        public string Prop_ListLength { get => _propListLength; set { _propListLength = value; InvokePropertyChanged(this, nameof(Prop_ListLength)); } }

        public RedisListPushBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_RedisListPush;
            sdkComponentHelp = @"Компонент ""Redis: Добавить в список""
Добавляет значение в начало (LPUSH) или конец (RPUSH) списка Redis.

Основные:
Строка подключения*: [String] Redis connection string.
Ключ*: [String] Ключ списка.
Значение*: [String] Добавляемое значение.

Настройки:
Направление: [Boolean] True = LPUSH (в начало), False = RPUSH (в конец).
Индекс БД: [Int32] Индекс базы данных Redis (0–15). По умолчанию 0.

Выходные данные:
Длина списка: [Int64] Новая длина списка после добавления.";
            sdkComponentIcon = ActivityIcons.Redis;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ConnectionString", "Redis connection string"),
                PropertyBuilder.String("Prop_Key", "Ключ списка"),
                PropertyBuilder.String("Prop_Value", "Добавляемое значение"),
                PropertyBuilder.BooleanObject("Prop_PushLeft", "LPUSH (в начало), иначе RPUSH (в конец)"),
                PropertyBuilder.Int("Prop_DatabaseIndex", "Индекс базы данных (0–15)"),
                PropertyBuilder.Variable<long>("Prop_ListLength", "Новая длина списка")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var connectionString = GetPropertyValue<string>(Prop_ConnectionString, nameof(Prop_ConnectionString), sd);
                var key = GetPropertyValue<string>(Prop_Key, nameof(Prop_Key), sd);
                var value = GetPropertyValue<string>(Prop_Value, nameof(Prop_Value), sd);
                int dbIndex; int.TryParse(GetPropertyValue<string>(Prop_DatabaseIndex, nameof(Prop_DatabaseIndex), sd), out dbIndex);

                var db = RedisHelper.GetDatabase(connectionString, dbIndex);
                var length = Prop_PushLeft
                    ? db.ListLeftPush(key, value)
                    : db.ListRightPush(key, value);

                SetVariableValue(Prop_ListLength, length, sd);

                var direction = Prop_PushLeft ? "LPUSH" : "RPUSH";
                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"{direction} выполнен: {key}, длина: {length}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка Redis PUSH: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            result.ValidateRequired(Prop_ConnectionString, ActivityStrings.Field_RedisConnectionString, ActivityStrings.Error_RedisConnectionRequired);
            result.ValidateRequired(Prop_Key, ActivityStrings.Field_RedisKey, ActivityStrings.Error_RedisKeyRequired);
            result.ValidateRequired(Prop_Value, ActivityStrings.Field_RedisValue, ActivityStrings.Error_RedisValueRequired);
            return result;
        }
    }
}
