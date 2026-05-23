using System;
using System.Collections.Generic;
using FluentAssertions;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.Xml
{
    public class XmlQueryLogicTests
    {
        private readonly XmlQueryLogic _logic = new XmlQueryLogic();

        private const string SampleXml =
            "<store><book id=\"1\"><title>Alpha</title><price>10</price></book>" +
            "<book id=\"2\"><title>Beta</title><price>20</price></book></store>";

        #region QuerySingleValue

        [Fact]
        public void QuerySingleValue_ExistingNode_ReturnsValue()
        {
            string result = _logic.QuerySingleValue(SampleXml, "//book[@id='1']/title", null, null);

            result.Should().Be("Alpha");
        }

        [Fact]
        public void QuerySingleValue_NonExistingNode_ReturnsNull()
        {
            string result = _logic.QuerySingleValue(SampleXml, "//book[@id='99']/title", null, null);

            result.Should().BeNull();
        }

        [Fact]
        public void QuerySingleValue_RootElement_ReturnsValue()
        {
            string xml = "<root><item>hello</item></root>";
            string result = _logic.QuerySingleValue(xml, "//item", null, null);

            result.Should().Be("hello");
        }

        [Fact]
        public void QuerySingleValue_InvalidXml_ThrowsXmlException()
        {
            Action act = () => _logic.QuerySingleValue("<broken", "//x", null, null);

            act.Should().Throw<System.Xml.XmlException>();
        }

        #endregion

        #region QueryList

        [Fact]
        public void QueryList_MultipleMatches_ReturnsAll()
        {
            var result = _logic.QueryList(SampleXml, "//book/title", null, null);

            result.Should().HaveCount(2);
            result.Should().Contain("Alpha");
            result.Should().Contain("Beta");
        }

        [Fact]
        public void QueryList_NoMatches_ReturnsEmptyList()
        {
            var result = _logic.QueryList(SampleXml, "//magazine", null, null);

            result.Should().BeEmpty();
        }

        [Fact]
        public void QueryList_SingleMatch_ReturnsSingleItem()
        {
            var result = _logic.QueryList(SampleXml, "//book[@id='2']/price", null, null);

            result.Should().HaveCount(1);
            result[0].Should().Be("20");
        }

        #endregion

        #region QueryCount

        [Fact]
        public void QueryCount_MultipleMatches_ReturnsCorrectCount()
        {
            int count = _logic.QueryCount(SampleXml, "//book", null, null);

            count.Should().Be(2);
        }

        [Fact]
        public void QueryCount_NoMatches_ReturnsZero()
        {
            int count = _logic.QueryCount(SampleXml, "//magazine", null, null);

            count.Should().Be(0);
        }

        [Fact]
        public void QueryCount_AllPrices_ReturnsCorrectCount()
        {
            int count = _logic.QueryCount(SampleXml, "//price", null, null);

            count.Should().Be(2);
        }

        #endregion

        #region Namespace Support

        [Fact]
        public void QuerySingleValue_WithNamespace_ReturnsValue()
        {
            string nsXml = "<root xmlns:ns=\"http://example.com\"><ns:item>val</ns:item></root>";
            string result = _logic.QuerySingleValue(nsXml, "//ns:item", "ns", "http://example.com");

            result.Should().Be("val");
        }

        [Fact]
        public void QueryCount_WithNamespace_ReturnsCount()
        {
            string nsXml = "<root xmlns:ns=\"http://example.com\"><ns:item>1</ns:item><ns:item>2</ns:item></root>";
            int count = _logic.QueryCount(nsXml, "//ns:item", "ns", "http://example.com");

            count.Should().Be(2);
        }

        #endregion
    }
}
