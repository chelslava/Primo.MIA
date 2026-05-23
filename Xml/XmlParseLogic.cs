using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Чистая логика парсинга XML без зависимости от SDK.
    /// Используется как production-кодом, так и unit-тестами.
    /// </summary>
    public class XmlParseLogic
    {
        public Dictionary<string, object> ParseToStructure(string xml)
        {
            var doc = XDocument.Parse(xml);
            return ElementToDictionary(doc.Root);
        }

        public bool ValidateXml(string xml, out string error, out string rootElementName)
        {
            try
            {
                var doc = XDocument.Parse(xml);
                error = string.Empty;
                rootElementName = doc.Root?.Name.LocalName ?? string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                rootElementName = string.Empty;
                return false;
            }
        }

        public string FormatXml(string xml)
        {
            var doc = XDocument.Parse(xml);
            return doc.ToString(SaveOptions.None);
        }

        public Dictionary<string, object> ElementToDictionary(XElement element)
        {
            var result = new Dictionary<string, object>();

            foreach (var attr in element.Attributes())
                result[$"@{attr.Name.LocalName}"] = attr.Value;

            var childGroups = new Dictionary<string, List<object>>();

            foreach (var child in element.Elements())
            {
                string name = child.Name.LocalName;
                if (!childGroups.ContainsKey(name))
                    childGroups[name] = new List<object>();

                if (!child.HasElements && !child.HasAttributes)
                    childGroups[name].Add(child.Value);
                else
                    childGroups[name].Add(ElementToDictionary(child));
            }

            foreach (var group in childGroups)
            {
                result[group.Key] = group.Value.Count == 1
                    ? group.Value[0]
                    : (object)group.Value;
            }

            if (!string.IsNullOrWhiteSpace(element.Value) && !element.HasElements)
                result["#text"] = element.Value.Trim();

            return result;
        }
    }
}
