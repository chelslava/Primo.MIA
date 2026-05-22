// =============================================================================
// BrowserSwitchToBack.cs — активность «Переключить контекст».
//
// Переключает контекст браузера между фреймами, окнами и основным контентом.
// Необходимо для работы с iframe, popup окнами и вкладками.
//
// Типы переключения:
//   Frame          — переключиться в iframe
//   Window         — переключиться в другое окно/вкладку
//   Alert          — переключиться на алерт
//   DefaultContent — вернуться к основному контенту
//   ParentFrame    — вернуться к родительскому фрейму
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using OpenQA.Selenium;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для переключения контекста браузера.
    /// REFACTORED: Использует BrowserActivityBase для устранения дублирования кода
    /// </summary>
    public class BrowserSwitchToBack : BrowserActivityBase<BrowserSwitchTo>
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

        private SwitchToType _propSwitchType;
        /// <summary>Тип переключения контекста.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_SwitchToType)]
        public SwitchToType Prop_SwitchType
        {
            get => _propSwitchType;
            set { _propSwitchType = value; InvokePropertyChanged(this, "Prop_SwitchType"); }
        }

        private string _propTarget;
        /// <summary>Цель переключения (индекс фрейма, handle окна или локатор).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Цель переключения")]
        public string Prop_Target
        {
            get => _propTarget;
            set { _propTarget = value; InvokePropertyChanged(this, "Prop_Target"); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propWindowHandles;
        /// <summary>Список handles всех окон (для Window).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_WindowHandles)]
        public string Prop_WindowHandles
        {
            get => _propWindowHandles;
            set { _propWindowHandles = value; InvokePropertyChanged(this, "Prop_WindowHandles"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public BrowserSwitchToBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_BrowserSwitchTo;
            sdkComponentHelp =
                "Переключает контекст браузера.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "Тип       — куда переключиться\n" +
                "Цель      — индекс/handle/локатор\n" +
                "\n" +
                "Типы переключения:\n" +
                "  Frame          — iframe по индексу или локатору\n" +
                "  Window         — окно/вкладка по handle\n" +
                "  Alert          — JavaScript алерт\n" +
                "  DefaultContent — основной контент страницы\n" +
                "  ParentFrame    — родительский фрейм\n" +
                "\n" +
                "Примеры целей:\n" +
                "  Frame: \"0\" (индекс) или \"iframe-id\" (локатор)\n" +
                "  Window: handle из WindowHandles\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Window Handles — список handles всех окон";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Enum<SwitchToType>("Prop_SwitchType", "Тип переключения"),
                PropertyBuilder.String("Prop_Target", "Цель переключения"),
                PropertyBuilder.Variable<List<string>>("Prop_WindowHandles", "Список handles окон")
            };

            InitClass(container);

            Prop_SessionId = "\"\"";
            Prop_SwitchType = SwitchToType.DefaultContent;
            Prop_Target = "\"\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // Получаем sessionId
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);
                string target = GetPropertyValue<string>(Prop_Target, nameof(Prop_Target), sd) ?? string.Empty;

                // Получение драйвера через базовый класс
                IWebDriver driver = GetDriverFromContext(sessionId);

                string actionMessage = string.Empty;

                // Переключение в зависимости от типа
                switch (Prop_SwitchType)
                {
                case SwitchToType.Frame:
                        Guard.NotNullOrWhiteSpace(target, nameof(target));

                        // Попытка переключиться по индексу или локатору
                        if (int.TryParse(target, out int frameIndex))
                        {
                            driver.SwitchTo().Frame(frameIndex);
                            actionMessage = $"Переключено в frame #{frameIndex}";
                        }
                        else
                        {
                            driver.SwitchTo().Frame(target);
                            actionMessage = $"Переключено в frame '{target}'";
                        }
                        break;

                case SwitchToType.Window:
                        Guard.NotNullOrWhiteSpace(target, nameof(target));

                        driver.SwitchTo().Window(target);
                        actionMessage = $"Переключено в окно {target}";

                        // Получение списка всех handles
                        var handles = driver.WindowHandles.ToList();
                        if (!string.IsNullOrWhiteSpace(Prop_WindowHandles))
                            SetVariableValue(Prop_WindowHandles, handles, sd);
                        break;

                    case SwitchToType.Alert:
                        driver.SwitchTo().Alert();
                        actionMessage = "Переключено на алерт";
                        break;

                    case SwitchToType.DefaultContent:
                        driver.SwitchTo().DefaultContent();
                        actionMessage = "Возврат к основному контенту";
                        break;

                    case SwitchToType.ParentFrame:
                        driver.SwitchTo().ParentFrame();
                        actionMessage = "Возврат к родительскому фрейму";
                        break;

                    default:
                        throw new NotSupportedException($"Тип {Prop_SwitchType} не поддерживается");
                }

                return CreateSuccessResult($"[Переключить контекст] {actionMessage}");
            }
            catch (Exception ex)
            {
                return CreateErrorResult(ex, "Переключить контекст");
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            // Для Frame и Window цель обязательна
            if (Prop_SwitchType == SwitchToType.Frame ||
                Prop_SwitchType == SwitchToType.Window)
            {
                ret.ValidateRequired(Prop_Target, "Цель переключения", "Цель переключения обязательна для Frame и Window");
            }

            return ret;
        }
    }
}
