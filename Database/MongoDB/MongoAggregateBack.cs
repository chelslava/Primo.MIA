using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    public class MongoAggregateBack : PrimoComponentTO<MongoAggregate>
    {
        public override string GroupName { get => ActivityCategories.MongoDB; protected set { } }

        protected override int sdkTimeOut { get => 60000; set { } }

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

        private string _propPipeline;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoPipeline)]
        public string Prop_Pipeline { get => _propPipeline; set { _propPipeline = value; InvokePropertyChanged(this, nameof(Prop_Pipeline)); } }

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

        public MongoAggregateBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_MongoAggregate;
            sdkComponentHelp = @"Компонент ""MongoDB: Агрегация""
Выполняет агрегационный pipeline в коллекции MongoDB и возвращает результат в виде JSON-массива.

Основные:
Строка подключения*: [String] MongoDB connection string.
База данных*: [String] Имя базы данных.
Коллекция*: [String] Имя коллекции.
Pipeline (JSON)*: [String] JSON-массив стадий агрегации (например, [{""$match"":{}},{""$group"":{...}}]).

Выходные данные:
Результат (JSON): [String] Массив результирующих документов в формате JSON.
Количество документов: [Int32] Количество документов в результате.";
            sdkComponentIcon = ActivityIcons.MongoDB;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ConnectionString", "MongoDB connection string"),
                PropertyBuilder.String("Prop_Database", "Имя базы данных"),
                PropertyBuilder.String("Prop_Collection", "Имя коллекции"),
                PropertyBuilder.String("Prop_Pipeline", "JSON-массив стадий агрегации"),
                PropertyBuilder.Variable<string>("Prop_ResultJson", "Результат агрегации в виде JSON массива"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество документов в результате")
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
                var pipelineJson = GetPropertyValue<string>(Prop_Pipeline, nameof(Prop_Pipeline), sd);

                var col = MongoDbHelper.GetCollection(connectionString, database, collection);
                var pipelineArray = BsonSerializer.Deserialize<BsonArray>(pipelineJson);
                var stages = pipelineArray.Select(v => v.AsBsonDocument).ToList();
                var pipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create(stages);

                var docs = col.Aggregate(pipeline).ToList();
                var resultJson = MongoDbHelper.DocumentsToJson(docs);

                SetVariableValue(Prop_ResultJson, resultJson, sd);
                SetVariableValue(Prop_Count, docs.Count, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Агрегация завершена. Документов: {docs.Count}" };
            }
            catch (MongoException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка MongoDB: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка агрегации MongoDB: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            result.ValidateRequired(Prop_ConnectionString, ActivityStrings.Field_MongoConnectionString, ActivityStrings.Error_RedisConnectionRequired);
            result.ValidateRequired(Prop_Database, ActivityStrings.Field_MongoDatabase, ActivityStrings.Error_MongoDatabaseRequired);
            result.ValidateRequired(Prop_Collection, ActivityStrings.Field_MongoCollection, ActivityStrings.Error_MongoCollectionRequired);
            result.ValidateRequired(Prop_Pipeline, ActivityStrings.Field_MongoPipeline, ActivityStrings.Error_MongoPipelineRequired);
            return result;
        }
    }
}
