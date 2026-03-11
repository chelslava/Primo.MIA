// =============================================================================
// AlertHandleBack.cs — активность «Обработка алерта».
//
// Работает с JavaScript алертами, подтверждениями и промптами.
// Поддерживает принятие, отклонение, чтение текста и ввод данных.
//
// Действия:
//   Accept   — принять алерт (OK)
//   Dismiss  — отклонить алерт (Cancel)
//   GetText  — получить текст алерта
//   SendKeys — ввести текст в prompt
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using OpenQA.Selenium;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для работы с JavaScript алертами.
    /// REFACTORED: Использует BrowserActivityBase для устранения дублирования кода
    /// </summary>
    public class AlertHandleBack : BrowserActivityBase<AlertHandle>
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

        private AlertAction _propAction;
        /// <summary>Действие с алертом.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_AlertAction)]
        public AlertAction Prop_Action
        {
            get => _propAction;
            set { _propAction = value; InvokePropertyChanged(this, "Prop_Action"); }
        }

        private string _propInputText;
        /// <summary>Текст для ввода в prompt.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_AlertInput)]
        public string Prop_InputText
        {
            get => _propInputText;
            set { _propInputText = value; InvokePropertyChanged(this, "Prop_InputText"); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propAlertText;
        /// <summary>Текст алерта.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_AlertText)]
        public string Prop_AlertText
        {
            get => _propAlertText;
            set { _propAlertText = value; InvokePropertyChanged(this, "Prop_AlertText"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public AlertHandleBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_AlertHandle;
            sdkComponentHelp =
                "Работает с JavaScript алертами, подтверждениями и промптами.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "Действие  — операция с алертом\n" +
                "\n" +
                "Действия:\n" +
                "  Accept   — принять (OK)\n" +
                "  Dismiss  — отклонить (Cancel)\n" +
                "  GetText  — получить текст\n" +
                "  SendKeys — ввести текст в prompt\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Текст алерта — содержимое сообщения";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Enum<AlertAction>("Prop_Action", "Действие с алертом"),
                PropertyBuilder.String("Prop_InputText", "Текст для ввода в prompt"),
                PropertyBuilder.Variable<string>("Prop_AlertText", "Текст алерта")
            };

            InitClass(container);

            Prop_SessionId = "\"\"";
            Prop_Action = AlertAction.Accept;
            Prop_InputText = "\"\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // Получаем sessionId
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);
                string inputText = GetPropertyValue<string>(Prop_InputText, nameof(Prop_InputText), sd) ?? string.Empty;

                // Получение драйвера через базовый класс
                IWebDriver driver = GetDriverFromContext(sessionId);

                // Переключение на алерт
                IAlert alert = driver.SwitchTo().Alert();

                string alertText = string.Empty;
                string actionMessage = string.Empty;

                // Выполнение действия
                switch (Prop_Action)
                {
                    case AlertAction.Accept:
                        alert.Accept();
                        actionMessage = "Принят";
                        break;

                    case AlertAction.Dismiss:
                        alert.Dismiss();
                        actionMessage = "Отклонён";
                        break;

                    case AlertAction.GetText:
                        alertText = alert.Text ?? string.Empty;
                        actionMessage = $"Текст: {alertText}";
                        break;

                    case AlertAction.SendKeys:
                        if (string.IsNullOrEmpty(inputText))
                            throw new ArgumentException("Текст для ввода не может быть пустым при действии SendKeys");
                        alert.SendKeys(inputText);
                        alert.Accept();
                        actionMessage = $"Введён текст: {inputText}";
                        break;

                    default:
                        throw new NotSupportedException($"Действие {Prop_Action} не поддерживается");
                }

                // Запись текста алерта
                if (!string.IsNullOrWhiteSpace(Prop_AlertText) && !string.IsNullOrEmpty(alertText))
                    SetVariableValue(Prop_AlertText, alertText, sd);

                return CreateSuccessResult($"[Обработка алерта] {actionMessage}");
            }
            catch (NoAlertPresentException)
            {
                return CreateErrorResult("Алерт не найден");
            }
            catch (Exception ex)
            {
                return CreateErrorResult(ex, "Обработка алерта");
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();


            // Для SendKeys текст обязателен
            if (Prop_Action == AlertAction.SendKeys)
                ret.ValidateRequired(Prop_InputText, ActivityStrings.Field_AlertInput, "Текст для ввода обязателен");

            return ret;
        }
    }
}
