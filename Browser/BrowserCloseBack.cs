// =============================================================================
// BrowserCloseBack.cs — активность «Закрыть браузер».
//
// Закрывает сессию браузера и освобождает ресурсы.
// Удаляет сессию из RepoDict.
//
// ВАЖНО: После закрытия ID сессии становится недействительным.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using OpenQA.Selenium;
using Primo.MIA.Common;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для закрытия браузера и освобождения ресурсов.
    /// REFACTORED: Использует BrowserActivityBase для устранения дублирования кода
    /// </summary>
    public class BrowserCloseBack : BrowserActivityBase<BrowserClose>
    {
        public override string GroupName
        {
            get => ActivityCategories.Browser;
            protected set { }
        }

        protected override int sdkTimeOut
        {
            get => 30000; // 30 секунд на закрытие
            set { }
        }

        // ── Входные параметры ──────────────────────────────────────────

        private string _propSessionId;
        /// <summary>ID сессии браузера для закрытия.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_SessionId)]
        public string Prop_SessionId
        {
            get => _propSessionId;
            set { _propSessionId = value; InvokePropertyChanged(this, nameof(Prop_SessionId)); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public BrowserCloseBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_BrowserClose;
            sdkComponentHelp = @"Компонент ""Закрыть браузер""
Закрывает сессию браузера и освобождает все связанные ресурсы. После выполнения идентификатор сессии становится недействительным.

Основные:
ID сессии*: [String] Идентификатор сессии, полученный из активности «Открыть браузер». Если не задан явно, используется ambient-контекст текущей сессии.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_SessionId", "ID сессии браузера для закрытия")
            };

            InitClass(container);

            this.Prop_SessionId = "\"\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return SafeExecute(() =>
            {
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);
                IWebDriver driver = GetDriverFromContext(sessionId);

                // Логирование начала операции
                Logger.LogInfo(sdkComponentName, "Начинается закрытие браузера для сессии: {0}", sessionId);

                string resolvedSessionId = SessionResolver.Resolve(sessionId);

                // issue #20: Quit/Dispose через SafeDispose.Quiet — исключение
                // освобождения уже разорванного драйвера не должно маскировать
                // результат активности
                SafeDispose.Quiet(() => driver.Quit(), $"BrowserClose: Quit сессии {resolvedSessionId}");
                SafeDispose.Quiet(() => driver.Dispose(), $"BrowserClose: Dispose сессии {resolvedSessionId}");

                // Удаление из SessionManager
                SessionManager.RemoveSession(resolvedSessionId);

                // issue #20: снимаем с реестра TTL-учёта
                BrowserSessionRegistry.Unregister(resolvedSessionId);

                // Удаление из RepoDict (для обратной совместимости)
                RepoDict.Remove(resolvedSessionId);

                // Снимаем сессию из ambient-контекста: целевой Pop — контейнер
                // закрывает свою сессию, а не вершину стека (issue #20)
                BrowserSessionContext.Pop(resolvedSessionId);

                // Логирование успешного завершения
                Logger.LogInfo(sdkComponentName, "Браузер успешно закрыт для сессии: {0}", resolvedSessionId);

                return CreateSuccessResult($"[Закрыть браузер] Сессия {resolvedSessionId} закрыта");
            }, "Закрыть браузер");
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            return ret;
        }
    }
}
