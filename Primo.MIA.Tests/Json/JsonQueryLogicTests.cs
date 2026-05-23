using System;
using System.Collections.Generic;
using FluentAssertions;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.Json
{
    public class JsonQueryLogicTests
    {
        private readonly JsonQueryLogic _logic = new JsonQueryLogic();

        private const string SampleJson = "{\"store\":{\"name\":\"Books\",\"items\":[{\"title\":\"A\",\"price\":10},{\"title\":\"B\",\"price\":20}]}}";

        #region Query — returnFirst = false (list mode)

        [Fact]
        public void Query_MatchingPath_ReturnsFoundTrue()
        {
            var result = _logic.Query(SampleJson, "$.store.name", false);

            result.Found.Should().BeTrue();
            result.Count.Should().Be(1);
        }

        [Fact]
        public void Query_ArrayPath_ReturnsAllMatches()
        {
            var result = _logic.Query(SampleJson, "$.store.items[*].title", false);

            result.Found.Should().BeTrue();
            result.Count.Should().Be(2);
            result.Result.Should().BeOfType<List<object>>();
            var list = (List<object>)result.Result;
            list.Should().Contain("A");
            list.Should().Contain("B");
        }

        [Fact]
        public void Query_NoMatch_ReturnsFoundFalse()
        {
            var result = _logic.Query(SampleJson, "$.nonexistent", false);

            result.Found.Should().BeFalse();
            result.Count.Should().Be(0);
            result.Result.Should().BeNull();
        }

        [Fact]
        public void Query_WildcardPath_ReturnsCount()
        {
            var result = _logic.Query(SampleJson, "$.store.items[*].price", false);

            result.Count.Should().Be(2);
        }

        #endregion

        #region Query — returnFirst = true

        [Fact]
        public void Query_ReturnFirstTrue_ReturnsSingleValue()
        {
            var result = _logic.Query(SampleJson, "$.store.items[*].title", true);

            result.Found.Should().BeTrue();
            result.Count.Should().Be(2);
            result.Result.Should().Be("A");
        }

        [Fact]
        public void Query_ReturnFirstNoMatch_ReturnsFoundFalse()
        {
            var result = _logic.Query(SampleJson, "$.nothing", true);

            result.Found.Should().BeFalse();
            result.Result.Should().BeNull();
        }

        #endregion

        #region Validation

        [Fact]
        public void Query_InvalidJson_ThrowsArgumentException()
        {
            Action act = () => _logic.Query("{invalid}", "$.key", false);

            act.Should().Throw<ArgumentException>().WithMessage("*JSON*");
        }

        [Fact]
        public void Query_EmptyJson_ThrowsArgumentException()
        {
            Action act = () => _logic.Query("", "$.key", false);

            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region Nested structures

        [Fact]
        public void Query_NestedObject_ReturnsDictionary()
        {
            var result = _logic.Query(SampleJson, "$.store", false);

            result.Found.Should().BeTrue();
            var list = (List<object>)result.Result;
            list[0].Should().BeOfType<System.Collections.Generic.Dictionary<string, object>>();
        }

        #endregion
    }
}
