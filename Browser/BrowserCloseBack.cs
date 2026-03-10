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
using System;
using System.Collections.Generic;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для закрытия браузера и освобождения ресурсов.
    /// </summary>
    public class BrowserCloseBack : PrimoComponentTO<BrowserClose>
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
            set { _propSessionId = value; InvokePropertyChanged(this, "Prop_SessionId"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public BrowserCloseBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_BrowserClose;
            sdkComponentHelp =
                "Закрывает браузер и освобождает ресурсы.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии из BrowserOpen\n" +
                "\n" +
                "ВАЖНО: После закрытия сессия становится недействительной.";

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
            try
            {
                // Чтение ID сессии через SessionResolver (поддержка ambient-контекста)
                string sessionId = SessionResolver.Resolve(
                    GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));

                // Получение драйвера
                var driver = SeleniumHelper.GetDriver(sessionId);

                // Закрытие браузера
                driver.Quit();
                driver.Dispose();

                // Удаление из RepoDict
                RepoDict.Remove(sessionId);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[Закрыть браузер] Сессия {sessionId} закрыта"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка [Закрыть браузер]: {ex.Message}"
                };
            }
            finally
            {
                // Снимаем сессию из ambient-контекста
                BrowserSessionContext.Pop();
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
             
            return ret;
        }
    }
}
