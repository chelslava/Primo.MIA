// =============================================================================
// JsonSchemaGenerateBack.cs — активность «JSON: Схема из примера» (issue #34).
//
// Быстрый старт контракта: валидный пример JSON → JSON Schema.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность генерации JSON Schema из примера.</summary>
    public class JsonSchemaGenerateBack : PrimoComponentTO<JsonSchemaGenerate>
    {
        public override string GroupName { get => ActivityCategories.HttpWeb; protected set { } }

        protected override int sdkTimeOut { get => 30000; set { } }

        // ── Входные параметры ──────────────────────────────────────────

        private string _propSampleJson;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SchemaSample)]
        /// <summary>Пример валидного JSON.</summary>
        public string Prop_SampleJson
        {
            get => _propSampleJson;
            set { _propSampleJson = value; InvokePropertyChanged(this, nameof(Prop_SampleJson)); }
        }

        private string _propTitle;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_SchemaTitle)]
        /// <summary>Заголовок схемы (title; пусто — без заголовка).</summary>
        public string Prop_Title
        {
            get => _propTitle;
            set { _propTitle = value; InvokePropertyChanged(this, nameof(Prop_Title)); }
        }

        private bool _propPrettyPrint = true;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_SchemaPretty)]
        /// <summary>Форматировать схему с отступами.</summary>
        public bool Prop_PrettyPrint
        {
            get => _propPrettyPrint;
            set { _propPrettyPrint = value; InvokePropertyChanged(this, nameof(Prop_PrettyPrint)); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propResult;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_SchemaResult)]
        /// <summary>Сгенерированная JSON Schema.</summary>
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); }
        }

        public JsonSchemaGenerateBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_JsonSchemaGenerate;
            sdkComponentHelp = @"Компонент ""JSON: Схема из примера""
Генерирует JSON Schema из примера валидного JSON (быстрый старт контракта).

Основные:
Пример JSON*: [String] Валидный JSON-документ.

Настройки:
Заголовок: [String] title схемы (пусто — без заголовка).
Форматировать: [Boolean] Отступы (по умолчанию да).

Выходные данные:
Schema: [String] JSON Schema. Проверьте ограничения (required, minLength и т.п.)
— генератор выводит типы/структуру, но не бизнес-правила.";
            sdkComponentIcon = ActivityIcons.Json;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_SampleJson", "Пример валидного JSON"),
                PropertyBuilder.String("Prop_Title", "Заголовок схемы (title)"),
                PropertyBuilder.BooleanObject("Prop_PrettyPrint", "Форматировать с отступами"),
                PropertyBuilder.Variable<string>("Prop_Result", "Сгенерированная схема")
            };

            InitClass(container);

            this.Prop_SampleJson = "\"\"";
            this.Prop_Title = "\"\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new JsonSchemaLogic();
            try
            {
                var sample = GetPropertyValue<string>(this.Prop_SampleJson, nameof(Prop_SampleJson), sd);
                var title = GetPropertyValue<string>(this.Prop_Title, nameof(Prop_Title), sd);

                var schema = logic.Generate(sample, title, this.Prop_PrettyPrint);

                SetVariableValue(this.Prop_Result, schema, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = "[JSON: Схема из примера] схема сгенерирована"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка генерации схемы: {ex.Message}"
                };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_SampleJson, ActivityStrings.Field_SchemaSample, ActivityStrings.Error_SchemaSampleRequired);
            return ret;
        }
    }
}
