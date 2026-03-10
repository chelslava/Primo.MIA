// =============================================================================
// BrowserTabManageBack.cs — активность «Управление вкладками браузера».
//
// Открывает, закрывает и переключается между вкладками браузера.
// Поддерживает получение списка всех открытых вкладок.
//
// Используется для:
//   - Открытия новых вкладок
//   - Закрытия текущей или конкретной вкладки
//   - Переключения между вкладками
//   - Получения информации о вкладках
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
    /// Активность для управления вкладками браузера.
    /// </summary>
    public class BrowserTabManageBack : PrimoComponentTO<BrowserTabManage>
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

        private TabOperation _propOperation;
        /// <summary>Тип операции с вкладками.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Операция")]
        public TabOperation Prop_Operation
        {
            get => _propOperation;
            set { _propOperation = value; InvokePropertyChanged(this, "Prop_Operation"); }
        }

        private string _propTabIndex;
        /// <summary>Индекс вкладки (для SwitchToTab).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Индекс вкладки")]
        public string Prop_TabIndex
        {
            get => _propTabIndex;
            set { _propTabIndex = value; InvokePropertyChanged(this, "Prop_TabIndex"); }
        }

        private string _propTabHandle;
        /// <summary>Handle вкладки (для CloseTabByHandle).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Handle вкладки")]
        public string Prop_TabHandle
        {
            get => _propTabHandle;
            set { _propTabHandle = value; InvokePropertyChanged(this, "Prop_TabHandle"); }
        }

        private string _propUrl;
        /// <summary>URL для открытия в новой вкладке.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("URL")]
        public string Prop_Url
        {
            get => _propUrl;
            set { _propUrl = value; InvokePropertyChanged(this, "Prop_Url"); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propOutHandles;
        /// <summary>Список всех handles вкладок.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Список handles")]
        public string Prop_OutHandles
        {
            get => _propOutHandles;
            set { _propOutHandles = value; InvokePropertyChanged(this, "Prop_OutHandles"); }
        }

        private string _propOutCurrentHandle;
        /// <summary>Handle текущей вкладки.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Текущий handle")]
        public string Prop_OutCurrentHandle
        {
            get => _propOutCurrentHandle;
            set { _propOutCurrentHandle = value; InvokePropertyChanged(this, "Prop_OutCurrentHandle"); }
        }

        private string _propOutTabCount;
        /// <summary>Количество открытых вкладок.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Количество вкладок")]
        public string Prop_OutTabCount
        {
            get => _propOutTabCount;
            set { _propOutTabCount = value; InvokePropertyChanged(this, "Prop_OutTabCount"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public BrowserTabManageBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Браузер: Управление вкладками";
            sdkComponentHelp =
                "Управляет вкладками браузера.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "Операция — тип операции с вкладками\n" +
                "\n" +
                "── Операции ──────────────────────────────────\n" +
                "OpenNewTab — открыть новую вкладку (опционально с URL)\n" +
                "CloseCurrentTab — закрыть текущую вкладку\n" +
                "CloseTabByHandle — закрыть вкладку по handle\n" +
                "GetAllHandles — получить список всех handles\n" +
                "GetCurrentHandle — получить handle текущей вкладки\n" +
                "SwitchToTab — переключиться на вкладку по индексу\n" +
                "\n" +
                "── Выходные данные ───────────────────────────\n" +
                "Для операций Get* возвращаются соответствующие значения.\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Handle — уникальный идентификатор вкладки.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Enum<TabOperation>("Prop_Operation", "Операция"),
                PropertyBuilder.Int("Prop_TabIndex", "Индекс вкладки"),
                PropertyBuilder.String("Prop_TabHandle", "Handle вкладки"),
                PropertyBuilder.String("Prop_Url", "URL"),
                PropertyBuilder.Variable<List<string>>("Prop_OutHandles", "Список handles"),
                PropertyBuilder.Variable<string>("Prop_OutCurrentHandle", "Текущий handle"),
                PropertyBuilder.Variable<int>("Prop_OutTabCount", "Количество вкладок")
            };

            InitClass(container);

            this.Prop_SessionId = "\"\"";
            this.Prop_Operation = TabOperation.OpenNewTab;
            this.Prop_TabIndex = "0";
            this.Prop_TabHandle = "\"\"";
            this.Prop_Url = "\"\"";
            this.Prop_OutHandles = "";
            this.Prop_OutCurrentHandle = "";
            this.Prop_OutTabCount = "";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                string sessionId = SessionResolver.Resolve(
                    GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));

                var driver = SeleniumHelper.GetDriver(sessionId);
                string resultMsg;

                switch (this.Prop_Operation)
                {
                    case TabOperation.OpenNewTab:
                        {
                            string url = GetPropertyValue<string>(this.Prop_Url, "Prop_Url", sd) ?? "";
                            SeleniumHelper.OpenNewTab(driver, url);
                            resultMsg = string.IsNullOrWhiteSpace(url)
                                ? "[Управление вкладками] Новая вкладка открыта"
                                : $"[Управление вкладками] Новая вкладка открыта: {url}";
                        }
                        break;

                    case TabOperation.CloseCurrentTab:
                        SeleniumHelper.CloseCurrentTab(driver);
                        resultMsg = "[Управление вкладками] Текущая вкладка закрыта";
                        break;

                    case TabOperation.CloseTabByHandle:
                        {
                            string handle = GetPropertyValue<string>(this.Prop_TabHandle, "Prop_TabHandle", sd);
                            if (string.IsNullOrWhiteSpace(handle))
                                throw new ArgumentException("Handle вкладки не может быть пустым");

                            SeleniumHelper.CloseTabByHandle(driver, handle);
                            resultMsg = $"[Управление вкладками] Вкладка закрыта: {handle}";
                        }
                        break;

                    case TabOperation.GetAllHandles:
                        {
                            var handles = SeleniumHelper.GetAllWindowHandles(driver);
                            SetVariableValue(this.Prop_OutHandles, handles, sd);
                            SetVariableValue(this.Prop_OutTabCount, handles.Count, sd);
                            resultMsg = $"[Управление вкладками] Получено handles: {handles.Count}";
                        }
                        break;

                    case TabOperation.GetCurrentHandle:
                        {
                            string currentHandle = SeleniumHelper.GetCurrentWindowHandle(driver);
                            SetVariableValue(this.Prop_OutCurrentHandle, currentHandle, sd);
                            resultMsg = $"[Управление вкладками] Текущий handle: {currentHandle}";
                        }
                        break;

                    case TabOperation.SwitchToTab:
                        {
                            string indexStr = GetPropertyValue<string>(this.Prop_TabIndex, "Prop_TabIndex", sd) ?? "0";
                            int index = int.TryParse(indexStr, out int idx) ? idx : 0;

                            SeleniumHelper.SwitchToTabByIndex(driver, index);
                            resultMsg = $"[Управление вкладками] Переключено на вкладку: {index}";
                        }
                        break;

                    default:
                        throw new NotSupportedException($"Операция {this.Prop_Operation} не поддерживается");
                }

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = resultMsg
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка [Управление вкладками]: {ex.Message}"
                };
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
             

            switch (this.Prop_Operation)
            {
                case TabOperation.CloseTabByHandle:
                    ret.ValidateRequired(this.Prop_TabHandle, "Handle вкладки", "Handle обязателен для операции CloseTabByHandle");
                    break;

                case TabOperation.SwitchToTab:
                    ret.ValidateRequired(this.Prop_TabIndex, "Индекс вкладки", "Индекс обязателен для операции SwitchToTab");
                    break;

                case TabOperation.GetAllHandles:
                    ret.ValidateRequired(this.Prop_OutHandles, "Список handles", "Переменная для списка handles обязательна");
                    break;

                case TabOperation.GetCurrentHandle:
                    ret.ValidateRequired(this.Prop_OutCurrentHandle, "Текущий handle", "Переменная для текущего handle обязательна");
                    break;
            }

            return ret;
        }
    }
}
