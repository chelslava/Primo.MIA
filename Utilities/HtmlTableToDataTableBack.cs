// =============================================================================
// HtmlTableToDataTableBack.cs — активность «HTML: Таблица → DataTable»
// (issue #36). Парсинг с поддержкой rowspan/colspan, вложенные таблицы
// игнорируются. HtmlAgilityPack — единственная новая зависимость.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;

namespace Primo.MIA
{
    /// <summary>Активность извлечения HTML-таблицы в DataTable.</summary>
    public class HtmlTableToDataTableBack : PrimoComponentTO<HtmlTableToDataTable>
    {
        public override string GroupName { get => ActivityCategories.Utilities; protected set { } }

        protected override int sdkTimeOut { get => 30000; set { } }

        // ── Входные параметры ──────────────────────────────────────────

        private string _propHtml;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_HtmlTableInput)]
        /// <summary>HTML-документ или фрагмент с таблицей.</summary>
        public string Prop_Html
        {
            get => _propHtml;
            set { _propHtml = value; InvokePropertyChanged(this, nameof(Prop_Html)); }
        }

        private string _propTableIndex;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_HtmlTableIndex)]
        /// <summary>Индекс таблицы в документе (0 = первая).</summary>
        public string Prop_TableIndex
        {
            get => _propTableIndex;
            set { _propTableIndex = value; InvokePropertyChanged(this, nameof(Prop_TableIndex)); }
        }

        private bool _propHeaderRow = true;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_HtmlTableHeader)]
        /// <summary>Первая строка содержит заголовки колонок.</summary>
        public bool Prop_HeaderRow
        {
            get => _propHeaderRow;
            set { _propHeaderRow = value; InvokePropertyChanged(this, nameof(Prop_HeaderRow)); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propResult;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_HtmlTableResult)]
        /// <summary>Результирующая DataTable (имя выходной переменной).</summary>
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); }
        }

        public HtmlTableToDataTableBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_HtmlTableToDataTable;
            sdkComponentHelp = @"Компонент ""HTML: Таблица → DataTable""
Извлекает HTML-таблицу в DataTable (HtmlAgilityPack).

Основные:
HTML*: [String] Документ или фрагмент с таблицей.

Настройки:
Индекс таблицы: [Int32] Если таблиц несколько (0 = первая, по умолчанию 0).
Первая строка — заголовки: [Boolean] Имена колонок из первой строки (по умолчанию да).

Поддерживаются rowspan/colspan; вложенные таблицы игнорируются;
пробельные последовательности схлопываются.

Выходные данные:
DataTable: [DataTable] Все колонки — string.";
            sdkComponentIcon = ActivityIcons.Table;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_Html", "HTML-документ или фрагмент"),
                PropertyBuilder.Int("Prop_TableIndex", "Индекс таблицы (0 = первая)"),
                PropertyBuilder.BooleanObject("Prop_HeaderRow", "Первая строка — заголовки"),
                PropertyBuilder.Variable<DataTable>("Prop_Result", "Результирующая DataTable")
            };

            InitClass(container);

            this.Prop_Html = "\"\"";
            this.Prop_TableIndex = "0";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new HtmlTableLogic();
            try
            {
                var html = GetPropertyValue<string>(this.Prop_Html, nameof(Prop_Html), sd);
                var tableIndex = StringHelper.TryParseInt(
                    GetPropertyValue<string>(this.Prop_TableIndex, nameof(Prop_TableIndex), sd)) ?? 0;

                var result = logic.ToDataTable(html, tableIndex, this.Prop_HeaderRow);

                SetVariableValue(this.Prop_Result, result, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[HTML: Таблица → DataTable] {result.Rows.Count} строк, {result.Columns.Count} колонок"
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
                    ErrorMessage = $"Ошибка разбора HTML: {ex.Message}"
                };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Html, ActivityStrings.Field_HtmlTableInput, ActivityStrings.Error_HtmlTableRequired);
            return ret;
        }
    }
}
