// =============================================================================
// HttpPaginateLogic.cs — чистая логика пагинации REST (issue #32).
//
// Режимы: PageNumber ({page} в шаблоне URL, старт/шаг 1) и Cursor
// ({cursor} в шаблоне, следующий курсор извлекается из ответа по JSONPath).
// Остановка: пустая страница / курсор не найден / MaxPages.
// Aggregация: сырые JSON-элементы → List<string> (через JsonQueryLogic).
// Цикл — с инъекцией fetch-делегата: unit-тесты без сети.
// =============================================================================

using Newtonsoft.Json;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>Результат пагинации.</summary>
    public class HttpPaginateResult
    {
        /// <summary>Собранные элементы (сырой JSON каждого).</summary>
        public List<string> Items { get; } = new List<string>();

        /// <summary>Число загруженных страниц.</summary>
        public int PagesFetched { get; set; }

        /// <summary>Последний курсор (для возобновления в режиме Cursor).</summary>
        public string NextCursor { get; set; }

        /// <summary>Причина остановки (для сообщения результата).</summary>
        public string StopReason { get; set; }
    }

    /// <summary>
    /// Логика итератора REST-пагинации.
    /// </summary>
    public class HttpPaginateLogic
    {
        private readonly JsonQueryLogic _jsonQuery;

        public HttpPaginateLogic() : this(new JsonQueryLogic()) { }

        public HttpPaginateLogic(JsonQueryLogic jsonQuery)
        {
            _jsonQuery = jsonQuery ?? new JsonQueryLogic();
        }

        // ── Чистые части ───────────────────────────────────────────────

        /// <summary>
        /// Подставляет плейсхолдеры в шаблон URL: {page} и/или {cursor}.
        /// Пример: https://api/x?page={page}&amp;size=50
        /// </summary>
        public string BuildPageUrl(string template, int? pageNumber, string cursor)
        {
            Guard.NotNullOrWhiteSpace(template, nameof(template));

            // Шаблон обязан содержать хотя бы один плейсхолдер — иначе
            // пагинация будет запрашивать одну и ту же страницу вечно
            var hasPage = template.Contains("{page}");
            var hasCursor = template.Contains("{cursor}");
            if (!hasPage && !hasCursor)
                throw new InvalidOperationException(
                    "Шаблон URL должен содержать плейсхолдер {page} (режим «По номеру») или {cursor} (режим «По курсору»)");

            var url = template;
            if (hasPage)
                url = url.Replace("{page}", (pageNumber ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (hasCursor)
                // пустой курсор → «cursor=» (первая страница в курсорных API)
                url = url.Replace("{cursor}", Uri.EscapeDataString(cursor ?? string.Empty));

            return url;
        }

        /// <summary>
        /// Извлекает элементы страницы как сырые JSON-строки по JSONPath
        /// (обычно к массиву: $.data.items или $.items[*]).
        /// </summary>
        public List<string> ExtractItems(string pageJson, string itemsPath)
        {
            Guard.NotNullOrWhiteSpace(pageJson, nameof(pageJson));
            Guard.NotNullOrWhiteSpace(itemsPath, nameof(itemsPath));

            var query = _jsonQuery.Query(pageJson, itemsPath, false);
            if (!query.Found || !(query.Result is List<object> list))
                return new List<string>();

            return list
                .Select(SerializeItem)
                .ToList();
        }

        /// <summary>
        /// Извлекает следующий курсор по JSONPath; null — курсора нет
        /// (остановка в режиме Cursor).
        /// </summary>
        public string ExtractCursor(string pageJson, string cursorPath)
        {
            Guard.NotNullOrWhiteSpace(pageJson, nameof(pageJson));
            Guard.NotNullOrWhiteSpace(cursorPath, nameof(cursorPath));

            var query = _jsonQuery.Query(pageJson, cursorPath, true);
            if (!query.Found || query.Result == null)
                return null;

            var cursor = query.Result as string;
            if (string.IsNullOrEmpty(cursor) || cursor == "null")
                return null;

            return cursor;
        }

        private static string SerializeItem(object item)
        {
            if (item is string s)
                return s;
            return JsonConvert.SerializeObject(item);
        }

        // ── Цикл пагинации (fetch-делегат — инъекция) ──────────────────

        /// <summary>
        /// Обходит страницы, агрегируя элементы.
        /// </summary>
        /// <param name="fetch">Сетевой делегат: URL → JSON страницы (или null при ошибке).</param>
        /// <param name="mode">Режим пагинации.</param>
        /// <param name="urlTemplate">Шаблон URL с {page} или {cursor}.</param>
        /// <param name="itemsPath">JSONPath к массиву элементов.</param>
        /// <param name="cursorPath">JSONPath следующего курсора (режим Cursor).</param>
        /// <param name="startPage">Начальный номер страницы (режим PageNumber).</param>
        /// <param name="maxPages">Максимальное число страниц (защита от бесконечного цикла).</param>
        /// <param name="stopOnEmptyPage">Останавливаться на пустой странице.</param>
        public HttpPaginateResult Paginate(
            Func<string, string> fetch,
            HttpPaginationMode mode,
            string urlTemplate,
            string itemsPath,
            string cursorPath,
            int startPage,
            int maxPages,
            bool stopOnEmptyPage)
        {
            Guard.NotNull(fetch, nameof(fetch));
            Guard.NotNullOrWhiteSpace(urlTemplate, nameof(urlTemplate));
            Guard.NotNullOrWhiteSpace(itemsPath, nameof(itemsPath));

            if (mode == HttpPaginationMode.Cursor && string.IsNullOrWhiteSpace(cursorPath))
                throw new InvalidOperationException("Для режима «По курсору» задайте JSONPath курсора");

            var result = new HttpPaginateResult();
            var pageNumber = startPage <= 0 ? 1 : startPage;
            string cursor = null;

            for (var page = 1; page <= maxPages; page++)
            {
                string url;
                if (mode == HttpPaginationMode.PageNumber)
                {
                    url = BuildPageUrl(urlTemplate, pageNumber, null);
                }
                else if (page > 1 && cursor == null)
                {
                    result.StopReason = "Курсор не найден в последнем ответе";
                    break;
                }
                else
                {
                    // первая страница — без курсора, далее — с извлечённым
                    url = BuildPageUrl(urlTemplate, null, page == 1 ? null : cursor);
                }

                string pageJson;
                try
                {
                    pageJson = fetch(url);
                }
                catch (Exception ex)
                {
                    result.StopReason = $"Сетевая ошибка на странице {page}: {ex.Message}";
                    break;
                }

                if (pageJson == null)
                {
                    result.StopReason = $"Нет ответа на странице {page}";
                    break;
                }

                var items = ExtractItems(pageJson, itemsPath);
                result.Items.AddRange(items);
                result.PagesFetched = page;

                if (mode == HttpPaginationMode.Cursor)
                {
                    cursor = ExtractCursor(pageJson, cursorPath);
                    result.NextCursor = cursor;
                }

                if (stopOnEmptyPage && items.Count == 0)
                {
                    result.StopReason = $"Пустая страница {page}";
                    break;
                }

                if (mode == HttpPaginationMode.PageNumber)
                    pageNumber++;
            }

            if (result.StopReason == null)
                result.StopReason = $"Достигнут лимит {maxPages} страниц";

            return result;
        }
    }
}
