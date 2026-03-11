// =============================================================================
// BrowserScreenshotBack.cs — активность «Скриншот браузера».
//
// Создаёт скриншот текущей страницы браузера.
// Поддерживает сохранение в файл и получение Base64 строки.
//
// Форматы вывода:
//   - Файл PNG по указанному пути
//   - Base64 строка для встраивания или передачи
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using OpenQA.Selenium;
using Primo.MIA.Common;
using System.Collections.Generic;
using System.IO;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для создания скриншота страницы браузера.
    /// REFACTORED: Использует BrowserActivityBase и сервисы для устранения дублирования кода
    /// </summary>
    public class BrowserScreenshotBack : BrowserActivityBase<BrowserScreenshot>
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

        private string _propFilePath;
        /// <summary>Путь для сохранения скриншота (опционально).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ScreenshotPath)]
        public string Prop_FilePath
        {
            get => _propFilePath;
            set { _propFilePath = value; InvokePropertyChanged(this, "Prop_FilePath"); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propBase64;
        /// <summary>Скриншот в формате Base64.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ScreenshotBase64)]
        public string Prop_Base64
        {
            get => _propBase64;
            set { _propBase64 = value; InvokePropertyChanged(this, "Prop_Base64"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public BrowserScreenshotBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_BrowserScreenshot;
            sdkComponentHelp =
                "Создаёт скриншот текущей страницы браузера.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии        — идентификатор сессии браузера\n" +
                "Путь к файлу     — путь для сохранения PNG (опционально)\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Base64           — скриншот в формате Base64 строки\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Если путь не указан, скриншот доступен только в Base64.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.FileSelector("Prop_FilePath", "Путь для сохранения скриншота"),
                PropertyBuilder.Variable<string>("Prop_Base64", "Скриншот в формате Base64")
            };

            InitClass(container);

            Prop_SessionId = "\"\"";
            Prop_FilePath = "\"\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            string message = string.Empty;

            var result = SafeExecute(() =>
            {
                // Получаем sessionId
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);

                // Получение драйвера через базовый класс
                IWebDriver driver = GetDriverFromContext(sessionId);

                string filePath = GetPropertyValue<string>(Prop_FilePath, nameof(Prop_FilePath), sd) ?? string.Empty;

                // Логирование начала операции
                Logger.LogInfo(sdkComponentName, "Создание скриншота для сессии: {0}", sessionId);

                // Создание скриншота
                var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                string base64 = screenshot.AsBase64EncodedString;

                // Сохранение в файл если указан путь
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    // Создание директории если не существует
                    string directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    // Сохранение
                    screenshot.SaveAsFile(filePath);
                    Logger.LogInfo(sdkComponentName, "Скриншот сохранен в файл: {0}", filePath);
                }

                // Запись Base64 в выходную переменную
                if (!string.IsNullOrWhiteSpace(Prop_Base64))
                    SetVariableValue(Prop_Base64, base64, sd);

                message = string.IsNullOrWhiteSpace(filePath)
                    ? "[Скриншот] Создан (Base64)"
                    : $"[Скриншот] Сохранён: {filePath}";

                Logger.LogInfo(sdkComponentName, "Скриншот успешно создан");

            }, "Скриншот");

            if (result.IsSuccess)
                result.SuccessMessage = message;

            return result;
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            return ret;
        }
    }
}