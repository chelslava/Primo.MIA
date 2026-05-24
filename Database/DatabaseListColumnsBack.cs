// =============================================================================
// DatabaseListColumnsBack.cs — активность «Database: Список колонок (ListColumns)».
// Возвращает список имён колонок указанной таблицы через schema metadata провайдера.
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность для получения списка колонок таблицы из БД по schema metadata.</summary>
    public class DatabaseListColumnsBack : PrimoComponentTO<DatabaseListColumns>
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
        /// <summary>Имя таблицы, для которой запрашивается список колонок.</summary>
        public string Prop_TableName { get => _propTableName; set { _propTableName = value; InvokePropertyChanged(this, nameof(Prop_TableName)); } }

        private string _propSchemaName;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Optional), System.ComponentModel.DisplayName(ActivityStrings.Field_SchemaName)]
        /// <summary>Имя схемы для уточнения таблицы; если не задано, используется схема по умолчанию.</summary>
        public string Prop_SchemaName { get => _propSchemaName; set { _propSchemaName = value; InvokePropertyChanged(this, nameof(Prop_SchemaName)); } }

        private string _propColumnNames;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ColumnNames)]
        /// <summary>Список имён колонок указанной таблицы.</summary>
        public string Prop_ColumnNames { get => _propColumnNames; set { _propColumnNames = value; InvokePropertyChanged(this, nameof(Prop_ColumnNames)); } }

        private string _propCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Count)]
        /// <summary>Количество колонок в указанной таблице.</summary>
        public string Prop_Count { get => _propCount; set { _propCount = value; InvokePropertyChanged(this, nameof(Prop_Count)); } }

        public DatabaseListColumnsBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DatabaseListColumns;
            sdkComponentHelp = @"Компонент ""Database: Список колонок""
Возвращает список имён колонок указанной таблицы через ADO.NET schema metadata. Полезно для динамического формирования запросов без hardcode имён полей.

Основные:
Провайдер*: [String] Инвариантное имя ADO.NET провайдера (например, System.Data.SqlClient).
Строка подключения*: [String] Строка подключения к базе данных.
Имя таблицы*: [String] Имя таблицы, для которой запрашивается список колонок.

Дополнительные:
Схема: [String] Имя схемы для уточнения таблицы (например, dbo). Если не задана, используется схема по умолчанию.

Выходные данные:
Список колонок: [List<String>] Имена колонок указанной таблицы.
Количество: [Int32] Количество колонок в таблице.";
            sdkComponentIcon = ActivityIcons.Base;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ProviderInvariantName", "ADO.NET provider invariant name"),
                PropertyBuilder.String("Prop_ConnectionString", "Строка подключения к БД"),
                PropertyBuilder.String("Prop_TableName", "Имя таблицы"),
                PropertyBuilder.String("Prop_SchemaName", "Имя схемы (опционально)"),
                PropertyBuilder.Variable<List<string>>("Prop_ColumnNames", "Список колонок"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество колонок")
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

                var columns = DatabaseHelper.GetColumnNames(provider, connectionString, tableName, schemaName);
                SetVariableValue(Prop_ColumnNames, columns, sd);
                SetVariableValue(Prop_Count, columns.Count, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Найдено колонок: {columns.Count}" };
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
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка получения списка колонок: {ex.Message}" };
            }
        }
    }
}
