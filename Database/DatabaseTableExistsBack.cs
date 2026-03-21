using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    public class DatabaseTableExistsBack : PrimoComponentTO<DatabaseTableExists>
    {
        public override string GroupName { get => ActivityCategories.Database; protected set { } }

        protected override int sdkTimeOut { get => 30000; set { } }

        private string _propProviderInvariantName = "\"" + DatabaseHelper.DefaultProviderInvariantName + "\"";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_DbProviderInvariantName)]
        public string Prop_ProviderInvariantName { get => _propProviderInvariantName; set { _propProviderInvariantName = value; InvokePropertyChanged(this, nameof(Prop_ProviderInvariantName)); } }

        private string _propConnectionString;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_ConnectionString)]
        public string Prop_ConnectionString { get => _propConnectionString; set { _propConnectionString = value; InvokePropertyChanged(this, nameof(Prop_ConnectionString)); } }

        private string _propTableName;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_TableName)]
        public string Prop_TableName { get => _propTableName; set { _propTableName = value; InvokePropertyChanged(this, nameof(Prop_TableName)); } }

        private string _propSchemaName;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Optional), System.ComponentModel.DisplayName(ActivityStrings.Field_SchemaName)]
        public string Prop_SchemaName { get => _propSchemaName; set { _propSchemaName = value; InvokePropertyChanged(this, nameof(Prop_SchemaName)); } }

        private bool _propIncludeViews;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Optional), System.ComponentModel.DisplayName(ActivityStrings.Field_IncludeViews)]
        public bool Prop_IncludeViews { get => _propIncludeViews; set { _propIncludeViews = value; InvokePropertyChanged(this, nameof(Prop_IncludeViews)); } }

        private string _propExists;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Exists)]
        public string Prop_Exists { get => _propExists; set { _propExists = value; InvokePropertyChanged(this, nameof(Prop_Exists)); } }

        public DatabaseTableExistsBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DatabaseTableExists;
            sdkComponentHelp = "Проверяет существование таблицы или view в БД через schema metadata.";
            sdkComponentIcon = ActivityIcons.Base;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ProviderInvariantName", "ADO.NET provider invariant name"),
                PropertyBuilder.String("Prop_ConnectionString", "Строка подключения к БД"),
                PropertyBuilder.String("Prop_TableName", "Имя таблицы"),
                PropertyBuilder.String("Prop_SchemaName", "Имя схемы (опционально)"),
                PropertyBuilder.BooleanObject("Prop_IncludeViews", "Учитывать представления"),
                PropertyBuilder.Variable<bool>("Prop_Exists", "Таблица существует")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var provider = GetPropertyValue<string>(Prop_ProviderInvariantName, nameof(Prop_ProviderInvariantName), sd);
                var connectionString = GetPropertyValue<string>(Prop_ConnectionString, nameof(Prop_ConnectionString), sd);
                var tableName = GetPropertyValue<string>(Prop_TableName, nameof(Prop_TableName), sd);
                var schemaName = GetPropertyValue<string>(Prop_SchemaName, nameof(Prop_SchemaName), sd);

                var exists = DatabaseHelper.TableExists(provider, connectionString, tableName, schemaName, Prop_IncludeViews);
                SetVariableValue(Prop_Exists, exists, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = exists ? "Таблица найдена" : "Таблица не найдена" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка проверки таблицы: {ex.Message}" };
            }
        }
    }
}
