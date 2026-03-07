using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Анализирует кортеж: определяет арность, типы элементов, их строковые значения.
    /// Опционально сравнивает два кортежа на структурное равенство.
    ///
    /// Все выходы вычисляются за один проход. Входной кортеж не изменяется.
    /// </summary>
    public class TupleInspectBack : PrimoComponentTO<TupleInspect>
    {
        private const string CGroupName = "MIA" + WFPublishedElementBase.TREE_SEPARATOR + "Кортежи";
        public override string GroupName { get => CGroupName; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // ── INPUT ──────────────────────────────────────────────────────────────

        private string _propTuple;
        /// <summary>Анализируемый кортеж.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Кортеж")]
        public string Prop_Tuple { get => _propTuple; set { _propTuple = value; InvokePropertyChanged(this, "Prop_Tuple"); } }

        private string _propTupleB;
        /// <summary>
        /// Второй кортеж для сравнения.
        /// Если задан — заполняются выходы «Равны» и «Тип совпадает».
        /// Если не задан — выходы сравнения равны false.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Сравнение"), System.ComponentModel.DisplayName("Второй кортеж (для сравнения)")]
        public string Prop_TupleB { get => _propTupleB; set { _propTupleB = value; InvokePropertyChanged(this, "Prop_TupleB"); } }

        // ── OUTPUT: основные ──────────────────────────────────────────────────

        private string _propArity;
        /// <summary>Количество элементов кортежа (1–7).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Структура"), System.ComponentModel.DisplayName("Арность")]
        public string Prop_Arity { get => _propArity; set { _propArity = value; InvokePropertyChanged(this, "Prop_Arity"); } }

        private string _propTypeName;
        /// <summary>
        /// Полное имя типа кортежа.
        /// Пример: "Tuple`3" для Tuple&lt;string,int,bool&gt;.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Структура"), System.ComponentModel.DisplayName("Тип кортежа")]
        public string Prop_TypeName { get => _propTypeName; set { _propTypeName = value; InvokePropertyChanged(this, "Prop_TypeName"); } }

        private string _propItemTypes;
        /// <summary>
        /// Типы элементов через запятую.
        /// Пример: "String, Int32, Boolean".
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Структура"), System.ComponentModel.DisplayName("Типы элементов")]
        public string Prop_ItemTypes { get => _propItemTypes; set { _propItemTypes = value; InvokePropertyChanged(this, "Prop_ItemTypes"); } }

        private string _propItemValues;
        /// <summary>
        /// Строковые значения всех элементов через разделитель "; ".
        /// Пример: "Иванов; 42; true".
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Структура"), System.ComponentModel.DisplayName("Значения элементов")]
        public string Prop_ItemValues { get => _propItemValues; set { _propItemValues = value; InvokePropertyChanged(this, "Prop_ItemValues"); } }

        private string _propHasNulls;
        /// <summary>true если хотя бы один элемент кортежа равен null.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Структура"), System.ComponentModel.DisplayName("Есть null элементы")]
        public string Prop_HasNulls { get => _propHasNulls; set { _propHasNulls = value; InvokePropertyChanged(this, "Prop_HasNulls"); } }

        // ── OUTPUT: сравнение ─────────────────────────────────────────────────

        private string _propAreEqual;
        /// <summary>
        /// true если оба кортежа структурно равны: одинаковая арность и значения всех элементов равны.
        /// Сравнение значений — через object.Equals().
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Сравнение"), System.ComponentModel.DisplayName("Равны")]
        public string Prop_AreEqual { get => _propAreEqual; set { _propAreEqual = value; InvokePropertyChanged(this, "Prop_AreEqual"); } }

        private string _propSameArity;
        /// <summary>true если оба кортежа имеют одинаковую арность.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Сравнение"), System.ComponentModel.DisplayName("Одинаковая арность")]
        public string Prop_SameArity { get => _propSameArity; set { _propSameArity = value; InvokePropertyChanged(this, "Prop_SameArity"); } }

        private string _propSameTypes;
        /// <summary>true если оба кортежа имеют одинаковую арность И одинаковые типы всех элементов.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Сравнение"), System.ComponentModel.DisplayName("Совпадают типы")]
        public string Prop_SameTypes { get => _propSameTypes; set { _propSameTypes = value; InvokePropertyChanged(this, "Prop_SameTypes"); } }

        // ── КОНСТРУКТОР ────────────────────────────────────────────────────────

        public TupleInspectBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Кортеж: Анализ";
            sdkComponentHelp =
                "Анализирует структуру кортежа за один проход.\n\n" +
                "── Основные выходы ──────────────────────────────────────\n" +
                "Арность          — количество элементов (1–7)\n" +
                "Тип кортежа      — имя типа: \"Tuple`2\", \"Tuple`3\"...\n" +
                "Типы элементов   — \"String, Int32, Boolean\"\n" +
                "Значения         — \"Иванов; 42; true\"\n" +
                "Есть null        — true если любой элемент == null\n\n" +
                "── Сравнение (требует второй кортеж) ───────────────────\n" +
                "Равны            — одинаковая арность И все значения равны\n" +
                "Одинаковая арность — число элементов совпадает\n" +
                "Совпадают типы   — арность И типы всех элементов совпадают";

            sdkComponentIcon = "pack://application:,,,/Primo.MIA;component/images/sharp.png";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Tuple",      PropertyType = PropertyTypes.SCRIPT,   EditorType = ScriptEditorTypes.NONE, DataType = typeof(object), ToolTip = "Анализируемый кортеж", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_TupleB",     PropertyType = PropertyTypes.SCRIPT,   EditorType = ScriptEditorTypes.NONE, DataType = typeof(object), ToolTip = "Второй кортеж для сравнения (необязательно)", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Arity",      PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int),    ToolTip = "Количество элементов", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_TypeName",   PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(string), ToolTip = "Тип кортежа", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_ItemTypes",  PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(string), ToolTip = "Типы элементов через запятую", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_ItemValues", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(string), ToolTip = "Значения элементов через \"; \"", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_HasNulls",   PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(bool),   ToolTip = "Есть ли null-элементы", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_AreEqual",   PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(bool),   ToolTip = "Кортежи равны (все элементы совпадают)", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_SameArity",  PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(bool),   ToolTip = "Одинаковая арность", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_SameTypes",  PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(bool),   ToolTip = "Совпадают типы элементов", IsReadOnly = false }
            };

            InitClass(container);
        }

        // ── ДЕЙСТВИЕ ───────────────────────────────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                object tupleA = GetPropertyValue<object>(this.Prop_Tuple, "Prop_Tuple", sd);
                if (tupleA == null) throw new ArgumentNullException("Prop_Tuple", "Кортеж не может быть null");

                int arityA = TupleHelper.GetArity(tupleA);
                if (arityA < 0) throw new InvalidOperationException($"Не является System.Tuple: {tupleA.GetType().Name}");

                // Читаем все значения и типы
                object[] values = Enumerable.Range(1, arityA).Select(i => TupleHelper.GetItem(tupleA, i)).ToArray();
                Type[] types = tupleA.GetType().GetGenericArguments();

                string itemTypes = string.Join(", ", types.Select(t => t.Name));
                string itemValues = string.Join("; ", values.Select(v => v?.ToString() ?? "null"));
                bool hasNulls = values.Any(v => v == null);

                SetVariableValue(this.Prop_Arity, arityA, sd);
                SetVariableValue(this.Prop_TypeName, tupleA.GetType().Name, sd);
                SetVariableValue(this.Prop_ItemTypes, itemTypes, sd);
                SetVariableValue(this.Prop_ItemValues, itemValues, sd);
                SetVariableValue(this.Prop_HasNulls, hasNulls, sd);

                // ── Сравнение с вторым кортежем ───────────────────────────────
                bool areEqual = false;
                bool sameArity = false;
                bool sameTypes = false;

                if (!string.IsNullOrWhiteSpace(this.Prop_TupleB))
                {
                    object tupleB = GetPropertyValue<object>(this.Prop_TupleB, "Prop_TupleB", sd);
                    int arityB = tupleB != null ? TupleHelper.GetArity(tupleB) : -1;

                    sameArity = (arityA == arityB);

                    if (sameArity && tupleB != null)
                    {
                        Type[] typesB = tupleB.GetType().GetGenericArguments();
                        sameTypes = types.SequenceEqual(typesB);

                        // Структурное равенство: все элементы равны через Equals
                        areEqual = sameArity && Enumerable.Range(1, arityA).All(i =>
                        {
                            object vA = TupleHelper.GetItem(tupleA, i);
                            object vB = TupleHelper.GetItem(tupleB, i);
                            return Equals(vA, vB);
                        });
                    }
                }

                SetVariableValue(this.Prop_AreEqual, areEqual, sd);
                SetVariableValue(this.Prop_SameArity, sameArity, sd);
                SetVariableValue(this.Prop_SameTypes, sameTypes, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Tuple({arityA}): [{itemTypes}] = [{itemValues}]"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка анализа кортежа: {ex.Message}" };
            }
        }

        // ── ВАЛИДАЦИЯ ──────────────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            if (string.IsNullOrWhiteSpace(this.Prop_Tuple))
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "Кортеж", Error = "Кортеж обязателен" });
            return ret;
        }
    }
}