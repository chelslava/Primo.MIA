using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using MongoDB.Bson;
using MongoDB.Driver;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    public class MongoUpdateBack : PrimoComponentTO<MongoUpdate>
    {
        public override string GroupName { get => ActivityCategories.MongoDB; protected set { } }

        protected override int sdkTimeOut { get => 30000; set { } }

        private string _propConnectionString;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoConnectionString)]
        public string Prop_ConnectionString { get => _propConnectionString; set { _propConnectionString = value; InvokePropertyChanged(this, nameof(Prop_ConnectionString)); } }

        private string _propDatabase;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoDatabase)]
        public string Prop_Database { get => _propDatabase; set { _propDatabase = value; InvokePropertyChanged(this, nameof(Prop_Database)); } }

        private string _propCollection;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoCollection)]
        public string Prop_Collection { get => _propCollection; set { _propCollection = value; InvokePropertyChanged(this, nameof(Prop_Collection)); } }

        private string _propFilter;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoFilter)]
        public string Prop_Filter { get => _propFilter; set { _propFilter = value; InvokePropertyChanged(this, nameof(Prop_Filter)); } }

        private string _propUpdate;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoUpdate)]
        public string Prop_Update { get => _propUpdate; set { _propUpdate = value; InvokePropertyChanged(this, nameof(Prop_Update)); } }

        private bool _propUpdateMany;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoUpdateMany)]
        public bool Prop_UpdateMany { get => _propUpdateMany; set { _propUpdateMany = value; InvokePropertyChanged(this, nameof(Prop_UpdateMany)); } }

        private string _propMatchedCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoMatchedCount)]
        public string Prop_MatchedCount { get => _propMatchedCount; set { _propMatchedCount = value; InvokePropertyChanged(this, nameof(Prop_MatchedCount)); } }

        private string _propModifiedCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoModifiedCount)]
        public string Prop_ModifiedCount { get => _propModifiedCount; set { _propModifiedCount = value; InvokePropertyChanged(this, nameof(Prop_ModifiedCount)); } }

        public MongoUpdateBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_MongoUpdate;
            sdkComponentHelp = @"Компонент ""MongoDB: Обновить документы""
Обновляет один или все совпадающие документы в коллекции MongoDB по заданному фильтру.

Основные:
Строка подключения*: [String] MongoDB connection string.
База данных*: [String] Имя базы данных.
Коллекция*: [String] Имя коллекции.
Фильтр*: [String] JSON-фильтр для поиска документов.
Обновление (JSON)*: [String] JSON-выражение обновления (например, {""$set"": {""field"": ""value""}}).

Настройки:
Обновить все совпадения: [Boolean] Если True — UpdateMany, иначе UpdateOne.

Выходные данные:
Совпало документов: [Int32] Количество документов, совпавших с фильтром.
Обновлено документов: [Int32] Количество фактически изменённых документов.";
            sdkComponentIcon = ActivityIcons.MongoDB;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ConnectionString", "MongoDB connection string"),
                PropertyBuilder.String("Prop_Database", "Имя базы данных"),
                PropertyBuilder.String("Prop_Collection", "Имя коллекции"),
                PropertyBuilder.String("Prop_Filter", "JSON фильтр"),
                PropertyBuilder.String("Prop_Update", "JSON выражение обновления ($set, $inc, ...)"),
                PropertyBuilder.BooleanObject("Prop_UpdateMany", "True = UpdateMany, False = UpdateOne"),
                PropertyBuilder.Variable<int>("Prop_MatchedCount", "Совпало документов"),
                PropertyBuilder.Variable<int>("Prop_ModifiedCount", "Обновлено документов")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var connectionString = GetPropertyValue<string>(Prop_ConnectionString, nameof(Prop_ConnectionString), sd);
                var database = GetPropertyValue<string>(Prop_Database, nameof(Prop_Database), sd);
                var collection = GetPropertyValue<string>(Prop_Collection, nameof(Prop_Collection), sd);
                var filterJson = GetPropertyValue<string>(Prop_Filter, nameof(Prop_Filter), sd);
                var updateJson = GetPropertyValue<string>(Prop_Update, nameof(Prop_Update), sd);

                var col = MongoDbHelper.GetCollection(connectionString, database, collection);
                var filter = MongoDbHelper.ParseFilter(filterJson);
                var update = BsonDocument.Parse(updateJson);

                long matched, modified;
                if (Prop_UpdateMany)
                {
                    var r = col.UpdateMany(filter, update);
                    matched = r.MatchedCount; modified = r.ModifiedCount;
                }
                else
                {
                    var r = col.UpdateOne(filter, update);
                    matched = r.MatchedCount; modified = r.ModifiedCount;
                }

                SetVariableValue(Prop_MatchedCount, (int)matched, sd);
                SetVariableValue(Prop_ModifiedCount, (int)modified, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Совпало: {matched}, обновлено: {modified}" };
            }
            catch (MongoException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка MongoDB: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка обновления MongoDB: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            result.ValidateRequired(Prop_ConnectionString, ActivityStrings.Field_MongoConnectionString, ActivityStrings.Error_RedisConnectionRequired);
            result.ValidateRequired(Prop_Database, ActivityStrings.Field_MongoDatabase, ActivityStrings.Error_MongoDatabaseRequired);
            result.ValidateRequired(Prop_Collection, ActivityStrings.Field_MongoCollection, ActivityStrings.Error_MongoCollectionRequired);
            result.ValidateRequired(Prop_Filter, ActivityStrings.Field_MongoFilter, "Фильтр обязателен для обновления");
            result.ValidateRequired(Prop_Update, ActivityStrings.Field_MongoUpdate, ActivityStrings.Error_MongoUpdateRequired);
            return result;
        }
    }
}
