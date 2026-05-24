using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using MongoDB.Driver;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    public class MongoCountBack : PrimoComponentTO<MongoCount>
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

        private string _propFilter = "\"{}\"";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoFilter)]
        public string Prop_Filter { get => _propFilter; set { _propFilter = value; InvokePropertyChanged(this, nameof(Prop_Filter)); } }

        private string _propCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoDocumentCount)]
        public string Prop_Count { get => _propCount; set { _propCount = value; InvokePropertyChanged(this, nameof(Prop_Count)); } }

        public MongoCountBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_MongoCount;
            sdkComponentHelp = @"Компонент ""MongoDB: Количество документов""
Возвращает количество документов в коллекции, соответствующих заданному фильтру.

Основные:
Строка подключения*: [String] MongoDB connection string.
База данных*: [String] Имя базы данных.
Коллекция*: [String] Имя коллекции.
Фильтр: [String] JSON-фильтр. По умолчанию {} (все документы).

Выходные данные:
Количество документов: [Int32] Количество документов, соответствующих фильтру.";
            sdkComponentIcon = ActivityIcons.MongoDB;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ConnectionString", "MongoDB connection string"),
                PropertyBuilder.String("Prop_Database", "Имя базы данных"),
                PropertyBuilder.String("Prop_Collection", "Имя коллекции"),
                PropertyBuilder.String("Prop_Filter", "JSON фильтр ({} = все документы)"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество документов")
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

                var col = MongoDbHelper.GetCollection(connectionString, database, collection);
                var filter = MongoDbHelper.ParseFilter(filterJson);
                var count = col.CountDocuments(filter);

                SetVariableValue(Prop_Count, (int)count, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Количество документов: {count}" };
            }
            catch (MongoException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка MongoDB: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка подсчёта MongoDB: {ex.Message}" };
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
