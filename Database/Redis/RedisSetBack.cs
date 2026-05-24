using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    public class RedisSetBack : PrimoComponentTO<RedisSet>
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

        private string _propExpirySeconds = "0";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisExpiry)]
        public string Prop_ExpirySeconds { get => _propExpirySeconds; set { _propExpirySeconds = value; InvokePropertyChanged(this, nameof(Prop_ExpirySeconds)); } }

        private string _propDatabaseIndex = "0";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisDatabaseIndex)]
        public string Prop_DatabaseIndex { get => _propDatabaseIndex; set { _propDatabaseIndex = value; InvokePropertyChanged(this, nameof(Prop_DatabaseIndex)); } }

        public RedisSetBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_RedisSet;
            sdkComponentHelp = @"Компонент ""Redis: Установить значение""
Записывает строковое значение по ключу в Redis с опциональным сроком хранения (TTL).

Основные:
Строка подключения*: [String] Redis connection string.
Ключ*: [String] Ключ для записи.
Значение*: [String] Значение для сохранения.

Настройки:
Срок хранения (сек): [Int32] TTL в секундах. 0 — без ограничения.
Индекс БД: [Int32] Индекс базы данных Redis (0–15). По умолчанию 0.";
            sdkComponentIcon = ActivityIcons.Redis;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ConnectionString", "Redis connection string"),
                PropertyBuilder.String("Prop_Key", "Ключ"),
                PropertyBuilder.String("Prop_Value", "Значение"),
                PropertyBuilder.Int("Prop_ExpirySeconds", "TTL в секундах (0 = без ограничения)"),
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
                var value = GetPropertyValue<string>(Prop_Value, nameof(Prop_Value), sd);
                int expiry; int.TryParse(GetPropertyValue<string>(Prop_ExpirySeconds, nameof(Prop_ExpirySeconds), sd), out expiry);
                int dbIndex; int.TryParse(GetPropertyValue<string>(Prop_DatabaseIndex, nameof(Prop_DatabaseIndex), sd), out dbIndex);

                var db = RedisHelper.GetDatabase(connectionString, dbIndex);
                var ttl = expiry > 0 ? (TimeSpan?)TimeSpan.FromSeconds(expiry) : null;
                db.StringSet(key, value, ttl);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Ключ записан: {key}" + (ttl.HasValue ? $" (TTL: {expiry}с)" : "") };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка Redis SET: {ex.Message}" };
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
