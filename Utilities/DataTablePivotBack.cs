// =============================================================================
// DataTablePivotBack.cs — активность «DataTable: Pivot / Unpivot» (issue #36).
//
// Pivot: длинная форма → сводная (строки × значения колонки → агрегат).
// Unpivot: широкая форма → длинная. Без внешних зависимостей.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>Активность сводной таблицы и её разбора.</summary>
    public class DataTablePivotBack : PrimoComponentTO<DataTablePivot>
    {
        public override string GroupName { get => ActivityCategories.Utilities; protected set { } }

        protected override int sdkTimeOut { get => 60000; set { } }

        // ── Входные параметры ──────────────────────────────────────────

        private DataTablePivotMode _propMode = DataTablePivotMode.Pivot;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_PivotMode)]
        /// <summary>Режим: сводная или разбор.</summary>
        public DataTablePivotMode Prop_Mode
        {
            get => _propMode;
            set { _propMode = value; InvokePropertyChanged(this, nameof(Prop_Mode)); }
        }

        private string _propSourceTable;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_PivotSource)]
        /// <summary>Исходная таблица.</summary>
        public string Prop_SourceTable
        {
            get => _propSourceTable;
            set { _propSourceTable = value; InvokePropertyChanged(this, nameof(Prop_SourceTable)); }
        }

        // Pivot-режим
        private string _propRowKeyColumn;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_PivotRowKey)]
        /// <summary>Колонка-ключ строк (стают строками результата).</summary>
        public string Prop_RowKeyColumn
        {
            get => _propRowKeyColumn;
            set { _propRowKeyColumn = value; InvokePropertyChanged(this, nameof(Prop_RowKeyColumn)); }
        }

        private string _propPivotColumn;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_PivotPivotColumn)]
        /// <summary>Колонка, уникальные значения которой становятся колонками.</summary>
        public string Prop_PivotColumn
        {
            get => _propPivotColumn;
            set { _propPivotColumn = value; InvokePropertyChanged(this, nameof(Prop_PivotColumn)); }
        }

        private string _propValueColumn;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_PivotValueColumn)]
        /// <summary>Колонка значений для агрегата.</summary>
        public string Prop_ValueColumn
        {
            get => _propValueColumn;
            set { _propValueColumn = value; InvokePropertyChanged(this, nameof(Prop_ValueColumn)); }
        }

        private DataTableAggregation _propAggregation = DataTableAggregation.Sum;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_PivotAggregation)]
        /// <summary>Агрегат ячейки.</summary>
        public DataTableAggregation Prop_Aggregation
        {
            get => _propAggregation;
            set { _propAggregation = value; InvokePropertyChanged(this, nameof(Prop_Aggregation)); }
        }

        // Unpivot-режим
        private string _propKeyColumns;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_PivotKeyColumns)]
        /// <summary>Ключевые колонки, копируемые в каждую строку (через запятую).</summary>
        public string Prop_KeyColumns
        {
            get => _propKeyColumns;
            set { _propKeyColumns = value; InvokePropertyChanged(this, nameof(Prop_KeyColumns)); }
        }

        private string _propValueColumns;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_PivotValueColumns)]
        /// <summary>Разворачиваемые колонки (через запятую).</summary>
        public string Prop_ValueColumns
        {
            get => _propValueColumns;
            set { _propValueColumns = value; InvokePropertyChanged(this, nameof(Prop_ValueColumns)); }
        }

        private string _propVariableColumnName;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_PivotVarName)]
        /// <summary>Имя колонки «имя развёрнутой колонки» (по умолчанию Параметр).</summary>
        public string Prop_VariableColumnName
        {
            get => _propVariableColumnName;
            set { _propVariableColumnName = value; InvokePropertyChanged(this, nameof(Prop_VariableColumnName)); }
        }

        private string _propValueColumnName;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_PivotValueName)]
        /// <summary>Имя колонки значения (по умолчанию Значение).</summary>
        public string Prop_ValueColumnName
        {
            get => _propValueColumnName;
            set { _propValueColumnName = value; InvokePropertyChanged(this, nameof(Prop_ValueColumnName)); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propResult;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_PivotResult)]
        /// <summary>Результат (имя выходной переменной).</summary>
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); }
        }

        public DataTablePivotBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DataTablePivot;
            sdkComponentHelp = @"Компонент ""DataTable: Pivot / Unpivot""
Сводная таблица и разбор сводной (без зависимостей).

Основные:
Режим: [Enum] Pivot (сводная) / Unpivot (разбор).
Исходная таблица*: [DataTable]

Pivot:
Колонка строк: [String] Уникальные значения → строки результата.
Pivot-колонка: [String] Уникальные значения → колонки результата.
Колонка значений: [String] Что агрегировать.
Агрегат: [Enum] Сумма / Количество / Минимум / Максимум / Первое.

Unpivot:
Ключевые колонки: [String] Копируются в каждую строку (через запятую).
Разворачиваемые колонки: [String] Их имена и значения — в строки.
Имя колонки параметра: [String] По умолчанию «Параметр».
Имя колонки значения: [String] По умолчанию «Значение».

Выходные данные:
Результат: [DataTable]";
            sdkComponentIcon = ActivityIcons.Table;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Enum<DataTablePivotMode>("Prop_Mode", "Режим: сводная или разбор"),
                PropertyBuilder.Variable<DataTable>("Prop_SourceTable", "Исходная таблица"),
                PropertyBuilder.String("Prop_RowKeyColumn", "Pivot: колонка строк"),
                PropertyBuilder.String("Prop_PivotColumn", "Pivot: pivot-колонка"),
                PropertyBuilder.String("Prop_ValueColumn", "Pivot: колонка значений"),
                PropertyBuilder.Enum<DataTableAggregation>("Prop_Aggregation", "Pivot: агрегат"),
                PropertyBuilder.String("Prop_KeyColumns", "Unpivot: ключевые колонки, через запятую"),
                PropertyBuilder.String("Prop_ValueColumns", "Unpivot: разворачиваемые колонки, через запятую"),
                PropertyBuilder.String("Prop_VariableColumnName", "Unpivot: имя колонки параметра"),
                PropertyBuilder.String("Prop_ValueColumnName", "Unpivot: имя колонки значения"),
                PropertyBuilder.Variable<DataTable>("Prop_Result", "Результат")
            };

            InitClass(container);

            this.Prop_SourceTable = "\"\"";
            this.Prop_RowKeyColumn = "\"\"";
            this.Prop_PivotColumn = "\"\"";
            this.Prop_ValueColumn = "\"\"";
            this.Prop_KeyColumns = "\"\"";
            this.Prop_ValueColumns = "\"\"";
            this.Prop_VariableColumnName = "\"Параметр\"";
            this.Prop_ValueColumnName = "\"Значение\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new DataTableJoinLogic();
            try
            {
                var source = (DataTable)GetPropertyValue<object>(
                    this.Prop_SourceTable, nameof(Prop_SourceTable), sd);

                DataTable result;
                if (this.Prop_Mode == DataTablePivotMode.Unpivot)
                {
                    var keyColumns = SplitColumns(GetPropertyValue<string>(this.Prop_KeyColumns, nameof(Prop_KeyColumns), sd));
                    var valueColumns = SplitColumns(GetPropertyValue<string>(this.Prop_ValueColumns, nameof(Prop_ValueColumns), sd));
                    var varName = GetPropertyValue<string>(this.Prop_VariableColumnName, nameof(Prop_VariableColumnName), sd);
                    var valueName = GetPropertyValue<string>(this.Prop_ValueColumnName, nameof(Prop_ValueColumnName), sd);

                    result = logic.Unpivot(source, keyColumns, valueColumns, varName, valueName);
                }
                else
                {
                    var rowKey = GetPropertyValue<string>(this.Prop_RowKeyColumn, nameof(Prop_RowKeyColumn), sd);
                    var pivotColumn = GetPropertyValue<string>(this.Prop_PivotColumn, nameof(Prop_PivotColumn), sd);
                    var valueColumn = GetPropertyValue<string>(this.Prop_ValueColumn, nameof(Prop_ValueColumn), sd);

                    result = logic.Pivot(source, rowKey, pivotColumn, valueColumn, this.Prop_Aggregation);
                }

                SetVariableValue(this.Prop_Result, result, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[DataTable: Pivot/Unpivot] {result.Rows.Count} строк, {result.Columns.Count} колонок"
                };
            }
            catch (InvalidOperationException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка Pivot/Unpivot: {ex.Message}"
                };
            }
        }

        private static string[] SplitColumns(string raw)
        {
            return (raw ?? string.Empty)
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToArray();
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_SourceTable, ActivityStrings.Field_PivotSource, ActivityStrings.Error_JoinTableRequired);
            return ret;
        }
    }
}
