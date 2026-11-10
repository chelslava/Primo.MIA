// =============================================================================
// DataTableJoinBack.cs — активность «DataTable: Соединение» (issue #36).
//
// SQL-подобное соединение двух DataTable по ключевым колонкам
// (inner/left/right/full), без внешних зависимостей.
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
    /// <summary>Активность соединения двух DataTable.</summary>
    public class DataTableJoinBack : PrimoComponentTO<DataTableJoin>
    {
        public override string GroupName { get => ActivityCategories.Utilities; protected set { } }

        protected override int sdkTimeOut { get => 60000; set { } }

        // ── Входные параметры ──────────────────────────────────────────

        private string _propLeftTable;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_JoinLeftTable)]
        /// <summary>Левая таблица.</summary>
        public string Prop_LeftTable
        {
            get => _propLeftTable;
            set { _propLeftTable = value; InvokePropertyChanged(this, nameof(Prop_LeftTable)); }
        }

        private string _propRightTable;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_JoinRightTable)]
        /// <summary>Правая таблица.</summary>
        public string Prop_RightTable
        {
            get => _propRightTable;
            set { _propRightTable = value; InvokePropertyChanged(this, nameof(Prop_RightTable)); }
        }

        private string _propLeftKeys;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_JoinLeftKeys)]
        /// <summary>Ключевые колонки левой таблицы (через запятую).</summary>
        public string Prop_LeftKeys
        {
            get => _propLeftKeys;
            set { _propLeftKeys = value; InvokePropertyChanged(this, nameof(Prop_LeftKeys)); }
        }

        private string _propRightKeys;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_JoinRightKeys)]
        /// <summary>Ключевые колонки правой таблицы (через запятую, по порядку).</summary>
        public string Prop_RightKeys
        {
            get => _propRightKeys;
            set { _propRightKeys = value; InvokePropertyChanged(this, nameof(Prop_RightKeys)); }
        }

        private DataTableJoinKind _propJoinKind = DataTableJoinKind.Inner;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_JoinKind)]
        /// <summary>Тип соединения.</summary>
        public DataTableJoinKind Prop_JoinKind
        {
            get => _propJoinKind;
            set { _propJoinKind = value; InvokePropertyChanged(this, nameof(Prop_JoinKind)); }
        }

        private string _propRightSuffix;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_JoinSuffix)]
        /// <summary>Суффикс для конфликтов имён колонок (по умолчанию _right).</summary>
        public string Prop_RightSuffix
        {
            get => _propRightSuffix;
            set { _propRightSuffix = value; InvokePropertyChanged(this, nameof(Prop_RightSuffix)); }
        }

        private bool _propCaseSensitive;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_JoinCaseSensitive)]
        /// <summary>Сравнивать строковые ключи с учётом регистра.</summary>
        public bool Prop_CaseSensitive
        {
            get => _propCaseSensitive;
            set { _propCaseSensitive = value; InvokePropertyChanged(this, nameof(Prop_CaseSensitive)); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propResult;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_JoinResult)]
        /// <summary>Соединённая таблица (имя выходной переменной).</summary>
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); }
        }

        public DataTableJoinBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DataTableJoin;
            sdkComponentHelp = @"Компонент ""DataTable: Соединение""
SQL-подобное соединение двух DataTable по ключевым колонкам (без зависимостей).

Основные:
Левая таблица*: [DataTable]
Правая таблица*: [DataTable]
Ключевые колонки (лево)*: [String] Через запятую: Id,Code
Ключевые колонки (право)*: [String] Через запятую, по порядку соответствуют левым.

Настройки:
Тип соединения: [Enum] Inner / Left / Right / Full.
Суффикс: [String] Для конфликтов имён колонок (по умолчанию _right).
Учитывать регистр: [Boolean] Для строковых ключей.

NULL-ключи не совпадают (семантика SQL). Ключевые колонки правой таблицы в результат не дублируются.

Выходные данные:
Результат: [DataTable]";
            sdkComponentIcon = ActivityIcons.Table;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<DataTable>("Prop_LeftTable", "Левая таблица"),
                PropertyBuilder.Variable<DataTable>("Prop_RightTable", "Правая таблица"),
                PropertyBuilder.String("Prop_LeftKeys", "Ключевые колонки левой, через запятую"),
                PropertyBuilder.String("Prop_RightKeys", "Ключевые колонки правой, через запятую"),
                PropertyBuilder.Enum<DataTableJoinKind>("Prop_JoinKind", "Тип соединения"),
                PropertyBuilder.String("Prop_RightSuffix", "Суффикс конфликтов имён (по умолчанию _right)"),
                PropertyBuilder.BooleanObject("Prop_CaseSensitive", "Учитывать регистр ключей"),
                PropertyBuilder.Variable<DataTable>("Prop_Result", "Соединённая таблица")
            };

            InitClass(container);

            this.Prop_LeftTable = "\"\"";
            this.Prop_RightTable = "\"\"";
            this.Prop_LeftKeys = "\"Id\"";
            this.Prop_RightKeys = "\"Id\"";
            this.Prop_RightSuffix = "\"_right\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new DataTableJoinLogic();
            try
            {
                // DataTable приходит как object (по образцу JsonDataTableBack)
                var left = (DataTable)GetPropertyValue<object>(this.Prop_LeftTable, nameof(Prop_LeftTable), sd);
                var right = (DataTable)GetPropertyValue<object>(this.Prop_RightTable, nameof(Prop_RightTable), sd);
                var leftKeysRaw = GetPropertyValue<string>(this.Prop_LeftKeys, nameof(Prop_LeftKeys), sd);
                var rightKeysRaw = GetPropertyValue<string>(this.Prop_RightKeys, nameof(Prop_RightKeys), sd);
                var suffix = GetPropertyValue<string>(this.Prop_RightSuffix, nameof(Prop_RightSuffix), sd);

                var leftKeys = SplitColumns(leftKeysRaw);
                var rightKeys = SplitColumns(rightKeysRaw);

                var result = logic.Join(left, right, leftKeys, rightKeys,
                    this.Prop_JoinKind, suffix, this.Prop_CaseSensitive);

                SetVariableValue(this.Prop_Result, result, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[DataTable: Соединение] {result.Rows.Count} строк, {result.Columns.Count} колонок ({this.Prop_JoinKind})"
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
                    ErrorMessage = $"Ошибка соединения: {ex.Message}"
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
            ret.ValidateRequired(this.Prop_LeftTable, ActivityStrings.Field_JoinLeftTable, ActivityStrings.Error_JoinTableRequired);
            ret.ValidateRequired(this.Prop_RightTable, ActivityStrings.Field_JoinRightTable, ActivityStrings.Error_JoinTableRequired);
            ret.ValidateRequired(this.Prop_LeftKeys, ActivityStrings.Field_JoinLeftKeys, ActivityStrings.Error_JoinKeysRequired);
            ret.ValidateRequired(this.Prop_RightKeys, ActivityStrings.Field_JoinRightKeys, ActivityStrings.Error_JoinKeysRequired);
            return ret;
        }
    }
}
