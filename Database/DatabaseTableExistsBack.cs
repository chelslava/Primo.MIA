// =============================================================================
// DatabaseTableExistsBack.cs — активность «Database: Таблица существует (TableExists)».
// Проверяет наличие таблицы или представления в БД через schema metadata провайдера.
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность для проверки существования таблицы или представления в БД.</summary>
    public class DatabaseTableExistsBack : PrimoComponentTO<DatabaseTableExists>
    {
        public override string GroupName { get => ActivityCategories.Database; protected set { } }

        protected override int sdkTimeOut { get => 30000; set { } }

        private string _propProviderInvariantName = "\"" + DatabaseHelper.DefaultProviderInvariantName + "\"";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_DbProviderInvariantName)]
        /// <summary>Инвариантное имя ADO.NET провайдера (например, System.Data.SqlClient).</summary>
        public string Prop_ProviderInvariantName { get => _propProviderInvariantName; set { _propProviderInvariantName = value; InvokePropertyChanged(this, nameof(Prop_ProviderInvariantName)); } }

        private string _propConnectionString;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_ConnectionString)]
        /// <summary>Строка подключения к базе данных.</summary>
        public string Prop_ConnectionString { get => _propConnectionString; set { _propConnectionString = value; InvokePropertyChanged(this, nameof(Prop_ConnectionString)); } }

        private string _propTableName;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_TableName)]
        /// <summary>Имя проверяемой таблицы или представления.</summary>
        public string Prop_TableName { get => _propTableName; set { _propTableName = value; InvokePropertyChanged(this, nameof(Prop_TableName)); } }

        private string _propSchemaName;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Optional), System.ComponentModel.DisplayName(ActivityStrings.Field_SchemaName)]
        /// <summary>Имя схемы для фильтрации; если не задано, поиск ведётся по всем схемам.</summary>
        public string Prop_SchemaName { get => _propSchemaName; set { _propSchemaName = value; InvokePropertyChanged(this, nameof(Prop_SchemaName)); } }

        private bool _propIncludeViews;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Optional), System.ComponentModel.DisplayName(ActivityStrings.Field_IncludeViews)]
        /// <summary>Включать представления (VIEW) в область поиска наряду с таблицами.</summary>
        public bool Prop_IncludeViews { get => _propIncludeViews; set { _propIncludeViews = value; InvokePropertyChanged(this, nameof(Prop_IncludeViews)); } }

        private string _propExists;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Exists)]
        /// <summary>Признак того, что таблица или представление с указанным именем существует в БД.</summary>
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
            catch (System.Data.Common.DbException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка БД: {ex.Message}" };
            }
            catch (InvalidOperationException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Недопустимая операция: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка проверки таблицы: {ex.Message}" };
            }
        }
    }
}
