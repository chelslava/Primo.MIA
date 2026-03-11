// =============================================================================
// ElementUploadFileBack.cs — активность «Загрузка файла».
//
// Загружает файл через input[type=file] элемент на странице.
// Автоматически проверяет существование файла перед загрузкой.
//
// Используется для:
//   - Загрузки файлов через веб-формы
//   - Прикрепления документов
//   - Импорта данных через веб-интерфейс
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
    /// Активность для загрузки файла через input[type=file] элемент.
    /// REFACTORED: Использует BrowserActivityBase для устранения дублирования кода
    /// </summary>
    public class ElementUploadFileBack : BrowserActivityBase<ElementUploadFile>
    {
        public override string GroupName
        {
            get => ActivityCategories.Browser;
            protected set { }
        }

        protected override int sdkTimeOut
        {
            get => 30000;
            set { }
        }

        // ── Входные параметры ──────────────────────────────────────────

        private string _propSessionId;
        /// <summary>ID сессии браузера.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_SessionId)]
        public string Prop_SessionId
        {
            get => _propSessionId;
            set { _propSessionId = value; InvokePropertyChanged(this, "Prop_SessionId"); }
        }

        private string _propElementId;
        /// <summary>ID элемента input[type=file].</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ElementId)]
        public string Prop_ElementId
        {
            get => _propElementId;
            set { _propElementId = value; InvokePropertyChanged(this, "Prop_ElementId"); }
        }

        private ElementLocatorType _propLocatorType;
        /// <summary>Тип локатора для поиска элемента.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator),
         System.ComponentModel.DisplayName(ActivityStrings.Field_LocatorType)]
        public ElementLocatorType Prop_LocatorType
        {
            get => _propLocatorType;
            set { _propLocatorType = value; InvokePropertyChanged(this, "Prop_LocatorType"); }
        }

        private string _propLocatorValue;
        /// <summary>Значение локатора для поиска элемента.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator),
         System.ComponentModel.DisplayName(ActivityStrings.Field_LocatorValue)]
        public string Prop_LocatorValue
        {
            get => _propLocatorValue;
            set { _propLocatorValue = value; InvokePropertyChanged(this, "Prop_LocatorValue"); }
        }

        private string _propFilePath;
        /// <summary>Путь к файлу для загрузки.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Путь к файлу")]
        public string Prop_FilePath
        {
            get => _propFilePath;
            set { _propFilePath = value; InvokePropertyChanged(this, "Prop_FilePath"); }
        }

        private string _propWaitTimeout;
        /// <summary>Таймаут ожидания элемента (сек).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Wait),
         System.ComponentModel.DisplayName(ActivityStrings.Field_WaitTimeout)]
        public string Prop_WaitTimeout
        {
            get => _propWaitTimeout;
            set { _propWaitTimeout = value; InvokePropertyChanged(this, "Prop_WaitTimeout"); }
        }

        private bool _propVerifyFileExists;
        /// <summary>Проверить существование файла перед загрузкой.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Проверить существование файла")]
        public bool Prop_VerifyFileExists
        {
            get => _propVerifyFileExists;
            set { _propVerifyFileExists = value; InvokePropertyChanged(this, "Prop_VerifyFileExists"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public ElementUploadFileBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Элемент: Загрузка файла";
            sdkComponentHelp =
                "Загружает файл через input[type=file] элемент.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "ID элемента — идентификатор input[type=file]\n" +
                "Путь к файлу — полный путь к загружаемому файлу\n" +
                "Проверить существование — проверка файла перед загрузкой\n" +
                "\n" +
                "── Локатор (альтернатива ID элемента) ────────\n" +
                "Тип локатора — способ поиска элемента\n" +
                "Значение локатора — конкретное значение для поиска\n" +
                "\n" +
                "── Ожидание ──────────────────────────────────\n" +
                "Таймаут (сек) — время ожидания появления элемента\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Элемент должен быть input[type=file].\n" +
                "Путь к файлу должен быть абсолютным.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_ElementId", "ID элемента (если уже найден)"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue", "Значение локатора"),
                PropertyBuilder.String("Prop_FilePath", "Путь к файлу"),
                PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания элемента (сек)"),
                PropertyBuilder.BooleanObject("Prop_VerifyFileExists", "Проверить существование файла")
            };

            InitClass(container);

            this.Prop_SessionId = "\"\"";
            this.Prop_ElementId = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.Id;
            this.Prop_LocatorValue = "\"\"";
            this.Prop_FilePath = "\"\"";
            this.Prop_WaitTimeout = "10";
            this.Prop_VerifyFileExists = true;
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);
                string elementId = GetPropertyValue<string>(Prop_ElementId, nameof(Prop_ElementId), sd);
                string locatorValue = GetPropertyValue<string>(Prop_LocatorValue, nameof(Prop_LocatorValue), sd);
                string filePath = GetPropertyValue<string>(Prop_FilePath, nameof(Prop_FilePath), sd);

                if (string.IsNullOrWhiteSpace(filePath))
                    throw new ArgumentException("Путь к файлу не может быть пустым");

                if (Prop_VerifyFileExists && !System.IO.File.Exists(filePath))
                    throw new System.IO.FileNotFoundException($"Файл не найден: {filePath}");

                IWebDriver driver = GetDriverFromContext(sessionId);

                IWebElement element;
                if (!string.IsNullOrWhiteSpace(locatorValue))
                {
                    string timeoutStr = GetPropertyValue<string>(Prop_WaitTimeout, "Prop_WaitTimeout", sd) ?? "10";
                    int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
                    timeout = SeleniumHelper.ValidateTimeout(timeout, 10);

                    var locator = SeleniumHelper.CreateLocator(Prop_LocatorType, locatorValue);
                    element = SeleniumHelper.WaitForElement(driver, locator, timeout);
                }
                else if (!string.IsNullOrWhiteSpace(elementId))
                {
                    element = SeleniumHelper.GetElement(elementId);
                }
                else
                {
                    throw new ArgumentException("Необходимо указать либо ID элемента, либо локатор для поиска");
                }

                if (!SeleniumHelper.IsFileInputElement(element))
                    throw new ArgumentException("Элемент должен быть input[type=file]");

                SeleniumHelper.UploadFile(element, filePath);

                string fileName = System.IO.Path.GetFileName(filePath);
                return CreateSuccessResult($"[Загрузка файла] Файл загружен: {fileName}");
            }
            catch (Exception ex)
            {
                return CreateErrorResult(ex, "Загрузка файла");
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
             
            ret.ValidateRequired(this.Prop_FilePath, "Путь к файлу", "Путь к файлу обязателен");

            bool hasElementId = !string.IsNullOrWhiteSpace(this.Prop_ElementId);
            bool hasLocator = !string.IsNullOrWhiteSpace(this.Prop_LocatorValue);

            if (!hasElementId && !hasLocator)
            {
                ret.Items.Add(new ValidationResult.ValidationItem()
                {
                    PropertyName = "ElementId/LocatorValue",
                    Error = "Необходимо указать либо ID элемента, либо локатор для поиска"
                });
            }

            return ret;
        }
    }
}
