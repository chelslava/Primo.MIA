using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    public class DatabaseListTablesBack : PrimoComponentTO<DatabaseListTables>
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

        private string _propSchemaName;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Optional), System.ComponentModel.DisplayName(ActivityStrings.Field_SchemaName)]
        public string Prop_SchemaName { get => _propSchemaName; set { _propSchemaName = value; InvokePropertyChanged(this, nameof(Prop_SchemaName)); } }

        private bool _propIncludeViews;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Optional), System.ComponentModel.DisplayName(ActivityStrings.Field_IncludeViews)]
        public bool Prop_IncludeViews { get => _propIncludeViews; set { _propIncludeViews = value; InvokePropertyChanged(this, nameof(Prop_IncludeViews)); } }

        private string _propTableNames;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_TableNames)]
        public string Prop_TableNames { get => _propTableNames; set { _propTableNames = value; InvokePropertyChanged(this, nameof(Prop_TableNames)); } }

        private string _propCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Count)]
        public string Prop_Count { get => _propCount; set { _propCount = value; InvokePropertyChanged(this, nameof(Prop_Count)); } }

        public DatabaseListTablesBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DatabaseListTables;
            sdkComponentHelp = "Возвращает список таблиц БД по schema metadata.";
            sdkComponentIcon = ActivityIcons.Table;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ProviderInvariantName", "ADO.NET provider invariant name"),
                PropertyBuilder.String("Prop_ConnectionString", "Строка подключения к БД"),
                PropertyBuilder.String("Prop_SchemaName", "Имя схемы (опционально)"),
                PropertyBuilder.BooleanObject("Prop_IncludeViews", "Учитывать представления"),
                PropertyBuilder.Variable<List<string>>("Prop_TableNames", "Список таблиц"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество таблиц")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var provider = GetPropertyValue<string>(Prop_ProviderInvariantName, nameof(Prop_ProviderInvariantName), sd);
                var connectionString = GetPropertyValue<string>(Prop_ConnectionString, nameof(Prop_ConnectionString), sd);
                var schemaName = GetPropertyValue<string>(Prop_SchemaName, nameof(Prop_SchemaName), sd);

                var tables = DatabaseHelper.GetTableNames(provider, connectionString, schemaName, Prop_IncludeViews);
                SetVariableValue(Prop_TableNames, tables, sd);
                SetVariableValue(Prop_Count, tables.Count, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Найдено таблиц: {tables.Count}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка получения списка таблиц: {ex.Message}" };
            }
        }
    }
}
