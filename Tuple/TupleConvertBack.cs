using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Конвертирует кортеж в другие структуры данных и обратно.
    ///
    /// Режимы:
    ///   ToList       — Tuple → List&lt;object&gt;
    ///   FromList     — List&lt;object&gt; → Tuple (первые 1–7 элементов)
    ///   ToDictionary — Tuple → Dictionary&lt;string,object&gt; с ключами "Item1".."ItemN"
    ///   ToString     — Tuple → строка вида "Item1=значение1; Item2=значение2"
    /// </summary>
    public class TupleConvertBack : PrimoComponentTO<TupleConvert>
    {
        public override string GroupName { get => ActivityCategories.Tuples; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // ── INPUT ──────────────────────────────────────────────────────────────

        private string _propTuple;
        /// <summary>Входной кортеж. Обязателен для ToList, ToDictionary, ToString.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Tuple)]
        public string Prop_Tuple { get => _propTuple; set { _propTuple = value; InvokePropertyChanged(this, "Prop_Tuple"); } }

        private TupleConvertMode _mode = TupleConvertMode.ToList;
        /// <summary>Режим конвертации.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_ConvertMode)]
        public TupleConvertMode Mode
        {
            get => _mode;
            set { _mode = value; InvokePropertyChanged(this, "Mode"); }
        }

        private string _propSourceList;
        /// <summary>Исходный список для режима FromList. Используются первые 1–7 элементов.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<object>))]
        [System.ComponentModel.Category(ActivityStrings.Category_FromList)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_SourceList)]
        public string Prop_SourceList { get => _propSourceList; set { _propSourceList = value; InvokePropertyChanged(this, "Prop_SourceList"); } }

        private string _propSeparator;
        /// <summary>Разделитель для режима ToString. По умолчанию "; ".</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_ToString)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Separator)]
        public string Prop_Separator { get => _propSeparator; set { _propSeparator = value; InvokePropertyChanged(this, "Prop_Separator"); } }

        private string _propIncludeKeys;
        /// <summary>
        /// Только для режима ToString: включать ли ключ "ItemN=" перед значением.
        /// true (по умолчанию) → "Item1=Иванов; Item2=42"
        /// false → "Иванов; 42"
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_ToString)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_IncludeKeys)]
        public string Prop_IncludeKeys { get => _propIncludeKeys; set { _propIncludeKeys = value; InvokePropertyChanged(this, "Prop_IncludeKeys"); } }

        // ── OUTPUT ─────────────────────────────────────────────────────────────

        private string _propResultTuple;
        /// <summary>Результирующий кортеж. Заполняется только для режима FromList.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_ResultTuple)]
        public string Prop_ResultTuple { get => _propResultTuple; set { _propResultTuple = value; InvokePropertyChanged(this, "Prop_ResultTuple"); } }

        private string _propResultList;
        /// <summary>Результирующий список. Заполняется только для режима ToList.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<object>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_ResultList)]
        public string Prop_ResultList { get => _propResultList; set { _propResultList = value; InvokePropertyChanged(this, "Prop_ResultList"); } }

        private string _propResultDict;
        /// <summary>Результирующий словарь. Заполняется только для режима ToDictionary.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, object>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_ResultDict)]
        public string Prop_ResultDict { get => _propResultDict; set { _propResultDict = value; InvokePropertyChanged(this, "Prop_ResultDict"); } }

        private string _propResultString;
        /// <summary>Результирующая строка. Заполняется только для режима ToString.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_ResultString)]
        public string Prop_ResultString { get => _propResultString; set { _propResultString = value; InvokePropertyChanged(this, "Prop_ResultString"); } }

        private string _propArity;
        /// <summary>Арность кортежа — количество элементов (1–7).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Arity)]
        public string Prop_Arity { get => _propArity; set { _propArity = value; InvokePropertyChanged(this, "Prop_Arity"); } }

        // ── КОНСТРУКТОР ────────────────────────────────────────────────────────

        public TupleConvertBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Кортеж: Конвертация";
            sdkComponentHelp =
                "Преобразует кортеж в другие структуры данных и обратно.\n\n" +
                "── Режимы ───────────────────────────────────────────────\n" +
                "ToList       — Tuple → List<object> (по одному элементу)\n" +
                "FromList     — List<object> → Tuple (первые 1–7 элементов)\n" +
                "ToDictionary — Tuple → Dictionary<string,object>\n" +
                "               ключи: \"Item1\", \"Item2\" ... \"ItemN\"\n" +
                "ToString     — Tuple → строка\n" +
                "               «Включать ключи»=true:  \"Item1=Иванов; Item2=42\"\n" +
                "               «Включать ключи»=false: \"Иванов; 42\"\n\n" +
                "Разделитель для ToString — по умолчанию \"; \".";
            sdkComponentIcon = ActivityIcons.Tuple;

            // Значения по умолчанию
            this.Prop_Separator = "\"; \"";
            this.Prop_IncludeKeys = "true";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<object>("Prop_Tuple", "Входной кортеж (для ToList, ToDictionary, ToString)"),
                PropertyBuilder.Enum<TupleConvertMode>("Mode", "Режим конвертации"),
                PropertyBuilder.Script<List<object>>("Prop_SourceList", "Список для конвертации в кортеж (только FromList)"),
                PropertyBuilder.Script<string>("Prop_Separator", "Разделитель (только ToString). По умолчанию \"; \""),
                PropertyBuilder.Script<bool>("Prop_IncludeKeys", "Включать Item1=, Item2=... в строку (только ToString)"),
                PropertyBuilder.Variable<object>("Prop_ResultTuple", "Результирующий кортеж (только FromList)"),
                PropertyBuilder.Variable<List<object>>("Prop_ResultList", "Результирующий список (только ToList)"),
                PropertyBuilder.Variable<Dictionary<string, object>>("Prop_ResultDict", "Результирующий словарь (только ToDictionary)"),
                PropertyBuilder.Variable<string>("Prop_ResultString", "Результирующая строка (только ToString)"),
                PropertyBuilder.Variable<int>("Prop_Arity", "Арность кортежа")
            };

            InitClass(container);
        }

        // ── ДЕЙСТВИЕ ───────────────────────────────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                switch (this.Mode)
                {
                    case TupleConvertMode.ToList: return DoToList(sd);
                    case TupleConvertMode.FromList: return DoFromList(sd);
                    case TupleConvertMode.ToDictionary: return DoToDictionary(sd);
                    case TupleConvertMode.ToString: return DoToString(sd);
                    default: throw new InvalidOperationException($"Неизвестный режим: {this.Mode}");
                }
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка конвертации кортежа: {ex.Message}" };
            }
        }

        // ── РЕЖИМ: ToList ──────────────────────────────────────────────────────

        private ExecutionResult DoToList(ScriptingData sd)
        {
            object tupleObj = GetPropertyValue<object>(this.Prop_Tuple, "Prop_Tuple", sd);
            if (tupleObj == null) throw new ArgumentNullException("Prop_Tuple", "Кортеж не может быть null");

            int arity = TupleHelper.GetArity(tupleObj);
            if (arity < 0) throw new InvalidOperationException($"Не является System.Tuple: {tupleObj.GetType().Name}");

            // Читаем все элементы и складываем в список
            var list = Enumerable.Range(1, arity)
                                 .Select(i => TupleHelper.GetItem(tupleObj, i))
                                 .ToList();

            SetVariableValue(this.Prop_ResultList, list, sd);
            SetVariableValue(this.Prop_Arity, arity, sd);

            return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Tuple({arity}) → List<object> с {arity} элементами" };
        }

        // ── РЕЖИМ: FromList ────────────────────────────────────────────────────

        private ExecutionResult DoFromList(ScriptingData sd)
        {
            var sourceList = GetPropertyValue<List<object>>(this.Prop_SourceList, "Prop_SourceList", sd);
            if (sourceList == null) throw new ArgumentNullException("Prop_SourceList", "Список не может быть null");
            if (sourceList.Count == 0) throw new InvalidOperationException("Список пуст — невозможно создать кортеж");
            if (sourceList.Count > 7) throw new InvalidOperationException($"Список содержит {sourceList.Count} элементов, максимум для Tuple — 7");

            int arity = sourceList.Count;
            object tuple = TupleHelper.CreateFromArray(sourceList.ToArray(), arity);

            SetVariableValue(this.Prop_ResultTuple, tuple, sd);
            SetVariableValue(this.Prop_Arity, arity, sd);

            return new ExecutionResult { IsSuccess = true, SuccessMessage = $"List<object>({arity}) → Tuple с {arity} элементами" };
        }

        // ── РЕЖИМ: ToDictionary ────────────────────────────────────────────────

        private ExecutionResult DoToDictionary(ScriptingData sd)
        {
            object tupleObj = GetPropertyValue<object>(this.Prop_Tuple, "Prop_Tuple", sd);
            if (tupleObj == null) throw new ArgumentNullException("Prop_Tuple", "Кортеж не может быть null");

            int arity = TupleHelper.GetArity(tupleObj);
            if (arity < 0) throw new InvalidOperationException($"Не является System.Tuple: {tupleObj.GetType().Name}");

            // Формируем словарь: "Item1" → значение, "Item2" → значение...
            var dict = Enumerable.Range(1, arity)
                                 .ToDictionary(i => "Item" + i,
                                               i => TupleHelper.GetItem(tupleObj, i));

            SetVariableValue(this.Prop_ResultDict, dict, sd);
            SetVariableValue(this.Prop_Arity, arity, sd);

            return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Tuple({arity}) → Dictionary с {arity} ключами" };
        }

        // ── РЕЖИМ: ToString ────────────────────────────────────────────────────

        private ExecutionResult DoToString(ScriptingData sd)
        {
            object tupleObj = GetPropertyValue<object>(this.Prop_Tuple, "Prop_Tuple", sd);
            if (tupleObj == null) throw new ArgumentNullException("Prop_Tuple", "Кортеж не может быть null");

            int arity = TupleHelper.GetArity(tupleObj);
            if (arity < 0) throw new InvalidOperationException($"Не является System.Tuple: {tupleObj.GetType().Name}");

            // Разрезолвим параметры
            string rawSep = GetPropertyValue<string>(this.Prop_Separator, "Prop_Separator", sd) ?? "; ";
            string rawInc = GetPropertyValue<string>(this.Prop_IncludeKeys, "Prop_IncludeKeys", sd) ?? "true";

            rawSep = rawSep.Trim().Trim('"');
            bool includeKeys = !rawInc.Equals("false", StringComparison.OrdinalIgnoreCase);

            // Строим строку из элементов
            var parts = Enumerable.Range(1, arity).Select(i =>
            {
                object val = TupleHelper.GetItem(tupleObj, i);
                string strVal = val?.ToString() ?? "null";
                return includeKeys ? $"Item{i}={strVal}" : strVal;
            });

            string result = string.Join(rawSep, parts);

            SetVariableValue(this.Prop_ResultString, result, sd);
            SetVariableValue(this.Prop_Arity, arity, sd);

            return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Tuple({arity}) → \"{result}\"" };
        }

        // ── ВАЛИДАЦИЯ ──────────────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            bool needsTuple = this.Mode != TupleConvertMode.FromList;
            bool needsList = this.Mode == TupleConvertMode.FromList;

            if (needsTuple && string.IsNullOrWhiteSpace(this.Prop_Tuple))
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "Кортеж", Error = "Кортеж обязателен для выбранного режима" });
            if (needsList && string.IsNullOrWhiteSpace(this.Prop_SourceList))
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "Список (вход)", Error = "Список обязателен для режима FromList" });

            return ret;
        }
    }
}