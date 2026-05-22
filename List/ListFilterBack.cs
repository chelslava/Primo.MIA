// =============================================================================
// ListFilter.cs — активность «Список: Фильтрация».
//
// Фильтрует List<string> по условию — возвращает два списка:
//   Prop_Matched  — элементы прошедшие условие
//   Prop_Rejected — элементы НЕ прошедшие условие (остаток)
//
// Режимы (ListFilterMode):
//   Contains       — значение содержит подстроку
//   NotContains    — значение НЕ содержит подстроку
//   StartsWith     — начинается с подстроки
//   EndsWith       — заканчивается на подстроку
//   ExactMatch     — точное совпадение
//   Regex          — совпадение с регулярным выражением
//   NotEmpty       — непустые строки (длина > 0, не null, не whitespace)
//   LengthRange    — длина строки в диапазоне [Min, Max]
//   NumericOnly    — строки являющиеся числами (int или float)
//
// Оригинальный список не изменяется.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Список: Фильтрация».
    /// Разделяет List&lt;string&gt; на две части:
    /// прошедшие условие (Matched) и не прошедшие (Rejected).
    /// Поддерживает регистрозависимый и регистронезависимый режим.
    /// </summary>
    public class ListFilterBack : PrimoComponentTO<ListFilter>
    {
        public override string GroupName { get => ActivityCategories.Lists; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // ── INPUT ──────────────────────────────────────────────────────────────

        private string _propList;
        /// <summary>Входной список List&lt;string&gt;</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_List)]
        public string Prop_List
        {
            get => _propList;
            set { _propList = value; InvokePropertyChanged(this, "Prop_List"); }
        }

        private ListFilterMode _mode = ListFilterMode.Contains;
        /// <summary>Условие фильтрации</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Condition)]
        public ListFilterMode Mode
        {
            get => _mode;
            set { _mode = value; InvokePropertyChanged(this, "Mode"); }
        }

        private string _propPattern;
        /// <summary>
        /// Строка или регулярное выражение для фильтрации.
        /// Не используется в режимах: NotEmpty, EmptyOnly, NumericOnly.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_PatternSubstring)]
        public string Prop_Pattern
        {
            get => _propPattern;
            set { _propPattern = value; InvokePropertyChanged(this, "Prop_Pattern"); }
        }

        private bool _caseSensitive = false;
        /// <summary>
        /// Учитывать регистр при сравнении.
        /// По умолчанию false — "Hello" содержит "hello".
        /// Не влияет на режимы NumericOnly, NotEmpty, EmptyOnly, LengthRange.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_CaseSensitive)]
        public bool Prop_CaseSensitive
        {
            get => _caseSensitive;
            set { _caseSensitive = value; InvokePropertyChanged(this, "Prop_CaseSensitive"); }
        }

        private string _propMinLength;
        /// <summary>Минимальная длина строки (включительно) для режима LengthRange</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_LengthRange), System.ComponentModel.DisplayName(ActivityStrings.Field_MinLength)]
        public string Prop_MinLength
        {
            get => _propMinLength;
            set { _propMinLength = value; InvokePropertyChanged(this, "Prop_MinLength"); }
        }

        private string _propMaxLength;
        /// <summary>Максимальная длина строки (включительно) для режима LengthRange</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_LengthRange), System.ComponentModel.DisplayName(ActivityStrings.Field_MaxLength)]
        public string Prop_MaxLength
        {
            get => _propMaxLength;
            set { _propMaxLength = value; InvokePropertyChanged(this, "Prop_MaxLength"); }
        }

        // ── OUTPUT ─────────────────────────────────────────────────────────────

        private string _propMatched;
        /// <summary>Элементы прошедшие условие фильтра</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Matched)]
        public string Prop_Matched
        {
            get => _propMatched;
            set { _propMatched = value; InvokePropertyChanged(this, "Prop_Matched"); }
        }

        private string _propRejected;
        /// <summary>Элементы НЕ прошедшие условие фильтра (остаток)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Rejected)]
        public string Prop_Rejected
        {
            get => _propRejected;
            set { _propRejected = value; InvokePropertyChanged(this, "Prop_Rejected"); }
        }

        private string _propMatchedCount;
        /// <summary>Количество элементов прошедших фильтр</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MatchedCount)]
        public string Prop_MatchedCount
        {
            get => _propMatchedCount;
            set { _propMatchedCount = value; InvokePropertyChanged(this, "Prop_MatchedCount"); }
        }

        private string _propRejectedCount;
        /// <summary>Количество элементов НЕ прошедших фильтр</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_RejectedCount)]
        public string Prop_RejectedCount
        {
            get => _propRejectedCount;
            set { _propRejectedCount = value; InvokePropertyChanged(this, "Prop_RejectedCount"); }
        }

        // ── КОНСТРУКТОР ────────────────────────────────────────────────────────

        public ListFilterBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Список: Фильтрация";
            sdkComponentHelp =
                "Фильтрует List<string> по условию.\n" +
                "Возвращает ДВА списка: прошедшие и не прошедшие фильтр.\n\n" +
                "── Режимы ─────────────────────────────────────────────\n" +
                "Contains    — строка содержит подстроку\n" +
                "NotContains — строка НЕ содержит подстроку\n" +
                "StartsWith  — начинается с подстроки\n" +
                "EndsWith    — заканчивается подстрокой\n" +
                "ExactMatch  — точное совпадение\n" +
                "Regex       — соответствует регулярному выражению\n" +
                "NotRegex    — НЕ соответствует регулярному выражению\n" +
                "NotEmpty    — непустые строки\n" +
                "EmptyOnly   — только пустые строки\n" +
                "LengthRange — длина в диапазоне [Мин, Макс]\n" +
                "NumericOnly — строки являющиеся числами";
            sdkComponentIcon = ActivityIcons.List;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<List<string>>("Prop_List", "Входной список"),
                PropertyBuilder.Enum<ListFilterMode>("Mode", "Условие фильтрации"),
                PropertyBuilder.Script<string>("Prop_Pattern", "Подстрока или регулярное выражение"),
                PropertyBuilder.BooleanObject("Prop_CaseSensitive", "Учитывать регистр при сравнении"),
                PropertyBuilder.Script<int>("Prop_MinLength", "Мин. длина для LengthRange"),
                PropertyBuilder.Script<int>("Prop_MaxLength", "Макс. длина для LengthRange"),
                PropertyBuilder.Variable<List<string>>("Prop_Matched", "Элементы прошедшие фильтр"),
                PropertyBuilder.Variable<List<string>>("Prop_Rejected", "Элементы НЕ прошедшие фильтр"),
                PropertyBuilder.Variable<int>("Prop_MatchedCount", "Кол-во прошедших"),
                PropertyBuilder.Variable<int>("Prop_RejectedCount", "Кол-во отсеянных")
            };

            InitClass(container);
        }

        // ── ДЕЙСТВИЕ ───────────────────────────────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var logic = new ListFilterLogic();
                var list = GetPropertyValue<List<string>>(this.Prop_List, "Prop_List", sd);
                string pat = GetPropertyValue<string>(this.Prop_Pattern, "Prop_Pattern", sd);

                if (list == null) throw new ArgumentNullException("Prop_List", "Список не может быть null");
                int minLength = 0;
                int maxLength = int.MaxValue;

                if (this.Mode == ListFilterMode.LengthRange)
                {
                    string minStr = GetPropertyValue<string>(this.Prop_MinLength, "Prop_MinLength", sd) ?? "0";
                    string maxStr = GetPropertyValue<string>(this.Prop_MaxLength, "Prop_MaxLength", sd) ?? int.MaxValue.ToString();
                    minLength = int.TryParse(minStr, out int minValue) ? minValue : 0;
                    maxLength = int.TryParse(maxStr, out int maxValue) ? maxValue : int.MaxValue;
                }

                var result = logic.Filter(list, pat ?? string.Empty, this.Mode, this.Prop_CaseSensitive, minLength, maxLength);

                SetVariableValue(this.Prop_Matched, result.Matched, sd);
                SetVariableValue(this.Prop_Rejected, result.Rejected, sd);
                SetVariableValue(this.Prop_MatchedCount, result.Matched.Count, sd);
                SetVariableValue(this.Prop_RejectedCount, result.Rejected.Count, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Прошло: {result.Matched.Count}, отсеяно: {result.Rejected.Count}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка фильтрации: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_List, ActivityStrings.Field_List, ActivityStrings.Error_ListRequired);

            // Паттерн обязателен для текстовых и regex режимов
            var modesWithPattern = new[]
            {
                ListFilterMode.Contains, ListFilterMode.NotContains,
                ListFilterMode.StartsWith, ListFilterMode.EndsWith,
                ListFilterMode.ExactMatch, ListFilterMode.Regex, ListFilterMode.NotRegex
            };

            if (modesWithPattern.Contains(this.Mode) && string.IsNullOrWhiteSpace(this.Prop_Pattern))
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = ActivityStrings.Field_PatternSubstring, Error = ActivityStrings.Error_PatternRequired });

            return ret;
        }
    }
}
