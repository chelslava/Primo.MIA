// =============================================================================
// HttpPaginateBack.cs — активность «HTTP: Пагинация» (issue #32).
//
// Итератор REST-пагинации: По номеру страницы ({page}) или По курсору
// ({cursor} из ответа по JSONPath). Агрегация: сырые JSON-элементы.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Primo.MIA
{
    /// <summary>Активность обхода REST-пагинации.</summary>
    public class HttpPaginateBack : PrimoComponentTO<HttpPaginate>
    {
        public override string GroupName { get => ActivityCategories.HttpWeb; protected set { } }

        protected override int sdkTimeOut { get => 3600000; set { } }

        // ── Входные параметры ──────────────────────────────────────────

        private HttpPaginationMode _propMode = HttpPaginationMode.PageNumber;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_PaginateMode)]
        /// <summary>Режим пагинации.</summary>
        public HttpPaginationMode Prop_Mode
        {
            get => _propMode;
            set { _propMode = value; InvokePropertyChanged(this, nameof(Prop_Mode)); }
        }

        private string _propUrlTemplate;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_PaginateUrlTemplate)]
        /// <summary>Шаблон URL с {page} или {cursor}.</summary>
        public string Prop_UrlTemplate
        {
            get => _propUrlTemplate;
            set { _propUrlTemplate = value; InvokePropertyChanged(this, nameof(Prop_UrlTemplate)); }
        }

        private string _propItemsPath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_PaginateItemsPath)]
        /// <summary>JSONPath к массиву элементов страницы.</summary>
        public string Prop_ItemsPath
        {
            get => _propItemsPath;
            set { _propItemsPath = value; InvokePropertyChanged(this, nameof(Prop_ItemsPath)); }
        }

        private string _propCursorPath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_PaginateCursorPath)]
        /// <summary>JSONPath следующего курсора (режим «По курсору»).</summary>
        public string Prop_CursorPath
        {
            get => _propCursorPath;
            set { _propCursorPath = value; InvokePropertyChanged(this, nameof(Prop_CursorPath)); }
        }

        private string _propStartPage;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_PaginateStartPage)]
        /// <summary>Начальная страница (режим «По номеру»).</summary>
        public string Prop_StartPage
        {
            get => _propStartPage;
            set { _propStartPage = value; InvokePropertyChanged(this, nameof(Prop_StartPage)); }
        }

        private string _propMaxPages;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_PaginateMaxPages)]
        /// <summary>Лимит страниц (защита от бесконечного цикла).</summary>
        public string Prop_MaxPages
        {
            get => _propMaxPages;
            set { _propMaxPages = value; InvokePropertyChanged(this, nameof(Prop_MaxPages)); }
        }

        private bool _propStopOnEmptyPage = true;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_PaginateStopOnEmpty)]
        /// <summary>Останавливаться на первой пустой странице.</summary>
        public bool Prop_StopOnEmptyPage
        {
            get => _propStopOnEmptyPage;
            set { _propStopOnEmptyPage = value; InvokePropertyChanged(this, nameof(Prop_StopOnEmptyPage)); }
        }

        private bool _propIgnoreSslErrors;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_WaitHttpIgnoreSsl)]
        /// <summary>Игнорировать ошибки SSL (только тестовые среды).</summary>
        public bool Prop_IgnoreSslErrors
        {
            get => _propIgnoreSslErrors;
            set { _propIgnoreSslErrors = value; InvokePropertyChanged(this, nameof(Prop_IgnoreSslErrors)); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propItems;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_PaginateItems)]
        /// <summary>Элементы всех страниц (сырой JSON каждого).</summary>
        public string Prop_Items
        {
            get => _propItems;
            set { _propItems = value; InvokePropertyChanged(this, nameof(Prop_Items)); }
        }

        private string _propPageCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_PaginatePageCount)]
        /// <summary>Загружено страниц (имя выходной переменной).</summary>
        public string Prop_PageCount
        {
            get => _propPageCount;
            set { _propPageCount = value; InvokePropertyChanged(this, nameof(Prop_PageCount)); }
        }

        private string _propNextCursor;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_PaginateNextCursor)]
        /// <summary>Последний курсор (для возобновления).</summary>
        public string Prop_NextCursor
        {
            get => _propNextCursor;
            set { _propNextCursor = value; InvokePropertyChanged(this, nameof(Prop_NextCursor)); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public HttpPaginateBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_HttpPaginate;
            sdkComponentHelp = @"Компонент ""HTTP: Пагинация""
Обходит страницы REST API и агрегирует элементы (для long-running выборок).

Основные:
Режим: [Enum] По номеру страницы ({page}) / По курсору ({cursor}).
Шаблон URL*: [String] Примеры: https://api/x?page={page}&size=50 | https://api/x?cursor={cursor}
JSONPath элементов*: [String] Путь к массиву: $.data.items[*]

Настройки:
JSONPath курсора: [String] Для режима «По курсору»: $.meta.next_cursor
Начальная страница: [Int32] По умолчанию 1.
Лимит страниц: [Int32] По умолчанию 100 (защита от бесконечного цикла).
Стоп на пустой странице: [Boolean] По умолчанию да.
Игнорировать SSL: [Boolean] Только для тестовых сред.

Выходные данные:
Элементы: [List<string>] Сырой JSON каждого элемента.
Страниц загружено: [Int32]
Следующий курсор: [String] Для возобновления (режим «По курсору»).";
            sdkComponentIcon = ActivityIcons.Http;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Enum<HttpPaginationMode>("Prop_Mode", "Режим пагинации"),
                PropertyBuilder.String("Prop_UrlTemplate", "Шаблон URL с {page} или {cursor}"),
                PropertyBuilder.Script<string>("Prop_ItemsPath", "JSONPath к массиву элементов"),
                PropertyBuilder.Script<string>("Prop_CursorPath", "JSONPath следующего курсора (режим «По курсору»)"),
                PropertyBuilder.Int("Prop_StartPage", "Начальная страница (по умолчанию 1)"),
                PropertyBuilder.Int("Prop_MaxPages", "Лимит страниц (по умолчанию 100)"),
                PropertyBuilder.BooleanObject("Prop_StopOnEmptyPage", "Стоп на пустой странице"),
                PropertyBuilder.BooleanObject("Prop_IgnoreSslErrors", "Игнорировать ошибки SSL"),
                PropertyBuilder.Variable<List<string>>("Prop_Items", "Элементы всех страниц"),
                PropertyBuilder.Variable<int>("Prop_PageCount", "Загружено страниц"),
                PropertyBuilder.Variable<string>("Prop_NextCursor", "Последний курсор")
            };

            InitClass(container);

            this.Prop_UrlTemplate = "\"\"";
            this.Prop_ItemsPath = "\"$..[*]\"";
            this.Prop_CursorPath = "\"$.meta.next_cursor\"";
            this.Prop_StartPage = "1";
            this.Prop_MaxPages = "100";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new HttpPaginateLogic();
            try
            {
                var urlTemplate = GetPropertyValue<string>(this.Prop_UrlTemplate, nameof(Prop_UrlTemplate), sd);
                var itemsPath = GetPropertyValue<string>(this.Prop_ItemsPath, nameof(Prop_ItemsPath), sd);
                var cursorPath = GetPropertyValue<string>(this.Prop_CursorPath, nameof(Prop_CursorPath), sd);
                var startPage = StringHelper.TryParseInt(GetPropertyValue<string>(this.Prop_StartPage, nameof(Prop_StartPage), sd)) ?? 1;
                var maxPagesRaw = StringHelper.TryParseInt(GetPropertyValue<string>(this.Prop_MaxPages, nameof(Prop_MaxPages), sd)) ?? 100;
                var maxPages = maxPagesRaw <= 0 ? 100 : maxPagesRaw;
                var ignoreSsl = this.Prop_IgnoreSslErrors;

                var result = logic.Paginate(
                    url => FetchPage(url, ignoreSsl),
                    this.Prop_Mode,
                    urlTemplate,
                    itemsPath,
                    cursorPath,
                    startPage,
                    maxPages,
                    this.Prop_StopOnEmptyPage);

                SetVariableValue(this.Prop_Items, result.Items, sd);
                SetVariableValue(this.Prop_PageCount, result.PagesFetched, sd);
                SetVariableValue(this.Prop_NextCursor, result.NextCursor, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[HTTP: Пагинация] {result.Items.Count} элементов с {result.PagesFetched} страниц ({result.StopReason})"
                };
            }
            catch (InvalidOperationException ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка HTTP: Пагинация — {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Одна загрузка страницы: JSON или null (ошибка обрабатывается логикой).
        /// </summary>
        private string FetchPage(string url, bool ignoreSsl)
        {
            using (var client = HttpClientPool.CreateClient(false, null, null, ignoreSsl, TimeSpan.FromSeconds(60)))
            using (var response = client.GetAsync(url).GetAwaiter().GetResult())
            using (var content = response.Content)
            {
                return content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_UrlTemplate, ActivityStrings.Field_PaginateUrlTemplate, ActivityStrings.Error_PaginateUrlRequired);
            ret.ValidateRequired(this.Prop_ItemsPath, ActivityStrings.Field_PaginateItemsPath, ActivityStrings.Error_PaginateItemsPathRequired);
            return ret;
        }
    }
}
