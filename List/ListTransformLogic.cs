using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Primo.MIA
{
    /// <summary>
    /// Чистая логика трансформации списка строк.
    /// </summary>
    public class ListTransformLogic
    {
        public List<string> Transform(
            List<string> source,
            ListTransformMode mode,
            string find = null,
            string replacement = null,
            string prefix = null,
            string suffix = null,
            int padWidth = 0,
            char padChar = ' ',
            int maxLength = 0,
            bool caseSensitive = false,
            bool preserveNullItems = true)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var transform = BuildTransform(
                mode,
                find ?? string.Empty,
                replacement ?? string.Empty,
                prefix ?? string.Empty,
                suffix ?? string.Empty,
                padWidth,
                padChar,
                maxLength,
                caseSensitive,
                preserveNullItems);

            return source.Select(transform).ToList();
        }

        private Func<string, string> BuildTransform(
            ListTransformMode mode,
            string find,
            string replacement,
            string prefix,
            string suffix,
            int padWidth,
            char padChar,
            int maxLength,
            bool caseSensitive,
            bool preserveNullItems)
        {
            Func<string, string> normalize;
            if (preserveNullItems)
            {
                normalize = s => s;
            }
            else
            {
                normalize = s => s ?? string.Empty;
            }

            switch (mode)
            {
                case ListTransformMode.ToUpper:
                    return s => normalize(s)?.ToUpper();

                case ListTransformMode.ToLower:
                    return s => normalize(s)?.ToLower();

                case ListTransformMode.Trim:
                    return s => normalize(s)?.Trim();

                case ListTransformMode.TrimStart:
                    return s => normalize(s)?.TrimStart();

                case ListTransformMode.TrimEnd:
                    return s => normalize(s)?.TrimEnd();

                case ListTransformMode.Replace:
                    if (!caseSensitive)
                    {
                        var replaceRegex = new Regex(
                            Regex.Escape(find),
                            RegexOptions.Compiled | RegexOptions.IgnoreCase);
                        return s =>
                        {
                            var value = normalize(s);
                            return value == null ? null : replaceRegex.Replace(value, replacement);
                        };
                    }

                    return s =>
                    {
                        var value = normalize(s);
                        return value == null ? null : value.Replace(find, replacement);
                    };

                case ListTransformMode.RegexReplace:
                    var regexOptions = caseSensitive
                        ? RegexOptions.Compiled
                        : RegexOptions.Compiled | RegexOptions.IgnoreCase;
                    var regex = new Regex(find, regexOptions);
                    return s =>
                    {
                        var value = normalize(s);
                        return value == null ? null : regex.Replace(value, replacement);
                    };

                case ListTransformMode.Prefix:
                    return s =>
                    {
                        var value = normalize(s);
                        return value == null ? null : prefix + value;
                    };

                case ListTransformMode.Suffix:
                    return s =>
                    {
                        var value = normalize(s);
                        return value == null ? null : value + suffix;
                    };

                case ListTransformMode.Wrap:
                    return s =>
                    {
                        var value = normalize(s);
                        return value == null ? null : prefix + value + suffix;
                    };

                case ListTransformMode.PadLeft:
                    return s =>
                    {
                        var value = normalize(s);
                        return value == null ? null : value.PadLeft(padWidth, padChar);
                    };

                case ListTransformMode.PadRight:
                    return s =>
                    {
                        var value = normalize(s);
                        return value == null ? null : value.PadRight(padWidth, padChar);
                    };

                case ListTransformMode.Truncate:
                    return s =>
                    {
                        var value = normalize(s);
                        if (value == null)
                            return null;

                        if (maxLength <= 0 || value.Length <= maxLength)
                            return value;

                        return value.Substring(0, maxLength);
                    };

                case ListTransformMode.RemoveNumbers:
                    return s =>
                    {
                        var value = normalize(s);
                        return value == null ? null : Regex.Replace(value, @"\d", string.Empty);
                    };

                case ListTransformMode.RemoveNonAlpha:
                    return s =>
                    {
                        var value = normalize(s);
                        return value == null
                            ? null
                            : Regex.Replace(value, @"[^a-zA-Zа-яА-ЯёЁ0-9 ]", string.Empty);
                    };

                default:
                    throw new InvalidOperationException($"Неизвестный режим: {mode}");
            }
        }
    }
}
