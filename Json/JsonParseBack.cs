// =============================================================================
// JsonParseBack.cs — активность «JSON: Парсинг».
//
// Парсит JSON-строки в структурированные данные:
// - Преобразование в Dictionary/List
// - Валидация JSON
// - Форматирование (pretty print)
// - Минификация
//
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для парсинга и обработки JSON-данных.
    /// </summary>
    public class JsonParseBack : PrimoComponentTO<JsonParse>
    {
        private readonly JsonParseLogic _logic = new JsonParseLogic();

        // ── Свойства SDK ──────────────────────────────────────────────────

        /// <summary>
        /// Категория активности в дизайнере Primo.
        /// </summary>
        public override string GroupName
        {
            get => ActivityCategories.HttpWeb; // Можно создать отдельную категорию JSON
            protected set { }
        }

        /// <summary>
        /// Максимальное время выполнения активности (мс).
        /// </summary>
        protected override int sdkTimeOut
        {
            get => 30000;
            set { }
        }

        // ── Входные параметры ─────────────────────────────────────────────

        #region Prop_Json

        private string _propJson;

        /// <summary>
        /// JSON-строка для обработки.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("JSON-строка")]
        public string Prop_Json
        {
            get => _propJson;
            set { _propJson = value; InvokePropertyChanged(this, nameof(Prop_Json)); }
        }

        #endregion

        #region Prop_Mode

        private JsonParseMode _propMode;

        /// <summary>
        /// Режим парсинга JSON.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Режим")]
        public JsonParseMode Prop_Mode
        {
            get => _propMode;
            set { _propMode = value; InvokePropertyChanged(this, nameof(Prop_Mode)); }
        }

        #endregion

        #region Prop_StrictMode

        private bool _propStrictMode;

        /// <summary>
        /// Строгий режим валидации.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Строгий режим")]
        public bool Prop_StrictMode
        {
            get => _propStrictMode;
            set { _propStrictMode = value; InvokePropertyChanged(this, nameof(Prop_StrictMode)); }
        }

        #endregion

        // ── Выходные параметры ────────────────────────────────────────────

        #region Prop_Result

        private string _propResult;

        /// <summary>
        /// Результат парсинга (Dictionary или List).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Результат")]
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); }
        }

        #endregion

        #region Prop_IsValid

        private string _propIsValid;

        /// <summary>
        /// Признак валидного JSON.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Валиден")]
        public string Prop_IsValid
        {
            get => _propIsValid;
            set { _propIsValid = value; InvokePropertyChanged(this, nameof(Prop_IsValid)); }
        }

        #endregion

        #region Prop_Errors

        private string _propErrors;

        /// <summary>
        /// Список ошибок валидации.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Ошибки")]
        public string Prop_Errors
        {
            get => _propErrors;
            set { _propErrors = value; InvokePropertyChanged(this, nameof(Prop_Errors)); }
        }

        #endregion

        // ── Конструктор ───────────────────────────────────────────────────

        /// <summary>
        /// Конструктор активности «JSON: Парсинг».
        /// </summary>
        public JsonParseBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "JSON: Парсинг";
            sdkComponentHelp =
                "Парсит JSON-строки в структурированные данные.\n" +
                "\n" +
                "── Режимы работы ──────────────────────────────\n" +
                "ToDictionary — преобразовать в Dictionary\n" +
                "ToList       — преобразовать в List\n" +
                "Validate     — только валидация\n" +
                "Format       — форматирование (pretty print)\n" +
                "\n" +
                "── Входные параметры ──────────────────────────\n" +
                "JSON-строка  — исходный JSON\n" +
                "Режим        — режим обработки\n" +
                "Строгий режим — строгая валидация\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Результат    — Dictionary или List\n" +
                "Валиден      — признак валидного JSON\n" +
                "Ошибки       — список ошибок валидации";

            sdkComponentIcon = ActivityIcons.Http;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<string>("Prop_Json", "JSON-строка для обработки"),
                PropertyBuilder.Enum<JsonParseMode>("Prop_Mode", "Режим парсинга"),
                PropertyBuilder.BooleanObject("Prop_StrictMode", "Строгий режим валидации"),
                PropertyBuilder.Variable<object>("Prop_Result", "Результат парсинга"),
                PropertyBuilder.Variable<bool>("Prop_IsValid", "Признак валидного JSON"),
                PropertyBuilder.Variable<string>("Prop_Errors", "Список ошибок")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_Json = "\"{\\\"key\\\": \\\"value\\\"}\"";
            this.Prop_Mode = JsonParseMode.ToDictionary;
            this.Prop_StrictMode = false;
        }

        // ── TimedAction — точка входа ──────────────────────────────────────

        /// <summary>
        /// Основной метод выполнения парсинга JSON.
        /// </summary>
        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // ── Чтение входных параметров ──────────────────────────────
                string json = GetPropertyValue<string>(this.Prop_Json, nameof(Prop_Json), sd);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new ExecutionResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "JSON-строка не может быть пустой"
                    };
                }

                // ── Валидация JSON ────────────────────────────────────────
                bool isValid = HttpHelper.IsValidJson(json);
                string errors = string.Empty;

                if (!isValid)
                {
                    errors = "Невалидный JSON формат";
                    
                    if (!string.IsNullOrWhiteSpace(this.Prop_IsValid))
                        SetVariableValue(this.Prop_IsValid, false, sd);

                    if (!string.IsNullOrWhiteSpace(this.Prop_Errors))
                        SetVariableValue(this.Prop_Errors, errors, sd);

                    if (this.Prop_StrictMode)
                    {
                        return new ExecutionResult
                        {
                            IsSuccess = false,
                            ErrorMessage = $"Ошибка валидации JSON: {errors}"
                        };
                    }

                    return new ExecutionResult
                    {
                        IsSuccess = true,
                        SuccessMessage = $"[JSON: Парсинг] JSON невалиден"
                    };
                }

                // ── Выполнение в соответствии с режимом ───────────────────
                object result = null;
                string successMessage = string.Empty;

                switch (this.Prop_Mode)
                {
                    case JsonParseMode.ToDictionary:
                        result = _logic.ParseToDictionary(json);
                        successMessage = "преобразован в Dictionary";
                        break;

                    case JsonParseMode.ToList:
                        result = _logic.ParseToList(json);
                        successMessage = "преобразован в List";
                        break;

                    case JsonParseMode.Validate:
                        result = null;
                        successMessage = "валидация пройдена";
                        break;

                    case JsonParseMode.Format:
                        result = _logic.FormatJson(json);
                        successMessage = "отформатирован";
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(Prop_Mode),
                            $"Неизвестный режим: {this.Prop_Mode}");
                }

                // ── Запись выходных параметров ────────────────────────────
                if (!string.IsNullOrWhiteSpace(this.Prop_Result) && result != null)
                    SetVariableValue(this.Prop_Result, result, sd);

                if (!string.IsNullOrWhiteSpace(this.Prop_IsValid))
                    SetVariableValue(this.Prop_IsValid, true, sd);

                if (!string.IsNullOrWhiteSpace(this.Prop_Errors))
                    SetVariableValue(this.Prop_Errors, string.Empty, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[JSON: Парсинг] JSON {successMessage}"
                };
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка JSON: {ex.Message}"
                };
            }
            catch (ArgumentException ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Неверный аргумент: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка [JSON: Парсинг]: {ex.Message}"
                };
            }
        }

        // ── Валидация ─────────────────────────────────────────────────────

        /// <summary>
        /// Валидация параметров активности.
        /// </summary>
        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            if (string.IsNullOrWhiteSpace(this.Prop_Json))
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = "Prop_Json",
                    Error = "JSON-строка не может быть пустой"
                });
            }

            return ret;
        }
    }
}
