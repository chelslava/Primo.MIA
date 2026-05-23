using System;
using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.Json
{
    public class JsonParseLogicTests
    {
        private readonly JsonParseLogic _logic = new JsonParseLogic();

        #region IsValidJson

        [Fact]
        public void IsValidJson_ValidObject_ReturnsTrue()
        {
            _logic.IsValidJson("{\"key\":\"value\"}").Should().BeTrue();
        }

        [Fact]
        public void IsValidJson_ValidArray_ReturnsTrue()
        {
            _logic.IsValidJson("[1,2,3]").Should().BeTrue();
        }

        [Fact]
        public void IsValidJson_InvalidJson_ReturnsFalse()
        {
            _logic.IsValidJson("{invalid}").Should().BeFalse();
        }

        [Fact]
        public void IsValidJson_EmptyString_ReturnsFalse()
        {
            _logic.IsValidJson("").Should().BeFalse();
        }

        #endregion

        #region ParseToDictionary

        [Fact]
        public void ParseToDictionary_SimpleObject_ReturnsDictionary()
        {
            var result = _logic.ParseToDictionary("{\"name\":\"Alice\",\"age\":30}");

            result.Should().ContainKey("name").WhoseValue.Should().Be("Alice");
            result.Should().ContainKey("age").WhoseValue.Should().Be(30L);
        }

        [Fact]
        public void ParseToDictionary_NestedObject_ReturnsNestedDictionary()
        {
            var result = _logic.ParseToDictionary("{\"person\":{\"name\":\"Bob\"}}");

            result["person"].Should().BeOfType<Dictionary<string, object>>();
            var person = (Dictionary<string, object>)result["person"];
            person["name"].Should().Be("Bob");
        }

        [Fact]
        public void ParseToDictionary_ArrayValue_ReturnsList()
        {
            var result = _logic.ParseToDictionary("{\"items\":[1,2,3]}");

            result["items"].Should().BeOfType<List<object>>();
            var items = (List<object>)result["items"];
            items.Should().HaveCount(3);
        }

        [Fact]
        public void ParseToDictionary_NullValue_ReturnsNullEntry()
        {
            var result = _logic.ParseToDictionary("{\"key\":null}");

            result.Should().ContainKey("key");
            result["key"].Should().BeNull();
        }

        [Fact]
        public void ParseToDictionary_BooleanValues_ReturnsBooleans()
        {
            var result = _logic.ParseToDictionary("{\"active\":true,\"deleted\":false}");

            result["active"].Should().Be(true);
            result["deleted"].Should().Be(false);
        }

        [Fact]
        public void ParseToDictionary_FloatValue_ReturnsDouble()
        {
            var result = _logic.ParseToDictionary("{\"price\":3.14}");

            result["price"].Should().Be(3.14);
        }

        #endregion

        #region ParseToList

        [Fact]
        public void ParseToList_SimpleArray_ReturnsList()
        {
            var result = _logic.ParseToList("[\"a\",\"b\",\"c\"]");

            result.Should().HaveCount(3);
            result[0].Should().Be("a");
            result[1].Should().Be("b");
            result[2].Should().Be("c");
        }

        [Fact]
        public void ParseToList_ArrayOfObjects_ReturnsListOfDictionaries()
        {
            var result = _logic.ParseToList("[{\"id\":1},{\"id\":2}]");

            result.Should().HaveCount(2);
            result[0].Should().BeOfType<Dictionary<string, object>>();
        }

        [Fact]
        public void ParseToList_EmptyArray_ReturnsEmptyList()
        {
            var result = _logic.ParseToList("[]");

            result.Should().BeEmpty();
        }

        [Fact]
        public void ParseToList_MixedTypes_ReturnsCorrectTypes()
        {
            var result = _logic.ParseToList("[1,\"text\",true,null]");

            result[0].Should().Be(1L);
            result[1].Should().Be("text");
            result[2].Should().Be(true);
            result[3].Should().BeNull();
        }

        #endregion

        #region JTokenToObject

        [Fact]
        public void JTokenToObject_IntegerToken_ReturnsLong()
        {
            var token = JToken.Parse("42");
            _logic.JTokenToObject(token).Should().Be(42L);
        }

        [Fact]
        public void JTokenToObject_FloatToken_ReturnsDouble()
        {
            var token = JToken.Parse("3.14");
            _logic.JTokenToObject(token).Should().Be(3.14);
        }

        [Fact]
        public void JTokenToObject_BoolToken_ReturnsBool()
        {
            var token = JToken.Parse("true");
            _logic.JTokenToObject(token).Should().Be(true);
        }

        [Fact]
        public void JTokenToObject_NullToken_ReturnsNull()
        {
            var token = JToken.Parse("null");
            _logic.JTokenToObject(token).Should().BeNull();
        }

        [Fact]
        public void JTokenToObject_ObjectToken_ReturnsDictionary()
        {
            var token = JToken.Parse("{\"k\":\"v\"}");
            _logic.JTokenToObject(token).Should().BeOfType<Dictionary<string, object>>();
        }

        [Fact]
        public void JTokenToObject_ArrayToken_ReturnsList()
        {
            var token = JToken.Parse("[1,2]");
            _logic.JTokenToObject(token).Should().BeOfType<List<object>>();
        }

        #endregion

        #region FormatJson

        [Fact]
        public void FormatJson_CompactJson_ReturnsIndented()
        {
            string result = _logic.FormatJson("{\"a\":1,\"b\":2}");

            result.Should().Contain("\n");
            result.Should().Contain("\"a\"");
        }

        #endregion
    }
}
