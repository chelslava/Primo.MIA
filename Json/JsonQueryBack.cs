// =============================================================================
// JsonQueryBack.cs — активность «JSON: Запрос (JSONPath)».
//
// Извлекает данные из JSON по JSONPath выражениям:
// - Поддержка полного синтаксиса JSONPath
// - Возврат одного значения или списка
// - Информация о количестве найденных элементов
//
// Примеры JSONPath:
//   $.store.book[*].author          — все авторы книг
//   $.store.book[?(@.price < 10)]   — книги дешевле 10
//   $..author                       — все авторы на любом уровне
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
    /// Активность для извлечения данных из JSON по JSONPath.
    /// </summary>
    public class JsonQueryBack : PrimoComponentTO<JsonQuery>
    {
        // ── Свойства SDK ──────────────────────────────────────────────────

        /// <summary>
        /// Категория активности в дизайнере Primo.
        /// </summary>
        public override string GroupName
        {
            get => ActivityCategories.HttpWeb;
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
        /// JSON-строка для запроса.
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

        #region Prop_JsonPath

        private string _propJsonPath;

        /// <summary>
        /// JSONPath выражение для поиска.
        /// Примеры: $.store.book[*].author, $..price
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("JSONPath")]
        public string Prop_JsonPath
        {
            get => _propJsonPath;
            set { _propJsonPath = value; InvokePropertyChanged(this, nameof(Prop_JsonPath)); }
        }

        #endregion

        #region Prop_ReturnFirst

        private bool _propReturnFirst;

        /// <summary>
        /// Вернуть только первое совпадение.
        /// Если false — возвращается список всех совпадений.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Только первое")]
        public bool Prop_ReturnFirst
        {
            get => _propReturnFirst;
            set { _propReturnFirst = value; InvokePropertyChanged(this, nameof(Prop_ReturnFirst)); }
        }

        #endregion

        // ── Выходные параметры ────────────────────────────────────────────

        #region Prop_Result

        private string _propResult;

        /// <summary>
        /// Результат запроса (значение или список).
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

        #region Prop_Count

        private string _propCount;

        /// <summary>
        /// Количество найденных элементов.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Количество")]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, nameof(Prop_Count)); }
        }

        #endregion

        #region Prop_Found

        private string _propFound;

        /// <summary>
        /// Признак наличия результатов.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Найдено")]
        public string Prop_Found
        {
            get => _propFound;
            set { _propFound = value; InvokePropertyChanged(this, nameof(Prop_Found)); }
        }

        #endregion

        // ── Конструктор ───────────────────────────────────────────────────

        /// <summary>
        /// Конструктор активности «JSON: Запрос».
        /// </summary>
        public JsonQueryBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "JSON: Запрос";
            sdkComponentHelp =
                "Извлекает данные из JSON по JSONPath выражениям.\n" +
                "\n" +
                "── Примеры JSONPath ───────────────────────────\n" +
                "$.store.book[*].author     — все авторы книг\n" +
                "$.store.book[0].title      — название первой книги\n" +
                "$.store.book[?(@.price<10)]— книги дешевле 10\n" +
                "$..author                  — все авторы на любом уровне\n" +
                "$.store.*                  — все в разделе store\n" +
                "\n" +
                "── Входные параметры ──────────────────────────\n" +
                "JSON-строка   — исходный JSON\n" +
                "JSONPath      — выражение для поиска\n" +
                "Только первое — вернуть только первое совпадение\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Результат     — найденное значение или список\n" +
                "Количество    — число найденных элементов\n" +
                "Найдено       — признак наличия результатов";

            sdkComponentIcon = ActivityIcons.Http;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<string>("Prop_Json", "JSON-строка для запроса"),
                PropertyBuilder.Script<string>("Prop_JsonPath", "JSONPath выражение"),
                PropertyBuilder.BooleanObject("Prop_ReturnFirst", "Вернуть только первое совпадение"),
                PropertyBuilder.Variable<object>("Prop_Result", "Результат запроса"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество найденных"),
                PropertyBuilder.Variable<bool>("Prop_Found", "Признак наличия результатов")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_Json = "\"{\\\"store\\\": {\\\"book\\\": []}}\"";
            this.Prop_JsonPath = "\"$..*\"";
            this.Prop_ReturnFirst = false;
        }

        // ── TimedAction — точка входа ──────────────────────────────────────

        /// <summary>
        /// Основной метод выполнения JSONPath запроса.
        /// </summary>
        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // ── Чтение входных параметров ──────────────────────────────
                string json = GetPropertyValue<string>(this.Prop_Json, nameof(Prop_Json), sd);
                Guard.NotNullOrWhiteSpace(json, nameof(Prop_Json));

                string jsonPath = GetPropertyValue<string>(this.Prop_JsonPath, nameof(Prop_JsonPath), sd);
                Guard.NotNullOrWhiteSpace(jsonPath, nameof(Prop_JsonPath));

                // ── Валидация JSON ────────────────────────────────────────
                if (!HttpHelper.IsValidJson(json))
                    throw new ArgumentException("Невалидный JSON формат");

                // ── Выполнение JSONPath запроса ───────────────────────────
                var jToken = JToken.Parse(json);
                var results = jToken.SelectTokens(jsonPath).ToList();

                int count = results.Count;
                bool found = count > 0;

                // ── Формирование результата ──────────────────────────────
                object result = null;

                if (found)
                {
                    if (this.Prop_ReturnFirst)
                    {
                        // Возвращаем только первое значение
                        result = JTokenToObject(results[0]);
                    }
                    else
                    {
                        // Возвращаем список всех значений
                        result = results.Select(JTokenToObject).ToList();
                    }
                }

                // ── Запись выходных параметров ────────────────────────────
                if (!string.IsNullOrWhiteSpace(this.Prop_Result) && result != null)
                    SetVariableValue(this.Prop_Result, result, sd);

                if (!string.IsNullOrWhiteSpace(this.Prop_Count))
                    SetVariableValue(this.Prop_Count, count, sd);

                if (!string.IsNullOrWhiteSpace(this.Prop_Found))
                    SetVariableValue(this.Prop_Found, found, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[JSON: Запрос] Найдено {count} элементов по пути {jsonPath}"
                };
            }
            catch (JsonException jex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка JSON [JSON: Запрос]: {jex.Message}"
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
                    ErrorMessage = $"Ошибка [JSON: Запрос]: {ex.Message}"
                };
            }
        }

        // ── Приватные методы ──────────────────────────────────────────────

        /// <summary>
        /// Преобразует JToken в соответствующий .NET тип.
        /// </summary>
        private object JTokenToObject(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    return JObjectToDictionary((JObject)token);

                case JTokenType.Array:
                    return JArrayToList((JArray)token);

                case JTokenType.String:
                    return token.ToString();

                case JTokenType.Integer:
                    return token.Value<long>();

                case JTokenType.Float:
                    return token.Value<double>();

                case JTokenType.Boolean:
                    return token.Value<bool>();

                case JTokenType.Null:
                    return null;

                case JTokenType.Date:
                    return token.Value<DateTime>();

                case JTokenType.Guid:
                    return token.Value<Guid>();

                case JTokenType.Uri:
                    return token.Value<Uri>();

                case JTokenType.TimeSpan:
                    return token.Value<TimeSpan>();

                default:
                    return token.ToString();
            }
        }

        /// <summary>
        /// Рекурсивно преобразует JObject в Dictionary.
        /// </summary>
        private Dictionary<string, object> JObjectToDictionary(JObject jObject)
        {
            var result = new Dictionary<string, object>();

            foreach (var property in jObject.Properties())
            {
                result[property.Name] = JTokenToObject(property.Value);
            }

            return result;
        }

        /// <summary>
        /// Рекурсивно преобразует JArray в List.
        /// </summary>
        private List<object> JArrayToList(JArray jArray)
        {
            var result = new List<object>();

            foreach (var item in jArray)
            {
                result.Add(JTokenToObject(item));
            }

            return result;
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

            if (string.IsNullOrWhiteSpace(this.Prop_JsonPath))
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = "Prop_JsonPath",
                    Error = "JSONPath не может быть пустым"
                });
            }

            return ret;
        }
    }
}
