using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    public class RedisDeleteBack : PrimoComponentTO<RedisDelete>
    {
        public override string GroupName { get => ActivityCategories.Redis; protected set { } }

        protected override int sdkTimeOut { get => 30000; set { } }

        private string _propConnectionString;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisConnectionString)]
        public string Prop_ConnectionString { get => _propConnectionString; set { _propConnectionString = value; InvokePropertyChanged(this, nameof(Prop_ConnectionString)); } }

        private string _propKeys;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisKeys)]
        public string Prop_Keys { get => _propKeys; set { _propKeys = value; InvokePropertyChanged(this, nameof(Prop_Keys)); } }

        private string _propDatabaseIndex = "0";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisDatabaseIndex)]
        public string Prop_DatabaseIndex { get => _propDatabaseIndex; set { _propDatabaseIndex = value; InvokePropertyChanged(this, nameof(Prop_DatabaseIndex)); } }

        private string _propDeletedCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisDeletedCount)]
        public string Prop_DeletedCount { get => _propDeletedCount; set { _propDeletedCount = value; InvokePropertyChanged(this, nameof(Prop_DeletedCount)); } }

        public RedisDeleteBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_RedisDelete;
            sdkComponentHelp = @"Компонент ""Redis: Удалить ключ""
Удаляет один или несколько ключей из Redis. Принимает List<string> с именами ключей.

Основные:
Строка подключения*: [String] Redis connection string.
Ключи*: [List<String>] Список ключей для удаления.

Настройки:
Индекс БД: [Int32] Индекс базы данных Redis (0–15). По умолчанию 0.

Выходные данные:
Удалено ключей: [Int32] Количество фактически удалённых ключей.";
            sdkComponentIcon = ActivityIcons.Redis;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ConnectionString", "Redis connection string"),
                PropertyBuilder.Script<List<string>>("Prop_Keys", "Список ключей для удаления"),
                PropertyBuilder.Int("Prop_DatabaseIndex", "Индекс базы данных (0–15)"),
                PropertyBuilder.Variable<int>("Prop_DeletedCount", "Количество удалённых ключей")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var connectionString = GetPropertyValue<string>(Prop_ConnectionString, nameof(Prop_ConnectionString), sd);
                var keys = GetPropertyValue<List<string>>(Prop_Keys, nameof(Prop_Keys), sd);
                int dbIndex; int.TryParse(GetPropertyValue<string>(Prop_DatabaseIndex, nameof(Prop_DatabaseIndex), sd), out dbIndex);

                if (keys == null || keys.Count == 0)
                    throw new InvalidOperationException("Список ключей не может быть пустым.");

                var db = RedisHelper.GetDatabase(connectionString, dbIndex);
                var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
                var deleted = db.KeyDelete(redisKeys);

                SetVariableValue(Prop_DeletedCount, (int)deleted, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Удалено ключей: {deleted}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка Redis DEL: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            result.ValidateRequired(Prop_ConnectionString, ActivityStrings.Field_RedisConnectionString, ActivityStrings.Error_RedisConnectionRequired);
            result.ValidateRequired(Prop_Keys, ActivityStrings.Field_RedisKeys, ActivityStrings.Error_RedisKeyRequired);
            return result;
        }
    }
}
