using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    public class RedisHashGetBack : PrimoComponentTO<RedisHashGet>
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

        private string _propDatabaseIndex = "0";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisDatabaseIndex)]
        public string Prop_DatabaseIndex { get => _propDatabaseIndex; set { _propDatabaseIndex = value; InvokePropertyChanged(this, nameof(Prop_DatabaseIndex)); } }

        private string _propFieldValue;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisHashResult)]
        public string Prop_FieldValue { get => _propFieldValue; set { _propFieldValue = value; InvokePropertyChanged(this, nameof(Prop_FieldValue)); } }

        private string _propAllFields;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_RedisAllFields)]
        public string Prop_AllFields { get => _propAllFields; set { _propAllFields = value; InvokePropertyChanged(this, nameof(Prop_AllFields)); } }

        public RedisHashGetBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_RedisHashGet;
            sdkComponentHelp = @"Компонент ""Redis: Получить поле хэша""
Получает значение одного поля хэша или все поля сразу (HGET / HGETALL).

Основные:
Строка подключения*: [String] Redis connection string.
Ключ*: [String] Ключ хэша.
Поле: [String] Имя поля. Если пусто — возвращаются все поля (HGETALL).

Настройки:
Индекс БД: [Int32] Индекс базы данных Redis (0–15). По умолчанию 0.

Выходные данные:
Значение поля: [String] Значение конкретного поля (при указанном поле).
Все поля (Dictionary): [Dictionary<String,String>] Все поля хэша (при пустом поле).";
            sdkComponentIcon = ActivityIcons.Redis;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ConnectionString", "Redis connection string"),
                PropertyBuilder.String("Prop_Key", "Ключ хэша"),
                PropertyBuilder.String("Prop_Field", "Поле хэша (пусто = все поля)"),
                PropertyBuilder.Int("Prop_DatabaseIndex", "Индекс базы данных (0–15)"),
                PropertyBuilder.Variable<string>("Prop_FieldValue", "Значение конкретного поля"),
                PropertyBuilder.Variable<Dictionary<string,string>>("Prop_AllFields", "Все поля хэша")
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
                int dbIndex; int.TryParse(GetPropertyValue<string>(Prop_DatabaseIndex, nameof(Prop_DatabaseIndex), sd), out dbIndex);

                var db = RedisHelper.GetDatabase(connectionString, dbIndex);

                if (!string.IsNullOrWhiteSpace(field))
                {
                    var val = db.HashGet(key, field);
                    SetVariableValue(Prop_FieldValue, val.HasValue ? (string)val : string.Empty, sd);
                    return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Поле получено: {field}" };
                }
                else
                {
                    var entries = db.HashGetAll(key);
                    var dict = entries.ToDictionary(e => (string)e.Name, e => (string)e.Value);
                    SetVariableValue(Prop_AllFields, dict, sd);
                    return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Получено полей: {dict.Count}" };
                }
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка Redis HGET: {ex.Message}" };
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
