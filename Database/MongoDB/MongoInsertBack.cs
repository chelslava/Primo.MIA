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
    public class MongoInsertBack : PrimoComponentTO<MongoInsert>
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

        private string _propDocument;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoDocument)]
        public string Prop_Document { get => _propDocument; set { _propDocument = value; InvokePropertyChanged(this, nameof(Prop_Document)); } }

        private string _propInsertedCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoInsertedCount)]
        public string Prop_InsertedCount { get => _propInsertedCount; set { _propInsertedCount = value; InvokePropertyChanged(this, nameof(Prop_InsertedCount)); } }

        private string _propInsertedIds;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoInsertedIds)]
        public string Prop_InsertedIds { get => _propInsertedIds; set { _propInsertedIds = value; InvokePropertyChanged(this, nameof(Prop_InsertedIds)); } }

        public MongoInsertBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_MongoInsert;
            sdkComponentHelp = @"Компонент ""MongoDB: Вставить документ""
Вставляет один или несколько документов в коллекцию MongoDB. Если документ — JSON-объект, выполняется InsertOne; если JSON-массив — InsertMany.

Основные:
Строка подключения*: [String] MongoDB connection string.
База данных*: [String] Имя базы данных.
Коллекция*: [String] Имя коллекции.
Документ (JSON)*: [String] JSON-объект или JSON-массив объектов для вставки.

Выходные данные:
Вставлено документов: [Int32] Количество вставленных документов.
ID вставленных документов: [List<String>] Список _id вставленных документов.";
            sdkComponentIcon = ActivityIcons.MongoDB;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ConnectionString", "MongoDB connection string"),
                PropertyBuilder.String("Prop_Database", "Имя базы данных"),
                PropertyBuilder.String("Prop_Collection", "Имя коллекции"),
                PropertyBuilder.String("Prop_Document", "JSON-объект или JSON-массив для вставки"),
                PropertyBuilder.Variable<int>("Prop_InsertedCount", "Количество вставленных документов"),
                PropertyBuilder.Variable<List<string>>("Prop_InsertedIds", "ID вставленных документов")
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
                var documentJson = GetPropertyValue<string>(Prop_Document, nameof(Prop_Document), sd);

                if (string.IsNullOrWhiteSpace(documentJson))
                    throw new InvalidOperationException(ActivityStrings.Error_MongoDocumentRequired);

                var col = MongoDbHelper.GetCollection(connectionString, database, collection);
                var trimmed = documentJson.TrimStart();
                var insertedIds = new List<string>();

                if (trimmed.StartsWith("["))
                {
                    var array = BsonSerializer.Deserialize<BsonArray>(documentJson);
                    var docs = array.Select(v => v.AsBsonDocument).ToList();
                    col.InsertMany(docs);
                    insertedIds = docs.Select(d => d["_id"].ToString()).ToList();
                }
                else
                {
                    var doc = BsonDocument.Parse(documentJson);
                    col.InsertOne(doc);
                    insertedIds.Add(doc["_id"].ToString());
                }

                SetVariableValue(Prop_InsertedCount, insertedIds.Count, sd);
                SetVariableValue(Prop_InsertedIds, insertedIds, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Вставлено документов: {insertedIds.Count}" };
            }
            catch (MongoException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка MongoDB: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка вставки в MongoDB: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            result.ValidateRequired(Prop_ConnectionString, ActivityStrings.Field_MongoConnectionString, ActivityStrings.Error_RedisConnectionRequired);
            result.ValidateRequired(Prop_Database, ActivityStrings.Field_MongoDatabase, ActivityStrings.Error_MongoDatabaseRequired);
            result.ValidateRequired(Prop_Collection, ActivityStrings.Field_MongoCollection, ActivityStrings.Error_MongoCollectionRequired);
            result.ValidateRequired(Prop_Document, ActivityStrings.Field_MongoDocument, ActivityStrings.Error_MongoDocumentRequired);
            return result;
        }
    }
}
