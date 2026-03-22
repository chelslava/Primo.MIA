using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Primo.MIA
{
    public class TextTemplateResult
    {
        public string Result { get; set; }

        public int ReplacedCount { get; set; }

        public List<string> MissingKeys { get; set; } = new List<string>();

        public string ErrorMessage { get; set; }

        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
    }

    public static class TextTemplateLogic
    {
        private static readonly Dictionary<TemplateSyntax, string> SyntaxPatterns =
            new Dictionary<TemplateSyntax, string>
            {
                { TemplateSyntax.DoubleBrace, @"\{\{([^}]+)\}\}" },
                { TemplateSyntax.SingleBrace, @"\{([^{}]+)\}"   },
                { TemplateSyntax.Percent,     @"%([^%]+)%"      }
            };

        public static TextTemplateResult Render(
            string template,
            Dictionary<string, string> variables,
            TemplateSyntax syntax = TemplateSyntax.DoubleBrace,
            MissingKeyBehavior missingKeyBehavior = MissingKeyBehavior.LeaveAsIs,
            bool caseSensitive = false)
        {
            if (template == null)
                return new TextTemplateResult { ErrorMessage = "Шаблон не указан" };

            if (variables == null)
                return new TextTemplateResult { ErrorMessage = "Словарь переменных не указан" };

            var missingKeys = new List<string>();
            int replacements = 0;
            string pattern = SyntaxPatterns[syntax];
            StringComparison comparison = caseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            string result = Regex.Replace(template, pattern, match =>
            {
                string fullKey = match.Groups[1].Value.Trim();
                string key = fullKey;
                string formatSpec = null;

                int colonIndex = fullKey.IndexOf(':');
                if (colonIndex > 0)
                {
                    key = fullKey.Substring(0, colonIndex).Trim();
                    formatSpec = fullKey.Substring(colonIndex + 1).Trim();
                }

                string value = variables
                    .Where(kv => string.Compare(kv.Key, key, comparison) == 0)
                    .Select(kv => kv.Value)
                    .FirstOrDefault();

                if (value != null)
                {
                    replacements++;
                    if (!string.IsNullOrEmpty(formatSpec))
                        value = ApplyFormat(value, formatSpec);

                    return value;
                }

                missingKeys.Add(key);
                return missingKeyBehavior == MissingKeyBehavior.ReplaceWithEmpty
                    ? string.Empty
                    : match.Value;
            });

            if (missingKeyBehavior == MissingKeyBehavior.ThrowError && missingKeys.Count > 0)
            {
                string keyList = string.Join(", ", missingKeys.Distinct());
                return new TextTemplateResult
                {
                    ErrorMessage = $"Незаполненные ключи: {keyList}"
                };
            }

            return new TextTemplateResult
            {
                Result = result,
                ReplacedCount = replacements,
                MissingKeys = missingKeys
            };
        }

        public static string ApplyFormat(string value, string format)
        {
            if (DateTime.TryParse(value, out DateTime dateValue))
                return dateValue.ToString(format);

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double numValue))
                return numValue.ToString(format);

            return value;
        }

        public static Encoding ParseEncoding(string encodingName)
        {
            if (string.IsNullOrWhiteSpace(encodingName))
                return Encoding.UTF8;

            try
            {
                return Encoding.GetEncoding(encodingName);
            }
            catch
            {
                return Encoding.UTF8;
            }
        }
    }
}
