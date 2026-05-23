using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tomlyn;
using Tomlyn.Model;

namespace Primo.MIA
{
    public class ReadTomlConfigLogic
    {
        public (bool found, string value) ReadSingleValue(
            string filePath,
            string keyPath,
            string defaultValue = "",
            string encodingName = "UTF-8")
        {
            ValidateFilePath(filePath);

            if (string.IsNullOrWhiteSpace(keyPath))
                throw new ArgumentException("Ключ не может быть пустым");

            TomlTable rootTable = ParseTomlFile(filePath, encodingName);
            string[] keyParts = keyPath.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            if (TryGetNestedValue(rootTable, keyParts, out string value))
                return (true, value);

            return (false, defaultValue);
        }

        public Dictionary<string, string> ReadSectionToDictionary(
            string filePath,
            string sectionName,
            string encodingName = "UTF-8")
        {
            ValidateFilePath(filePath);

            if (string.IsNullOrWhiteSpace(sectionName))
                throw new ArgumentException("Имя секции не может быть пустым");

            TomlTable rootTable = ParseTomlFile(filePath, encodingName);
            string[] parts = sectionName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            TomlTable section = FindNestedSection(rootTable, parts);

            if (section == null)
                return new Dictionary<string, string>();

            return section
                .Where(pair => !(pair.Value is TomlTable) && !(pair.Value is TomlTableArray))
                .ToDictionary(pair => pair.Key, pair => TomlValueToString(pair.Value));
        }

        public Dictionary<string, string> ReadFullFileToDictionary(
            string filePath,
            string encodingName = "UTF-8")
        {
            ValidateFilePath(filePath);

            TomlTable rootTable = ParseTomlFile(filePath, encodingName);
            var result = new Dictionary<string, string>();
            FlattenTomlTable(rootTable, string.Empty, result);
            return result;
        }

        public (Dictionary<string, string> merged, int fromProfile, int fromDefault, List<string> availableProfiles) ReadProfile(
            string filePath,
            string profileName,
            string defaultProfileName = "default",
            ProfileMergeStrategy strategy = ProfileMergeStrategy.DefaultThenProfile,
            bool includeNestedSections = true,
            string encodingName = "UTF-8")
        {
            ValidateFilePath(filePath);

            if (string.IsNullOrWhiteSpace(profileName))
                throw new ArgumentException("Имя профиля не может быть пустым");

            if (string.IsNullOrWhiteSpace(defaultProfileName))
                defaultProfileName = "default";

            TomlTable rootTable = ParseTomlFile(filePath, encodingName);
            List<string> availableProfiles = rootTable.Keys
                .Where(key => rootTable[key] is TomlTable)
                .OrderBy(key => key)
                .ToList();

            if (!rootTable.ContainsKey(profileName) || !(rootTable[profileName] is TomlTable))
                throw new KeyNotFoundException($"Профиль '[{profileName}]' не найден");

            var profileTable = rootTable[profileName] as TomlTable;
            var profileFlat = new Dictionary<string, string>();
            if (includeNestedSections)
                FlattenTomlTable(profileTable, string.Empty, profileFlat);
            else
                FlattenShallowTomlTable(profileTable, profileFlat);

            if (strategy == ProfileMergeStrategy.ProfileOnly)
                return (profileFlat, profileFlat.Count, 0, availableProfiles);

            var defaultFlat = new Dictionary<string, string>();
            if (rootTable.ContainsKey(defaultProfileName) && rootTable[defaultProfileName] is TomlTable defaultTable)
            {
                if (includeNestedSections)
                    FlattenTomlTable(defaultTable, string.Empty, defaultFlat);
                else
                    FlattenShallowTomlTable(defaultTable, defaultFlat);
            }

            var (merged, fromProfile, fromDefault) =
                MergeProfileDictionaries(defaultFlat, profileFlat, strategy);
            return (merged, fromProfile, fromDefault, availableProfiles);
        }

        private static void ValidateFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"TOML-файл не найден: {filePath}");
        }

        private static TomlTable ParseTomlFile(string filePath, string encodingName)
        {
            string tomlContent = File.ReadAllText(filePath, ResolveEncoding(encodingName));
            return TomlSerializer.Deserialize<TomlTable>(tomlContent, default(TomlSerializerOptions));
        }

        private static Encoding ResolveEncoding(string encodingName)
        {
            try
            {
                return Encoding.GetEncoding(encodingName);
            }
            catch
            {
                return Encoding.UTF8;
            }
        }

        private static bool TryGetNestedValue(TomlTable table, string[] keyParts, out string value)
        {
            value = string.Empty;
            if (keyParts == null || keyParts.Length == 0)
                return false;

            TomlTable targetTable = keyParts
                .Take(keyParts.Length - 1)
                .Aggregate(table, (current, part) =>
                    current != null && current.ContainsKey(part)
                        ? current[part] as TomlTable
                        : null);

            TomlTable searchIn = keyParts.Length == 1 ? table : targetTable;
            string finalKey = keyParts[keyParts.Length - 1];

            if (searchIn == null || !searchIn.ContainsKey(finalKey))
                return false;

            value = TomlValueToString(searchIn[finalKey]);
            return true;
        }

        private static TomlTable FindNestedSection(TomlTable rootTable, string[] sectionParts)
        {
            return sectionParts.Aggregate(rootTable, (current, part) =>
                current != null && current.ContainsKey(part)
                    ? current[part] as TomlTable
                    : null);
        }

        private static void FlattenTomlTable(TomlTable table, string prefix, Dictionary<string, string> result)
        {
            foreach (var pair in table)
            {
                string fullKey = string.IsNullOrEmpty(prefix) ? pair.Key : $"{prefix}.{pair.Key}";

                switch (pair.Value)
                {
                    case TomlTable nestedTable:
                        FlattenTomlTable(nestedTable, fullKey, result);
                        break;

                    case TomlTableArray tableArray:
                        foreach (var indexedItem in tableArray.Select((t, i) => new { t, i }))
                            FlattenTomlTable(indexedItem.t, $"{fullKey}[{indexedItem.i}]", result);
                        break;

                    default:
                        result[fullKey] = TomlValueToString(pair.Value);
                        break;
                }
            }
        }

        private static void FlattenShallowTomlTable(TomlTable table, Dictionary<string, string> result)
        {
            table
                .Where(pair => !(pair.Value is TomlTable) && !(pair.Value is TomlTableArray))
                .ToList()
                .ForEach(pair => result[pair.Key] = TomlValueToString(pair.Value));
        }

        private static (Dictionary<string, string> merged, int fromProfile, int fromDefault) MergeProfileDictionaries(
            Dictionary<string, string> defaultDict,
            Dictionary<string, string> profileDict,
            ProfileMergeStrategy strategy)
        {
            var merged = new Dictionary<string, string>();

            if (strategy == ProfileMergeStrategy.DefaultThenProfile)
            {
                foreach (var pair in defaultDict)
                    merged[pair.Key] = pair.Value;

                foreach (var pair in profileDict)
                    merged[pair.Key] = pair.Value;

                int fromDefault = defaultDict.Keys.Count(key => !profileDict.ContainsKey(key));
                return (merged, profileDict.Count, fromDefault);
            }

            foreach (var pair in profileDict)
                merged[pair.Key] = pair.Value;

            var addedFromDefault = defaultDict
                .Where(pair => !merged.ContainsKey(pair.Key))
                .ToList();

            foreach (var pair in addedFromDefault)
                merged[pair.Key] = pair.Value;

            return (merged, profileDict.Count, addedFromDefault.Count);
        }

        private static string TomlValueToString(object value)
        {
            if (value == null)
                return string.Empty;

            switch (value)
            {
                case string s:
                    return s;
                case bool b:
                    return b.ToString().ToLower();
                case long l:
                    return l.ToString();
                case double d:
                    return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case DateTime dt:
                    return dt.ToString("O");
                case TomlArray array:
                    return "[" + string.Join(", ", array.Select(item => TomlValueToString(item))) + "]";
                case TomlTable table:
                    return "{" + string.Join(", ", table.Select(pair => $"{pair.Key}={TomlValueToString(pair.Value)}")) + "}";
                default:
                    return value.ToString() ?? string.Empty;
            }
        }
    }
}
