using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Primo.MIA
{
    /// <summary>
    /// Чистая логика XPath-запросов без зависимости от SDK.
    /// Используется как production-кодом, так и unit-тестами.
    /// </summary>
    public class XmlQueryLogic
    {
        public string QuerySingleValue(string xml, string xpath, string nsPrefix, string nsUri)
        {
            var navigator = CreateNavigator(xml);
            var nsManager = BuildNsManager(navigator, nsPrefix, nsUri);
            var node = nsManager != null
                ? navigator.SelectSingleNode(xpath, nsManager)
                : navigator.SelectSingleNode(xpath);
            return node?.Value;
        }

        public List<string> QueryList(string xml, string xpath, string nsPrefix, string nsUri)
        {
            var navigator = CreateNavigator(xml);
            var nsManager = BuildNsManager(navigator, nsPrefix, nsUri);
            var iterator = nsManager != null
                ? navigator.Select(xpath, nsManager)
                : navigator.Select(xpath);

            var results = new List<string>();
            while (iterator.MoveNext())
                results.Add(iterator.Current.Value);
            return results;
        }

        public int QueryCount(string xml, string xpath, string nsPrefix, string nsUri)
        {
            var navigator = CreateNavigator(xml);
            var nsManager = BuildNsManager(navigator, nsPrefix, nsUri);
            var iterator = nsManager != null
                ? navigator.Select(xpath, nsManager)
                : navigator.Select(xpath);
            return iterator.Count;
        }

        private static XPathNavigator CreateNavigator(string xml)
        {
            return XDocument.Parse(xml).CreateNavigator();
        }

        private static XmlNamespaceManager BuildNsManager(XPathNavigator navigator, string nsPrefix, string nsUri)
        {
            if (string.IsNullOrWhiteSpace(nsPrefix) || string.IsNullOrWhiteSpace(nsUri))
                return null;
            var mgr = new XmlNamespaceManager(navigator.NameTable);
            mgr.AddNamespace(nsPrefix, nsUri);
            return mgr;
        }
    }
}
