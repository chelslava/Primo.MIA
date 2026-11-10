using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests.HttpWeb
{
    /// <summary>
    /// Тесты HttpPaginateLogic (issue #32): шаблоны URL, извлечение элементов
    /// и курсора (реальный JSONPath через JsonQueryLogic), цикл пагинации
    /// с fake-fetcher (без сети) в обоих режимах, остановки, Guard.
    /// </summary>
    [Trait("Category", "Fast")]
    public class HttpPaginateLogicTests
    {
        private readonly HttpPaginateLogic _logic = new HttpPaginateLogic();

        private const string Page1Json = @"{""data"":{""items"":[{""id"":1},{""id"":2}],""next_cursor"":""abc""}}";
        private const string Page2Json = @"{""data"":{""items"":[{""id"":3}],""next_cursor"":""def""}}";
        private const string Page3EmptyJson = @"{""data"":{""items"":[],""next_cursor"":null}}";

        private Func<string, string> CursedFetcher(params string[] pages)
        {
            var queue = new Queue<string>(pages);
            return _ => queue.Dequeue();
        }

        // ── BuildPageUrl ───────────────────────────────────────────────

        [Fact(DisplayName = "URL: подстановка {page}")]
        public void BuildUrl_Page()
        {
            _logic.BuildPageUrl("https://api/x?page={page}&size=50", 3, null)
                .Should().Be("https://api/x?page=3&size=50");
        }

        [Fact(DisplayName = "URL: подстановка {cursor} с экранированием")]
        public void BuildUrl_Cursor_Escaped()
        {
            _logic.BuildPageUrl("https://api/x?c={cursor}", null, "a b/c")
                .Should().Be("https://api/x?c=a%20b%2Fc");
        }

        [Fact(DisplayName = "URL: шаблон без плейсхолдера — InvalidOperationException")]
        public void BuildUrl_NoPlaceholder_Throws()
        {
            Action act = () => _logic.BuildPageUrl("https://api/x/fixed", 1, null);
            act.Should().Throw<InvalidOperationException>().WithMessage("*плейсхолдер*");
        }

        [Fact(DisplayName = "URL: пустой шаблон — ArgumentException (Guard)")]
        public void BuildUrl_EmptyTemplate_Throws()
        {
            Action act = () => _logic.BuildPageUrl(" ", 1, null);
            act.Should().Throw<ArgumentException>();
        }

        // ── ExtractItems / ExtractCursor ───────────────────────────────

        [Fact(DisplayName = "Элементы: сырой JSON каждого элемента массива")]
        public void ExtractItems_RawJson()
        {
            var items = _logic.ExtractItems(Page1Json, "$.data.items[*]");
            items.Should().HaveCount(2);
            // Newtonsoft сериализует компактно
            items[0].Should().Be("{\"id\":1}");
            items[1].Should().Be("{\"id\":2}");
        }

        [Fact(DisplayName = "Элементы: путь без совпадений — пустой список")]
        public void ExtractItems_NoMatches_Empty()
        {
            _logic.ExtractItems(Page1Json, "$.nonexistent[*]").Should().BeEmpty();
        }

        [Fact(DisplayName = "Курсор: извлечение по JSONPath, null при отсутствии")]
        public void ExtractCursor_Basic()
        {
            _logic.ExtractCursor(Page1Json, "$.data.next_cursor").Should().Be("abc");
            _logic.ExtractCursor(Page3EmptyJson, "$.data.next_cursor").Should().BeNull();
            _logic.ExtractCursor(Page1Json, "$.missing").Should().BeNull();
        }

        // ── Пагинация: режим PageNumber ────────────────────────────────

        [Fact(DisplayName = "PageNumber: страницы 1..N собираются, остановка по пустой")]
        public void Paginate_PageNumber_Aggregates()
        {
            var requestedUrls = new List<string>();
            var pages = new Queue<string>(new[]
            {
                PageJson("1", "a"), PageJson("2", "b"), "{\"items\":[]}" // 3-я пустая
            });

            var result = _logic.Paginate(
                url => { requestedUrls.Add(url); return pages.Dequeue(); },
                HttpPaginationMode.PageNumber,
                "https://api/x?page={page}",
                "$.items[*]",
                null,
                1, 10, true);

            result.PagesFetched.Should().Be(3);
            result.Items.Should().HaveCount(2, "третья страница пустая");
            result.StopReason.Should().Contain("Пустая страница 3");
            requestedUrls.Should().ContainInOrder(
                "https://api/x?page=1", "https://api/x?page=2", "https://api/x?page=3");
        }

        [Fact(DisplayName = "PageNumber: старт со страницы 5")]
        public void Paginate_StartPage_Respected()
        {
            var requestedUrls = new List<string>();
            _logic.Paginate(
                url => { requestedUrls.Add(url); return PageJson("1", "a"); },
                HttpPaginationMode.PageNumber,
                "https://api/x?page={page}",
                "$.items[*]", null,
                5, 10, false);

            requestedUrls.First().Should().Be("https://api/x?page=5");
        }

        [Fact(DisplayName = "PageNumber: лимит страниц — остановка без ошибки")]
        public void Paginate_MaxPages_Stops()
        {
            var result = _logic.Paginate(
                url => PageJson("1", "x"),
                HttpPaginationMode.PageNumber,
                "https://api/x?page={page}",
                "$.items[*]", null,
                1, 3, false);

            result.PagesFetched.Should().Be(3);
            result.StopReason.Should().Contain("лимит 3");
        }

        // ── Пагинация: режим Cursor ────────────────────────────────────

        [Fact(DisplayName = "Cursor: цепочка курсоров до null, агрегация")]
        public void Paginate_Cursor_Chain()
        {
            var requestedUrls = new List<string>();

            var result = _logic.Paginate(
                url =>
                {
                    requestedUrls.Add(url);
                    return url.EndsWith("cursor=abc") ? Page2Json
                        : url.EndsWith("cursor=def") ? Page3EmptyJson
                        : Page1Json; // первая страница (пустой курсор)
                },
                HttpPaginationMode.Cursor,
                "https://api/x?cursor={cursor}",
                "$.data.items[*]",
                "$.data.next_cursor",
                1, 10, false);

            result.PagesFetched.Should().Be(3);
            result.Items.Should().HaveCount(3, "2 + 1 + 0 элементов");
            result.NextCursor.Should().BeNull("последний ответ без курсора");
            result.StopReason.Should().Contain("Курсор не найден");
            requestedUrls.Should().HaveCount(3);
            requestedUrls[0].Should().EndWith("cursor=", "первая страница — пустой курсор");
            requestedUrls[1].Should().EndWith("cursor=abc");
            requestedUrls[2].Should().EndWith("cursor=def");
        }

        [Fact(DisplayName = "Cursor: режим без JSONPath курсора — InvalidOperationException")]
        public void Paginate_CursorWithoutPath_Throws()
        {
            Action act = () => _logic.Paginate(
                url => "{}", HttpPaginationMode.Cursor,
                "https://api/x?cursor={cursor}", "$.items[*]",
                null, 1, 10, true);

            act.Should().Throw<InvalidOperationException>().WithMessage("*курсора*");
        }

        [Fact(DisplayName = "Cursor: сетевая ошибка на странице — остановка с причиной")]
        public void Paginate_NetworkError_StopsWithReason()
        {
            var result = _logic.Paginate(
                url => throw new InvalidOperationException("connection reset"),
                HttpPaginationMode.PageNumber,
                "https://api/x?page={page}", "$.items[*]", null,
                1, 10, true);

            result.PagesFetched.Should().Be(0);
            result.StopReason.Should().Contain("Сетевая ошибка");
        }

        [Fact(DisplayName = "Paginate: Guard — null fetch / пустые шаблоны")]
        public void Paginate_Guard()
        {
            Action noFetch = () => _logic.Paginate(null, HttpPaginationMode.PageNumber,
                "https://x?page={page}", "$.items[*]", null, 1, 1, true);
            Action noTemplate = () => _logic.Paginate(url => "{}", HttpPaginationMode.PageNumber,
                "", "$.items[*]", null, 1, 1, true);
            Action noItemsPath = () => _logic.Paginate(url => "{}", HttpPaginationMode.PageNumber,
                "https://x?page={page}", null, null, 1, 1, true);

            noFetch.Should().Throw<ArgumentException>();
            noTemplate.Should().Throw<ArgumentException>();
            noItemsPath.Should().Throw<ArgumentException>();
        }

        // ── Хелпер ─────────────────────────────────────────────────────

        private static string PageJson(string id, string value) =>
            "{\"items\":[{\"" + id + "\":\"" + value + "\"}]}";
    }
}
