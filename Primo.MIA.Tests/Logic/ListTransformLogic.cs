using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA.Tests.Logic
{
    /// <summary>
    /// Бизнес-логика трансформации списков
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
            int maxLength = 0)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Select(item => TransformItem(item, mode, find, replacement, 
                prefix, suffix, padWidth, padChar, maxLength)).ToList();
        }

        private string TransformItem(
            string item,
            ListTransformMode mode,
            string find,
            string replacement,
            string prefix,
            string suffix,
            int padWidth,
            char padChar,
            int maxLength)
        {
            if (item == null) return null;

            switch (mode)
            {
                case ListTransformMode.ToUpper:
                    return item.ToUpper();

                case ListTransformMode.ToLower:
                    return item.ToLower();

                case ListTransformMode.Trim:
                    return item.Trim();

                case ListTransformMode.TrimStart:
                    return item.TrimStart();

                case ListTransformMode.TrimEnd:
                    return item.TrimEnd();

                case ListTransformMode.Replace:
                    return item.Replace(find ?? "", replacement ?? "");

                case ListTransformMode.RegexReplace:
                    return System.Text.RegularExpressions.Regex.Replace(
                        item, find ?? "", replacement ?? "");

                case ListTransformMode.Prefix:
                    return (prefix ?? "") + item;

                case ListTransformMode.Suffix:
                    return item + (suffix ?? "");

                case ListTransformMode.Wrap:
                    return (prefix ?? "") + item + (suffix ?? "");

                case ListTransformMode.PadLeft:
                    return item.PadLeft(padWidth, padChar);

                case ListTransformMode.PadRight:
                    return item.PadRight(padWidth, padChar);

                case ListTransformMode.Truncate:
                    return maxLength > 0 && item.Length > maxLength 
                        ? item.Substring(0, maxLength) 
                        : item;

                case ListTransformMode.RemoveNumbers:
                    return System.Text.RegularExpressions.Regex.Replace(item, @"\d", "");

                case ListTransformMode.RemoveNonAlpha:
                    return System.Text.RegularExpressions.Regex.Replace(
                        item, @"[^a-zA-Z0-9\s]", "");

                default:
                    throw new InvalidOperationException($"Неизвестный режим: {mode}");
            }
        }
    }
}
