using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Primo.MIA
{
    public class TextParseResult
    {
        public bool IsMatched { get; set; }

        public Dictionary<string, string> Result { get; set; } = new Dictionary<string, string>();

        public int Count { get; set; }

        public string CompiledPattern { get; set; }

        public List<Dictionary<string, string>> AllResults { get; set; } = new List<Dictionary<string, string>>();

        public string ErrorMessage { get; set; }

        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
    }

    public static class TextParseLogic
    {
        private static readonly Dictionary<TemplateSyntax, string> PlaceholderPatterns =
            new Dictionary<TemplateSyntax, string>
            {
                { TemplateSyntax.DoubleBrace, @"\{\{([^}]+)\}\}" },
                { TemplateSyntax.SingleBrace, @"\{([^{}]+)\}" },
                { TemplateSyntax.Percent, @"%([^%]+)%" }
            };

        public static TextParseResult Parse(
            string inputText,
            string mask,
            TemplateSyntax syntax = TemplateSyntax.SingleBrace,
            bool caseSensitive = false,
            bool greedy = false,
            bool multiLine = false,
            bool allMatches = false)
        {
            if (inputText == null)
                return new TextParseResult { ErrorMessage = "Входная строка не указана" };

            if (string.IsNullOrEmpty(mask))
                return new TextParseResult { ErrorMessage = "Маска не указана" };

            string compiledPattern;
            try
            {
                compiledPattern = CompileMask(mask, syntax, greedy, allMatches);
            }
            catch (Exception ex)
            {
                return new TextParseResult { ErrorMessage = $"Ошибка компиляции маски: {ex.Message}" };
            }

            RegexOptions options = RegexOptions.Compiled;
            if (!caseSensitive)
                options |= RegexOptions.IgnoreCase;
            if (multiLine)
                options |= RegexOptions.Singleline;

            Regex regex;
            try
            {
                regex = new Regex(compiledPattern, options);
            }
            catch (Exception ex)
            {
                return new TextParseResult { ErrorMessage = $"Ошибка создания regex: {ex.Message}" };
            }

            var result = new TextParseResult { CompiledPattern = compiledPattern };

            if (allMatches)
            {
                MatchCollection matches = regex.Matches(inputText);
                result.AllResults = matches
                    .Cast<Match>()
                    .Select(m => ExtractNamedGroups(regex, m))
                    .ToList();

                result.Result = result.AllResults.Count > 0
                    ? result.AllResults[0]
                    : new Dictionary<string, string>();
                result.IsMatched = result.AllResults.Count > 0;
                result.Count = result.AllResults.Count;
            }
            else
            {
                Match match = regex.Match(inputText);
                result.IsMatched = match.Success;

                if (match.Success)
                {
                    result.Result = ExtractNamedGroups(regex, match);
                    result.Count = result.Result.Count;
                }
            }

            return result;
        }

        public static string CompileMask(string mask, TemplateSyntax syntax, bool greedy, bool allMatches = false)
        {
            string placeholderPattern = PlaceholderPatterns[syntax];
            string quantifier = greedy ? ".+" : ".+?";
            var sb = new StringBuilder();
            int lastIndex = 0;
            bool hasPlaceholders = false;

            if (!allMatches)
                sb.Append("^");

            foreach (Match match in Regex.Matches(mask, placeholderPattern))
            {
                hasPlaceholders = true;

                string literal = mask.Substring(lastIndex, match.Index - lastIndex);
                if (!string.IsNullOrEmpty(literal))
                    sb.Append(Regex.Escape(literal));

                string groupName = match.Groups[1].Value.Trim();
                if (!IsValidGroupName(groupName))
                {
                    throw new ArgumentException(
                        $"Недопустимое имя плейсхолдера «{groupName}». " +
                        "Имя должно начинаться с буквы или _, содержать только буквы, цифры и _");
                }

                sb.Append($"(?<{groupName}>{quantifier})");
                lastIndex = match.Index + match.Length;
            }

            string tail = mask.Substring(lastIndex);
            if (!string.IsNullOrEmpty(tail))
                sb.Append(Regex.Escape(tail));

            if (!allMatches)
                sb.Append("$");

            if (!hasPlaceholders)
            {
                throw new ArgumentException(
                    "Маска не содержит ни одного плейсхолдера. " +
                    "Добавьте хотя бы один плейсхолдер, например {Значение}");
            }

            return sb.ToString();
        }

        public static bool IsValidGroupName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            if (!char.IsLetter(name[0]) && name[0] != '_')
                return false;

            return name.All(c => char.IsLetterOrDigit(c) || c == '_');
        }

        private static Dictionary<string, string> ExtractNamedGroups(Regex regex, Match match) =>
            regex.GetGroupNames()
                .Where(name => !int.TryParse(name, out _))
                .ToDictionary(
                    name => name,
                    name => match.Groups[name].Value);
    }
}
