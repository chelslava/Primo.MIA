using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    public class RedisListRangeBack : PrimoComponentTO<RedisListRange>
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

        private string _propStart = "0";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisStart)]
        public string Prop_Start { get => _propStart; set { _propStart = value; InvokePropertyChanged(this, nameof(Prop_Start)); } }

        private string _propStop = "-1";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisStop)]
        public string Prop_Stop { get => _propStop; set { _propStop = value; InvokePropertyChanged(this, nameof(Prop_Stop)); } }

        private string _propDatabaseIndex = "0";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisDatabaseIndex)]
        public string Prop_DatabaseIndex { get => _propDatabaseIndex; set { _propDatabaseIndex = value; InvokePropertyChanged(this, nameof(Prop_DatabaseIndex)); } }

        private string _propValues;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisValues)]
        public string Prop_Values { get => _propValues; set { _propValues = value; InvokePropertyChanged(this, nameof(Prop_Values)); } }

        public RedisListRangeBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_RedisListRange;
            sdkComponentHelp = @"Компонент ""Redis: Получить диапазон списка""
Возвращает элементы списка Redis в заданном диапазоне (LRANGE).

Основные:
Строка подключения*: [String] Redis connection string.
Ключ*: [String] Ключ списка.

Настройки:
Начало диапазона: [Int32] Индекс начального элемента (0 = первый). По умолчанию 0.
Конец диапазона: [Int32] Индекс конечного элемента (-1 = до конца). По умолчанию -1.
Индекс БД: [Int32] Индекс базы данных Redis (0–15). По умолчанию 0.

Выходные данные:
Значения: [List<String>] Список элементов в заданном диапазоне.";
            sdkComponentIcon = ActivityIcons.Redis;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ConnectionString", "Redis connection string"),
                PropertyBuilder.String("Prop_Key", "Ключ списка"),
                PropertyBuilder.Int("Prop_Start", "Начальный индекс (0 = первый)"),
                PropertyBuilder.Int("Prop_Stop", "Конечный индекс (-1 = до конца)"),
                PropertyBuilder.Int("Prop_DatabaseIndex", "Индекс базы данных (0–15)"),
                PropertyBuilder.Variable<List<string>>("Prop_Values", "Элементы списка")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var connectionString = GetPropertyValue<string>(Prop_ConnectionString, nameof(Prop_ConnectionString), sd);
                var key = GetPropertyValue<string>(Prop_Key, nameof(Prop_Key), sd);
                long start; long.TryParse(GetPropertyValue<string>(Prop_Start, nameof(Prop_Start), sd), out start);
                long stop; long.TryParse(GetPropertyValue<string>(Prop_Stop, nameof(Prop_Stop), sd) ?? "-1", out stop);
                int dbIndex; int.TryParse(GetPropertyValue<string>(Prop_DatabaseIndex, nameof(Prop_DatabaseIndex), sd), out dbIndex);

                var db = RedisHelper.GetDatabase(connectionString, dbIndex);
                var entries = db.ListRange(key, start, stop);
                var result = entries.Select(e => (string)e).ToList();

                SetVariableValue(Prop_Values, result, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Получено элементов: {result.Count}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка Redis LRANGE: {ex.Message}" };
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
