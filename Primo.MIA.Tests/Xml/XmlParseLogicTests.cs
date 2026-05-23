using System;
using System.Collections.Generic;
using FluentAssertions;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.Xml
{
    public class XmlParseLogicTests
    {
        private readonly XmlParseLogic _logic = new XmlParseLogic();

        #region ParseToStructure

        [Fact]
        public void ParseToStructure_SimpleElement_ReturnsDictionary()
        {
            var result = _logic.ParseToStructure("<root><name>Alice</name></root>");

            result.Should().ContainKey("name");
            result["name"].Should().Be("Alice");
        }

        [Fact]
        public void ParseToStructure_WithAttributes_IncludesAtPrefix()
        {
            var result = _logic.ParseToStructure("<root id=\"1\"><name>Bob</name></root>");

            result.Should().ContainKey("@id");
            result["@id"].Should().Be("1");
            result["name"].Should().Be("Bob");
        }

        [Fact]
        public void ParseToStructure_NestedElements_ReturnsNestedDictionary()
        {
            var result = _logic.ParseToStructure("<root><person><name>Carol</name><age>25</age></person></root>");

            result["person"].Should().BeOfType<Dictionary<string, object>>();
            var person = (Dictionary<string, object>)result["person"];
            person["name"].Should().Be("Carol");
            person["age"].Should().Be("25");
        }

        [Fact]
        public void ParseToStructure_RepeatingElements_ReturnsList()
        {
            var result = _logic.ParseToStructure("<root><item>A</item><item>B</item><item>C</item></root>");

            result["item"].Should().BeOfType<List<object>>();
            var items = (List<object>)result["item"];
            items.Should().HaveCount(3);
            items.Should().Contain("A");
        }

        [Fact]
        public void ParseToStructure_SingleChildElement_ReturnsValueNotList()
        {
            var result = _logic.ParseToStructure("<root><item>X</item></root>");

            result["item"].Should().Be("X");
        }

        [Fact]
        public void ParseToStructure_InvalidXml_ThrowsXmlException()
        {
            Action act = () => _logic.ParseToStructure("<root><unclosed>");

            act.Should().Throw<System.Xml.XmlException>();
        }

        #endregion

        #region ValidateXml

        [Fact]
        public void ValidateXml_ValidXml_ReturnsTrueAndRootName()
        {
            string error, rootName;
            bool isValid = _logic.ValidateXml("<catalog><book>A</book></catalog>", out error, out rootName);

            isValid.Should().BeTrue();
            error.Should().BeEmpty();
            rootName.Should().Be("catalog");
        }

        [Fact]
        public void ValidateXml_InvalidXml_ReturnsFalseWithError()
        {
            string error, rootName;
            bool isValid = _logic.ValidateXml("<root><unclosed>", out error, out rootName);

            isValid.Should().BeFalse();
            error.Should().NotBeEmpty();
            rootName.Should().BeEmpty();
        }

        [Fact]
        public void ValidateXml_EmptyString_ReturnsFalse()
        {
            string error, rootName;
            bool isValid = _logic.ValidateXml("", out error, out rootName);

            isValid.Should().BeFalse();
        }

        [Fact]
        public void ValidateXml_WellFormedXml_ReturnsCorrectRootName()
        {
            string error, rootName;
            _logic.ValidateXml("<persons><person/></persons>", out error, out rootName);

            rootName.Should().Be("persons");
        }

        #endregion

        #region FormatXml

        [Fact]
        public void FormatXml_CompactXml_ReturnsIndented()
        {
            string result = _logic.FormatXml("<root><child>val</child></root>");

            result.Should().Contain("\n");
            result.Should().Contain("<child>");
        }

        [Fact]
        public void FormatXml_AlreadyFormatted_ReturnsEquivalentXml()
        {
            string xml = "<root><child>val</child></root>";
            string result = _logic.FormatXml(xml);

            result.Should().Contain("val");
            result.Should().Contain("root");
        }

        [Fact]
        public void FormatXml_InvalidXml_ThrowsXmlException()
        {
            Action act = () => _logic.FormatXml("<broken");

            act.Should().Throw<System.Xml.XmlException>();
        }

        #endregion

        #region ElementToDictionary

        [Fact]
        public void ElementToDictionary_TextOnlyElement_ReturnsHashText()
        {
            var doc = System.Xml.Linq.XDocument.Parse("<node>hello</node>");
            var result = _logic.ElementToDictionary(doc.Root);

            result.Should().ContainKey("#text");
            result["#text"].Should().Be("hello");
        }

        [Fact]
        public void ElementToDictionary_MultipleAttributes_AllPrefixed()
        {
            var doc = System.Xml.Linq.XDocument.Parse("<node a=\"1\" b=\"2\"/>");
            var result = _logic.ElementToDictionary(doc.Root);

            result.Should().ContainKey("@a");
            result.Should().ContainKey("@b");
        }

        #endregion
    }
}
