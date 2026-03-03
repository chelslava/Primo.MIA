// =============================================================================
// ListTransform.cs — активность «Список: Преобразование».
//
// Применяет трансформацию к каждому элементу List<string>.
//
// Режимы (ListTransformMode):
//   ToUpper         — в верхний регистр
//   ToLower         — в нижний регистр
//   Trim            — удалить пробелы по краям
//   TrimStart       — удалить пробелы слева
//   TrimEnd         — удалить пробелы справа
//   Replace         — замена подстроки на другую
//   RegexReplace    — замена по регулярному выражению
//   Prefix          — добавить префикс к каждому элементу
//   Suffix          — добавить суффикс к каждому элементу
//   Wrap            — обернуть в Prefix...Suffix (например кавычки)
//   PadLeft         — выровнять по правому краю (дополнить слева символом)
//   PadRight        — выровнять по левому краю (дополнить справа символом)
//   Truncate        — обрезать до MaxLength символов
//   RemoveNumbers   — удалить все цифры
//   RemoveNonAlpha  — оставить только буквы и цифры
//
// Возвращает новый список — оригинал не изменяется.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.Enums;
using LTools.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Список: Преобразование».
    /// Применяет трансформацию к каждому элементу List&lt;string&gt;.
    /// Возвращает новый список — оригинал не изменяется.
    /// </summary>
    public class ListTransformBack : PrimoComponentTO<ListTransform>
    {
        private const string CGroupName = "MIA" + WFPublishedElementBase.TREE_SEPARATOR + "Списки";
        public override string GroupName { get => CGroupName; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // ── INPUT ──────────────────────────────────────────────────────────────

        private string _propList;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Список")]
        public string Prop_List
        {
            get => _propList;
            set { _propList = value; InvokePropertyChanged(this, "Prop_List"); }
        }

        private ListTransformMode _mode = ListTransformMode.Trim;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Преобразование")]
        public ListTransformMode Mode
        {
            get => _mode;
            set { _mode = value; InvokePropertyChanged(this, "Mode"); }
        }

        private string _propFind;
        /// <summary>Строка или regex для поиска (режимы Replace, RegexReplace)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Replace"), System.ComponentModel.DisplayName("Найти")]
        public string Prop_Find
        {
            get => _propFind;
            set { _propFind = value; InvokePropertyChanged(this, "Prop_Find"); }
        }

        private string _propReplacement;
        /// <summary>Строка-замена (режимы Replace, RegexReplace). Может быть пустой — удаление.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Replace"), System.ComponentModel.DisplayName("Заменить на")]
        public string Prop_Replacement
        {
            get => _propReplacement;
            set { _propReplacement = value; InvokePropertyChanged(this, "Prop_Replacement"); }
        }

        private string _propPrefix;
        /// <summary>Префикс для режимов Prefix и Wrap</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Prefix/Suffix"), System.ComponentModel.DisplayName("Префикс")]
        public string Prop_Prefix
        {
            get => _propPrefix;
            set { _propPrefix = value; InvokePropertyChanged(this, "Prop_Prefix"); }
        }

        private string _propSuffix;
        /// <summary>Суффикс для режимов Suffix и Wrap</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Prefix/Suffix"), System.ComponentModel.DisplayName("Суффикс")]
        public string Prop_Suffix
        {
            get => _propSuffix;
            set { _propSuffix = value; InvokePropertyChanged(this, "Prop_Suffix"); }
        }

        private string _propPadWidth;
        /// <summary>Целевая ширина строки для PadLeft/PadRight</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Pad/Truncate"), System.ComponentModel.DisplayName("Ширина (Pad)")]
        public string Prop_PadWidth
        {
            get => _propPadWidth;
            set { _propPadWidth = value; InvokePropertyChanged(this, "Prop_PadWidth"); }
        }

        private string _propPadChar;
        /// <summary>Символ заполнения для PadLeft/PadRight. По умолчанию пробел.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Pad/Truncate"), System.ComponentModel.DisplayName("Символ заполнения")]
        public string Prop_PadChar
        {
            get => _propPadChar;
            set { _propPadChar = value; InvokePropertyChanged(this, "Prop_PadChar"); }
        }

        private string _propMaxLength;
        /// <summary>Максимальная длина строки для режима Truncate</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Pad/Truncate"), System.ComponentModel.DisplayName("Макс. длина (Truncate)")]
        public string Prop_MaxLength
        {
            get => _propMaxLength;
            set { _propMaxLength = value; InvokePropertyChanged(this, "Prop_MaxLength"); }
        }

        private bool _caseSensitive = false;
        /// <summary>Учитывать регистр при поиске (режимы Replace, RegexReplace)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category("Replace"), System.ComponentModel.DisplayName("Учитывать регистр")]
        public bool Prop_CaseSensitive
        {
            get => _caseSensitive;
            set { _caseSensitive = value; InvokePropertyChanged(this, "Prop_CaseSensitive"); }
        }

        // ── OUTPUT ─────────────────────────────────────────────────────────────

        private string _propResult;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Результат")]
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, "Prop_Result"); }
        }

        private string _propCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Количество")]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, "Prop_Count"); }
        }

        private string _propChangedCount;
        /// <summary>Количество элементов которые реально изменились после трансформации</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Изменилось элементов")]
        public string Prop_ChangedCount
        {
            get => _propChangedCount;
            set { _propChangedCount = value; InvokePropertyChanged(this, "Prop_ChangedCount"); }
        }

        // ── КОНСТРУКТОР ────────────────────────────────────────────────────────

        public ListTransformBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Список: Преобразование";
            sdkComponentHelp =
                "Применяет трансформацию к каждому элементу List<string>.\n" +
                "Возвращает новый список — оригинал не изменяется.\n\n" +
                "── Режимы ─────────────────────────────────────────────\n" +
                "ToUpper / ToLower    — смена регистра\n" +
                "Trim / TrimStart / TrimEnd — удаление пробелов\n" +
                "Replace              — замена подстроки (Find → Replacement)\n" +
                "RegexReplace         — замена по regex (Find → Replacement)\n" +
                "Prefix / Suffix      — добавить текст до/после каждого элемента\n" +
                "Wrap                 — Prefix + элемент + Suffix\n" +
                "PadLeft / PadRight   — дополнить символом до указанной ширины\n" +
                "Truncate             — обрезать до MaxLength символов\n" +
                "RemoveNumbers        — удалить все цифры\n" +
                "RemoveNonAlpha       — оставить только буквы и цифры";
            sdkComponentIcon = "pack://application:,,,/Primo.MIA;component/images/list.png";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_List", PropertyType = PropertyTypes.SCRIPT, EditorType = ScriptEditorTypes.NONE, DataType = typeof(List<string>), ToolTip = "Входной список", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Mode", PropertyType = PropertyTypes.OBJECT, EditorType = ScriptEditorTypes.NONE, DataType = typeof(ListTransformMode), ToolTip = "Тип преобразования", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Find", PropertyType = PropertyTypes.SCRIPT, EditorType = ScriptEditorTypes.NONE, DataType = typeof(string), ToolTip = "Что заменить (Replace/RegexReplace)", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Replacement", PropertyType = PropertyTypes.SCRIPT, EditorType = ScriptEditorTypes.NONE, DataType = typeof(string), ToolTip = "На что заменить (пусто = удалить)", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Prefix", PropertyType = PropertyTypes.SCRIPT, EditorType = ScriptEditorTypes.NONE, DataType = typeof(string), ToolTip = "Префикс (Prefix/Wrap)", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Suffix", PropertyType = PropertyTypes.SCRIPT, EditorType = ScriptEditorTypes.NONE, DataType = typeof(string), ToolTip = "Суффикс (Suffix/Wrap)", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_PadWidth", PropertyType = PropertyTypes.SCRIPT, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int), ToolTip = "Целевая ширина (PadLeft/PadRight)", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_PadChar", PropertyType = PropertyTypes.SCRIPT, EditorType = ScriptEditorTypes.NONE, DataType = typeof(string), ToolTip = "Символ заполнения (по умолч. пробел)", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_MaxLength", PropertyType = PropertyTypes.SCRIPT, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int), ToolTip = "Макс. длина (Truncate)", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Result", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(List<string>), ToolTip = "Преобразованный список", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Count", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int), ToolTip = "Количество элементов", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_ChangedCount", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int), ToolTip = "Кол-во изменённых элементов", IsReadOnly = false }
            };

            InitClass(container);
            this.Prop_PadChar = "\" \"";
        }

        // ── ДЕЙСТВИЕ ───────────────────────────────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var list = GetPropertyValue<List<string>>(this.Prop_List, "Prop_List", sd);
                if (list == null) throw new ArgumentNullException("Prop_List", "Список не может быть null");

                string find        = GetPropertyValue<string>(this.Prop_Find,        "Prop_Find",        sd) ?? string.Empty;
                string replacement = GetPropertyValue<string>(this.Prop_Replacement, "Prop_Replacement", sd) ?? string.Empty;
                string prefix      = GetPropertyValue<string>(this.Prop_Prefix,      "Prop_Prefix",      sd) ?? string.Empty;
                string suffix      = GetPropertyValue<string>(this.Prop_Suffix,      "Prop_Suffix",      sd) ?? string.Empty;
                string padCharStr  = GetPropertyValue<string>(this.Prop_PadChar,     "Prop_PadChar",     sd) ?? " ";
                char   padChar     = padCharStr.Length > 0 ? padCharStr[0] : ' ';

                int padWidth  = int.TryParse(this.Prop_PadWidth,  out int pw) ? pw : 0;
                int maxLength = int.TryParse(this.Prop_MaxLength, out int ml) ? ml : int.MaxValue;

                // Получаем функцию трансформации для выбранного режима
                Func<string, string> transform = BuildTransform(find, replacement, prefix, suffix, padChar, padWidth, maxLength);

                // Применяем трансформацию через LINQ Select и считаем изменения
                var result = list.Select(item =>
                {
                    string original  = item ?? string.Empty;
                    string converted = transform(original);
                    return converted;
                }).ToList();

                // Считаем сколько реально изменилось
                int changed = list
                    .Zip(result, (original, transformed) => original != transformed)
                    .Count(diff => diff);

                SetVariableValue(this.Prop_Result,       result,        sd);
                SetVariableValue(this.Prop_Count,        result.Count,  sd);
                SetVariableValue(this.Prop_ChangedCount, changed,       sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Преобразовано: {result.Count} элементов, изменилось: {changed}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка преобразования: {ex.Message}" };
            }
        }

        /// <summary>
        /// Строит функцию трансформации string → string для выбранного режима.
        /// Все параметры захватываются в замыкание — вычисляются один раз перед LINQ Select.
        /// </summary>
        private Func<string, string> BuildTransform(
            string find, string replacement, string prefix, string suffix,
            char padChar, int padWidth, int maxLength)
        {
            switch (this.Mode)
            {
                case ListTransformMode.ToUpper:
                    return s => (s ?? string.Empty).ToUpper();

                case ListTransformMode.ToLower:
                    return s => (s ?? string.Empty).ToLower();

                case ListTransformMode.Trim:
                    return s => (s ?? string.Empty).Trim();

                case ListTransformMode.TrimStart:
                    return s => (s ?? string.Empty).TrimStart();

                case ListTransformMode.TrimEnd:
                    return s => (s ?? string.Empty).TrimEnd();

                case ListTransformMode.Replace:
                {
                    StringComparison sc = this.Prop_CaseSensitive
                        ? StringComparison.Ordinal
                        : StringComparison.OrdinalIgnoreCase;

                    // Replace без учёта регистра через regex (string.Replace не поддерживает StringComparison)
                    if (!this.Prop_CaseSensitive)
                    {
                        var rx = new Regex(Regex.Escape(find), RegexOptions.IgnoreCase | RegexOptions.Compiled);
                        return s => s == null ? string.Empty : rx.Replace(s, replacement);
                    }
                    return s => s == null ? string.Empty : s.Replace(find, replacement);
                }

                case ListTransformMode.RegexReplace:
                {
                    var opts = this.Prop_CaseSensitive
                        ? RegexOptions.Compiled
                        : RegexOptions.Compiled | RegexOptions.IgnoreCase;
                    var rx = new Regex(find, opts);
                    return s => s == null ? string.Empty : rx.Replace(s, replacement);
                }

                case ListTransformMode.Prefix:
                    return s => prefix + (s ?? string.Empty);

                case ListTransformMode.Suffix:
                    return s => (s ?? string.Empty) + suffix;

                case ListTransformMode.Wrap:
                    return s => prefix + (s ?? string.Empty) + suffix;

                case ListTransformMode.PadLeft:
                    return s => (s ?? string.Empty).PadLeft(padWidth, padChar);

                case ListTransformMode.PadRight:
                    return s => (s ?? string.Empty).PadRight(padWidth, padChar);

                case ListTransformMode.Truncate:
                    return s =>
                    {
                        string str = s ?? string.Empty;
                        return str.Length <= maxLength ? str : str.Substring(0, maxLength);
                    };

                case ListTransformMode.RemoveNumbers:
                    return s => s == null ? string.Empty : Regex.Replace(s, @"\d", string.Empty);

                case ListTransformMode.RemoveNonAlpha:
                    return s => s == null ? string.Empty : Regex.Replace(s, @"[^a-zA-Zа-яА-ЯёЁ0-9 ]", string.Empty);

                default:
                    throw new InvalidOperationException($"Неизвестный режим: {this.Mode}");
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            if (string.IsNullOrWhiteSpace(this.Prop_List))
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "Список", Error = "Список обязателен" });

            if ((this.Mode == ListTransformMode.Replace || this.Mode == ListTransformMode.RegexReplace)
                && string.IsNullOrEmpty(this.Prop_Find))
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "Найти", Error = "Строка поиска обязательна для Replace/RegexReplace" });

            return ret;
        }
    }
}
