// =============================================================================
// JsonSchemaValidateBack.cs — активность «JSON: Валидация по схеме»
// (issue #34). Контрактный контроль JSON-сообщений (webhook/API ответы).
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System.Data;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность валидации JSON по JSON Schema.</summary>
    public class JsonSchemaValidateBack : PrimoComponentTO<JsonSchemaValidate>
    {
        public override string GroupName { get => ActivityCategories.HttpWeb; protected set { } }

        protected override int sdkTimeOut { get => 30000; set { } }

        // ── Входные параметры ──────────────────────────────────────────

        private string _propJson;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SchemaJson)]
        /// <summary>Проверяемый JSON.</summary>
        public string Prop_Json
        {
            get => _propJson;
            set { _propJson = value; InvokePropertyChanged(this, nameof(Prop_Json)); }
        }

        private string _propSchema;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SchemaDefinition)]
        /// <summary>JSON Schema.</summary>
        public string Prop_Schema
        {
            get => _propSchema;
            set { _propSchema = value; InvokePropertyChanged(this, nameof(Prop_Schema)); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propIsValid;
        /// <summary>JSON соответствует схеме (имя выходной переменной).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_SchemaIsValid)]
        public string Prop_IsValid
        {
            get => _propIsValid;
            set { _propIsValid = value; InvokePropertyChanged(this, nameof(Prop_IsValid)); }
        }

        private string _propErrorCount;
        /// <summary>Число ошибок (имя выходной переменной).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_SchemaErrorCount)]
        public string Prop_ErrorCount
        {
            get => _propErrorCount;
            set { _propErrorCount = value; InvokePropertyChanged(this, nameof(Prop_ErrorCount)); }
        }

        private string _propErrors;
        /// <summary>Ошибки: path, error (имя выходной переменной).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_SchemaErrors)]
        public string Prop_Errors
        {
            get => _propErrors;
            set { _propErrors = value; InvokePropertyChanged(this, nameof(Prop_Errors)); }
        }

        public JsonSchemaValidateBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_JsonSchemaValidate;
            sdkComponentHelp = @"Компонент ""JSON: Валидация по схеме""
Проверяет JSON по JSON Schema (NJsonSchema, draft-07 подмножество).

Основные:
JSON*: [String] Проверяемый JSON.
Schema*: [String] JSON Schema документа.

Выходные данные:
Валиден: [Boolean] Соответствует ли схема.
Ошибок: [Int32]
Ошибки: [DataTable] Колонки path (путь в данных) и error (вид нарушения).

Схему можно сгенерировать из примера активностью «JSON: Схема из примера».";
            sdkComponentIcon = ActivityIcons.Json;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_Json", "Проверяемый JSON"),
                PropertyBuilder.String("Prop_Schema", "JSON Schema"),
                PropertyBuilder.Variable<bool>("Prop_IsValid", "Валиден"),
                PropertyBuilder.Variable<int>("Prop_ErrorCount", "Число ошибок"),
                PropertyBuilder.Variable<DataTable>("Prop_Errors", "Ошибки (path, error)")
            };

            InitClass(container);

            this.Prop_Json = "\"\"";
            this.Prop_Schema = "\"\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new JsonSchemaLogic();
            try
            {
                var json = GetPropertyValue<string>(this.Prop_Json, nameof(Prop_Json), sd);
                var schema = GetPropertyValue<string>(this.Prop_Schema, nameof(Prop_Schema), sd);

                var result = logic.Validate(json, schema);

                SetVariableValue(this.Prop_IsValid, result.IsValid, sd);
                SetVariableValue(this.Prop_ErrorCount, result.Errors.Count, sd);
                SetVariableValue(this.Prop_Errors, logic.ErrorsToDataTable(result), sd);

                return new ExecutionResult
                {
                    IsSuccess = result.IsValid,
                    SuccessMessage = $"[JSON: Валидация по схеме] JSON соответствует схеме",
                    ErrorMessage = result.IsValid
                        ? null
                        : $"JSON не соответствует схеме: {result.Errors.Count} нарушений"
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
                    ErrorMessage = $"Ошибка валидации: {ex.Message}"
                };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Json, ActivityStrings.Field_SchemaJson, ActivityStrings.Error_SchemaJsonRequired);
            ret.ValidateRequired(this.Prop_Schema, ActivityStrings.Field_SchemaDefinition, ActivityStrings.Error_SchemaDefinitionRequired);
            return ret;
        }
    }
}
