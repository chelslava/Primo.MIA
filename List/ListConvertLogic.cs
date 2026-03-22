using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Primo.MIA
{
    /// <summary>
    /// Бизнес-логика конвертации списков.
    /// </summary>
    public class ListConvertLogic
    {
        public object Convert(
            List<string> source,
            ListConvertMode mode,
            string separator = "=",
            string csvSeparator = ",",
            List<string> secondList = null,
            int chunkSize = 10)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            switch (mode)
            {
                case ListConvertMode.ToDict:
                    var dict = new Dictionary<string, string>();
                    foreach (var item in source.Where(s => !string.IsNullOrEmpty(s)))
                    {
                        var parts = item.Split(new[] { separator }, 2, StringSplitOptions.None);
                        if (parts.Length == 2)
                            dict[parts[0].Trim()] = parts[1].Trim();
                    }
                    return dict;

                case ListConvertMode.ToDictIndexed:
                    return source
                        .Select((value, index) => new { Key = index.ToString(), Value = value })
                        .ToDictionary(item => item.Key, item => item.Value ?? string.Empty);

                case ListConvertMode.ToCSVRow:
                    return string.Join(csvSeparator,
                        source.Select(value => $"\"{value?.Replace("\"", "\"\"")}\""));

                case ListConvertMode.FromCSVRow:
                    if (source.Count == 0) return new List<string>();
                    var csvLine = source[0] ?? string.Empty;
                    return ParseCsv(csvLine, csvSeparator);

                case ListConvertMode.ZipToDict:
                    if (secondList == null)
                    {
                        throw new ArgumentNullException(nameof(secondList), "Второй список обязателен для ZipToDict");
                    }

                    var zipDict = new Dictionary<string, string>();
                    var minLength = Math.Min(source.Count, secondList.Count);
                    for (int i = 0; i < minLength; i++)
                    {
                        zipDict[source[i] ?? $"key{i}"] = secondList[i] ?? string.Empty;
                    }
                    return zipDict;

                case ListConvertMode.Flatten:
                    return source
                        .Where(value => value != null)
                        .SelectMany(value => value.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries))
                        .Select(value => value.Trim())
                        .ToList();

                case ListConvertMode.Chunk:
                    if (chunkSize <= 0)
                        throw new ArgumentException("Размер чанка должен быть больше 0");

                    var chunks = new List<List<string>>();
                    for (int i = 0; i < source.Count; i += chunkSize)
                    {
                        chunks.Add(source.Skip(i).Take(chunkSize).ToList());
                    }
                    return chunks;

                default:
                    throw new InvalidOperationException($"Неизвестный режим: {mode}");
            }
        }

        private List<string> ParseCsv(string line, string separator)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == separator[0] && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result;
        }
    }
}
