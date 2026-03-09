// =============================================================================
// ElementGetPropertyBack.cs — активность «Получить свойство элемента».
//
// Извлекает свойства элемента: текст, атрибуты, CSS свойства.
// Используется для чтения данных со страницы.
//
// Типы свойств:
//   - Text: видимый текст элемента
//   - Attribute: значение HTML атрибута
//   - CssValue: значение CSS свойства
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using OpenQA.Selenium;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для получения свойств элемента на странице.
    /// </summary>
    public class ElementGetPropertyBack : PrimoComponentTO<ElementGetProperty>
    {
        public override string GroupName
        {
            get => ActivityCategories.Browser;
            protected set { }
        }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // ── Входные параметры ──────────────────────────────────────────

        private string _propElementId;
        /// <summary>ID элемента.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ElementId)]
        public string Prop_ElementId
        {
            get => _propElementId;
            set { _propElementId = value; InvokePropertyChanged(this, "Prop_ElementId"); }
        }

        private string _propPropertyName;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_PropertyName)]
        public string Prop_PropertyName
        {
            get => _propPropertyName;
            set { _propPropertyName = value; InvokePropertyChanged(this, "Prop_PropertyName"); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propValue;
        /// <summary>Значение свойства.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ElementValue)]
        public string Prop_Value
        {
            get => _propValue;
            set { _propValue = value; InvokePropertyChanged(this, "Prop_Value"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public ElementGetPropertyBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_ElementGetProperty;
            sdkComponentHelp =
                "Получает свойство элемента на странице.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID элемента  — идентификатор элемента\n" +
                "Имя свойства — text, attribute:name, css:property\n" +
                "\n" +
                "Примеры:\n" +
                "  text              — видимый текст элемента\n" +
                "  attribute:href    — значение атрибута href\n" +
                "  attribute:value   — значение поля ввода\n" +
                "  css:color         — CSS свойство color\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Значение — полученное значение свойства";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ElementId", "ID элемента"),
                PropertyBuilder.String("Prop_PropertyName", "Имя свойства (text/attribute:name/css:property)"),
                PropertyBuilder.Variable<string>("Prop_Value", "Значение свойства")
            };

            InitClass(container);

            this.Prop_ElementId = "\"\"";
            this.Prop_PropertyName = "\"text\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // Чтение параметров
                string elementId = GetPropertyValue<string>(this.Prop_ElementId, "Prop_ElementId", sd);
                string propertyName = GetPropertyValue<string>(this.Prop_PropertyName, "Prop_PropertyName", sd);

                if (string.IsNullOrWhiteSpace(elementId))
                    throw new ArgumentException("ID элемента не может быть пустым");

                if (string.IsNullOrWhiteSpace(propertyName))
                    throw new ArgumentException("Имя свойства не может быть пустым");

                // Получение элемента
                var element = SeleniumHelper.GetElement(elementId);

                // Извлечение значения в зависимости от типа свойства
                string value = GetPropertyValue(element, propertyName);
                // Запись результата
                if (!string.IsNullOrWhiteSpace(this.Prop_Value))
                    SetVariableValue(this.Prop_Value, value, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[Получить свойство] {propertyName} = {value}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка [Получить свойство]: {ex.Message}"
                };
            }
        }

        // ── Приватные методы ───────────────────────────────────────────

        /// <summary>
        /// Извлекает значение свойства элемента по имени.
        /// Поддерживает text, attribute:name, css:property.
        /// </summary>
        private string GetPropertyValue(IWebElement element, string propertyName)
        {
            propertyName = propertyName.ToLower().Trim();

            // Текст элемента
            if (propertyName == "text")
                return element.Text ?? string.Empty;

            // HTML атрибут
            if (propertyName.StartsWith("attribute:"))
            {
                string attrName = propertyName.Substring("attribute:".Length).Trim();
                return element.GetAttribute(attrName) ?? string.Empty;
            }

            // CSS свойство
            if (propertyName.StartsWith("css:"))
            {
                string cssName = propertyName.Substring("css:".Length).Trim();
                return element.GetCssValue(cssName) ?? string.Empty;
            }

            // По умолчанию пытаемся получить как атрибут
            return element.GetAttribute(propertyName) ?? string.Empty;
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_ElementId, ActivityStrings.Field_ElementId, "ID элемента обязателен");
            ret.ValidateRequired(this.Prop_PropertyName, ActivityStrings.Field_PropertyName, "Имя свойства обязательно");
            return ret;
        }
    }
}
