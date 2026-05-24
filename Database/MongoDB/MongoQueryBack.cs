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
    public class MongoQueryBack : PrimoComponentTO<MongoQuery>
    {
        public override string GroupName { get => ActivityCategories.MongoDB; protected set { } }

        protected override int sdkTimeOut
        {
            get { int s; return int.TryParse(Prop_TimeoutSeconds, out s) && s > 0 ? s * 1000 : 30000; }
            set { }
        }

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

        private string _propFilter = "\"{}\"";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoFilter)]
        public string Prop_Filter { get => _propFilter; set { _propFilter = value; InvokePropertyChanged(this, nameof(Prop_Filter)); } }

        private string _propProjection;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Optional), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoProjection)]
        public string Prop_Projection { get => _propProjection; set { _propProjection = value; InvokePropertyChanged(this, nameof(Prop_Projection)); } }

        private string _propSort;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Optional), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoSort)]
        public string Prop_Sort { get => _propSort; set { _propSort = value; InvokePropertyChanged(this, nameof(Prop_Sort)); } }

        private string _propLimit = "0";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoLimit)]
        public string Prop_Limit { get => _propLimit; set { _propLimit = value; InvokePropertyChanged(this, nameof(Prop_Limit)); } }

        private string _propTimeoutSeconds = "30";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoTimeoutSeconds)]
        public string Prop_TimeoutSeconds { get => _propTimeoutSeconds; set { _propTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_TimeoutSeconds)); } }

        private string _propResultJson;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoResultJson)]
        public string Prop_ResultJson { get => _propResultJson; set { _propResultJson = value; InvokePropertyChanged(this, nameof(Prop_ResultJson)); } }

        private string _propCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoDocumentCount)]
        public string Prop_Count { get => _propCount; set { _propCount = value; InvokePropertyChanged(this, nameof(Prop_Count)); } }

        private string _propHasDocuments;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoHasDocuments)]
        public string Prop_HasDocuments { get => _propHasDocuments; set { _propHasDocuments = value; InvokePropertyChanged(this, nameof(Prop_HasDocuments)); } }

        public MongoQueryBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_MongoQuery;
            sdkComponentHelp = @"Компонент ""MongoDB: Запрос""
Выполняет поиск документов в коллекции MongoDB по заданному фильтру. Поддерживает проекцию, сортировку и ограничение количества результатов.

Основные:
Строка подключения*: [String] MongoDB connection string (например, mongodb://localhost:27017).
База данных*: [String] Имя базы данных.
Коллекция*: [String] Имя коллекции.
Фильтр: [String] JSON-фильтр. По умолчанию {} (все документы).

Дополнительные:
Проекция: [String] JSON-проекция для включения/исключения полей.
Сортировка: [String] JSON-выражение сортировки (например, {""name"": 1}).

Настройки:
Макс. документов: [Int32] Ограничение выборки. 0 — без ограничения.
Таймаут: [Int32] Таймаут выполнения в секундах. По умолчанию 30.

Выходные данные:
Результат (JSON): [String] Массив документов в формате JSON.
Количество документов: [Int32] Количество найденных документов.
Есть документы: [Boolean] True, если найден хотя бы один документ.";
            sdkComponentIcon = ActivityIcons.MongoDB;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ConnectionString", "MongoDB connection string"),
                PropertyBuilder.String("Prop_Database", "Имя базы данных"),
                PropertyBuilder.String("Prop_Collection", "Имя коллекции"),
                PropertyBuilder.String("Prop_Filter", "JSON фильтр ({} = все документы)"),
                PropertyBuilder.String("Prop_Projection", "JSON проекция (опционально)"),
                PropertyBuilder.String("Prop_Sort", "JSON сортировка (опционально)"),
                PropertyBuilder.Int("Prop_Limit", "Макс. документов (0 = все)"),
                PropertyBuilder.Int("Prop_TimeoutSeconds", "Таймаут в секундах"),
                PropertyBuilder.Variable<string>("Prop_ResultJson", "Результат в виде JSON массива"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество найденных документов"),
                PropertyBuilder.Variable<bool>("Prop_HasDocuments", "Есть ли документы в результате")
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
                var projectionJson = GetPropertyValue<string>(Prop_Projection, nameof(Prop_Projection), sd);
                var sortJson = GetPropertyValue<string>(Prop_Sort, nameof(Prop_Sort), sd);
                int limit; int.TryParse(GetPropertyValue<string>(Prop_Limit, nameof(Prop_Limit), sd), out limit);
                int timeout; int.TryParse(GetPropertyValue<string>(Prop_TimeoutSeconds, nameof(Prop_TimeoutSeconds), sd), out timeout);
                if (timeout <= 0) timeout = 30;

                var col = MongoDbHelper.GetCollection(connectionString, database, collection);
                var filter = MongoDbHelper.ParseFilter(filterJson);
                var options = new FindOptions<BsonDocument> { MaxTime = TimeSpan.FromSeconds(timeout) };
                if (!string.IsNullOrWhiteSpace(projectionJson))
                    options.Projection = BsonDocument.Parse(projectionJson);
                if (!string.IsNullOrWhiteSpace(sortJson))
                    options.Sort = BsonDocument.Parse(sortJson);
                if (limit > 0)
                    options.Limit = limit;

                var docs = col.FindSync(filter, options).ToList();
                var resultJson = MongoDbHelper.DocumentsToJson(docs);

                SetVariableValue(Prop_ResultJson, resultJson, sd);
                SetVariableValue(Prop_Count, docs.Count, sd);
                SetVariableValue(Prop_HasDocuments, docs.Count > 0, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Найдено документов: {docs.Count}" };
            }
            catch (MongoException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка MongoDB: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка запроса MongoDB: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            result.ValidateRequired(Prop_ConnectionString, ActivityStrings.Field_MongoConnectionString, ActivityStrings.Error_RedisConnectionRequired);
            result.ValidateRequired(Prop_Database, ActivityStrings.Field_MongoDatabase, ActivityStrings.Error_MongoDatabaseRequired);
            result.ValidateRequired(Prop_Collection, ActivityStrings.Field_MongoCollection, ActivityStrings.Error_MongoCollectionRequired);
            return result;
        }
    }
}
