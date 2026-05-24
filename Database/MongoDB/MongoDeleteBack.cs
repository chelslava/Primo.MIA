using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using MongoDB.Driver;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    public class MongoDeleteBack : PrimoComponentTO<MongoDelete>
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

        private bool _propDeleteMany;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoDeleteMany)]
        public bool Prop_DeleteMany { get => _propDeleteMany; set { _propDeleteMany = value; InvokePropertyChanged(this, nameof(Prop_DeleteMany)); } }

        private string _propDeletedCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MongoDeletedCount)]
        public string Prop_DeletedCount { get => _propDeletedCount; set { _propDeletedCount = value; InvokePropertyChanged(this, nameof(Prop_DeletedCount)); } }

        public MongoDeleteBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_MongoDelete;
            sdkComponentHelp = @"Компонент ""MongoDB: Удалить документы""
Удаляет один или все совпадающие документы из коллекции MongoDB по заданному фильтру.

Основные:
Строка подключения*: [String] MongoDB connection string.
База данных*: [String] Имя базы данных.
Коллекция*: [String] Имя коллекции.
Фильтр*: [String] JSON-фильтр для поиска удаляемых документов.

Настройки:
Удалить все совпадения: [Boolean] Если True — DeleteMany, иначе DeleteOne.

Выходные данные:
Удалено документов: [Int32] Количество удалённых документов.";
            sdkComponentIcon = ActivityIcons.MongoDB;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ConnectionString", "MongoDB connection string"),
                PropertyBuilder.String("Prop_Database", "Имя базы данных"),
                PropertyBuilder.String("Prop_Collection", "Имя коллекции"),
                PropertyBuilder.String("Prop_Filter", "JSON фильтр"),
                PropertyBuilder.BooleanObject("Prop_DeleteMany", "True = DeleteMany, False = DeleteOne"),
                PropertyBuilder.Variable<int>("Prop_DeletedCount", "Количество удалённых документов")
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

                long deleted;
                if (Prop_DeleteMany)
                    deleted = col.DeleteMany(filter).DeletedCount;
                else
                    deleted = col.DeleteOne(filter).DeletedCount;

                SetVariableValue(Prop_DeletedCount, (int)deleted, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Удалено документов: {deleted}" };
            }
            catch (MongoException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка MongoDB: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка удаления MongoDB: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            result.ValidateRequired(Prop_ConnectionString, ActivityStrings.Field_MongoConnectionString, ActivityStrings.Error_RedisConnectionRequired);
            result.ValidateRequired(Prop_Database, ActivityStrings.Field_MongoDatabase, ActivityStrings.Error_MongoDatabaseRequired);
            result.ValidateRequired(Prop_Collection, ActivityStrings.Field_MongoCollection, ActivityStrings.Error_MongoCollectionRequired);
            result.ValidateRequired(Prop_Filter, ActivityStrings.Field_MongoFilter, "Фильтр обязателен для удаления");
            return result;
        }
    }
}
