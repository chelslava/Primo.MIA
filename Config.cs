using Hjson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Primo.MIA.Config
{
    /// <summary>
    /// Окружение выполнения приложения.
    /// </summary>
    public enum ExecutionEnvironment
    {
        /// <summary>Тестирование</summary>
        Test,
        /// <summary>Препродакшн</summary>
        Preprod,
        /// <summary>Продакшн</summary>
        Prod
    }

    /// <summary>
    /// Удобный доступ к Hjson-конфигу с поддержкой выбора значения по окружению.
    /// </summary>
    public class HjsonConfig
    {
        private const string PathSeparator = ".";
        private const string BoolTrue = "1";
        private const string BoolFalse = "0";

        private readonly JsonValue _root;
        public ExecutionEnvironment CurrentEnvironment { get; }

        private readonly HashSet<string> _allKeysCached;
        private readonly IReadOnlyList<string> _allKeysWithoutObjects;
        private readonly IReadOnlyList<string> _allKeysWithObjects;

        private HjsonConfig(JsonValue root, ExecutionEnvironment env)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            CurrentEnvironment = env;
            
            // Кешируем оба варианта ключей
            var without = new List<string>();
            CollectKeys(_root, string.Empty, without, includeObjects: false);
            _allKeysWithoutObjects = without.AsReadOnly();

            var with = new List<string>();
            CollectKeys(_root, string.Empty, with, includeObjects: true);
            _allKeysWithObjects = with.AsReadOnly();

            _allKeysCached = new HashSet<string>(with);
        }

        /// <summary>
        /// Загружает конфиг из файла.
        /// </summary>
        /// <exception cref="ArgumentException">Если путь пустой</exception>
        /// <exception cref="FileNotFoundException">Если файл не найден</exception>
        /// <exception cref="InvalidOperationException">Если Hjson невалидный</exception>
        public static HjsonConfig Load(string filePath, ExecutionEnvironment env)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл конфига не найден: {filePath}", filePath);

            try
            {
                var text = File.ReadAllText(filePath, Encoding.UTF8);
                var root = HjsonValue.Parse(text);
                return new HjsonConfig(root, env);
            }
            catch (Exception ex) when (!(ex is FileNotFoundException || ex is ArgumentException))
            {
                throw new InvalidOperationException(
                    $"Ошибка при парсинге Hjson конфига '{filePath}'. Проверьте формат файла.",
                    ex);
            }
        }

        /// <summary>
        /// Получает строковое значение по пути или выбросит исключение.
        /// </summary>
        public string GetString(string path)    
        {
            var value = GetValueInternal(path);
            return JsonValueToRawString(ExtractValue(value));
        }

        /// <summary>
        /// Получает булево значение по пути или выбросит исключение.
        /// </summary>
        public bool GetBool(string path)
        {
            var raw = JsonValueToRawString(ExtractValue(GetValueInternal(path))).Trim();

            if (bool.TryParse(raw, out var parsed))
                return parsed;
            if (raw == BoolTrue)
                return true;
            if (raw == BoolFalse)
                return false;

            throw new FormatException(
                $"Значение '{raw}' по пути '{path}' не может быть приведено к bool");
        }

        /// <summary>
        /// Пытается получить строковое значение по пути.
        /// </summary>
        public bool TryGetString(string path, out string value)
        {
            try
            {
                value = GetString(path);
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Пытается получить булево значение по пути.
        /// </summary>
        public bool TryGetBool(string path, out bool value)
        {
            try
            {
                value = GetBool(path);
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Возвращает все ключи конфигурации (точечные пути).
        /// </summary>
        public IReadOnlyList<string> GetAllKeys(bool includeObjects = false)
        {
            return includeObjects ? _allKeysWithObjects : _allKeysWithoutObjects;
        }

        /// <summary>
        /// Проверяет наличие ключа в конфиге за O(1).
        /// </summary>
        public bool ContainsKey(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            return _allKeysCached.Contains(path);
        }

        // --- Приватные методы ---

        private JsonValue GetValueInternal(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Путь не может быть пустым.", nameof(path));

            var parts = path.Split(PathSeparator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length == 0 || parts.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException($"Некорректный путь: '{path}'", nameof(path));

            var found = GetDeepJsonValue(_root, parts);
            if (found == null)
                throw new KeyNotFoundException(
                    $"Ключ '{path}' не найден. Доступные ключи: {string.Join(", ", _allKeysCached.Take(3))}...");

            return found;
        }

        private JsonValue ExtractValue(JsonValue value)
        {
            var obj = value as JsonObject;
            if (obj == null)
                return value;

            var envKey = CurrentEnvironment.ToString();
            if (obj.ContainsKey(envKey))
                return obj[envKey];

            throw new KeyNotFoundException(
                $"Объект не содержит значения для окружения '{envKey}'");
        }

        private static JsonValue GetDeepJsonValue(JsonValue current, string[] parts, int index = 0)
        {
            if (current == null) return null;
            if (index >= parts.Length) return current;
            var obj = current as JsonObject;
            if (obj == null) return null;

            var key = parts[index];
            return !obj.ContainsKey(key) ? null : GetDeepJsonValue(obj[key], parts, index + 1);
        }

        private static string JsonValueToRawString(JsonValue v)
        {
            if (v == null) return string.Empty;
            if (v is JsonObject) throw new InvalidOperationException("Ожидалось примитивное значение");

            string stringified = v.ToString(new HjsonOptions { KeepWsc = false });
            if (string.IsNullOrEmpty(stringified)) return string.Empty;

            stringified = stringified.Trim();
            stringified = RemoveHjsonComments(stringified);

            if (stringified.StartsWith("\"") && stringified.EndsWith("\"") && stringified.Length > 1)
                return UnescapeJsonString(stringified.Substring(1, stringified.Length - 2));

            return stringified;
        }

        private static string RemoveHjsonComments(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            bool inString = false;
            bool inEscape = false;

            for (int i = 0; i < input.Length; i++)
            {
                if (inString && inEscape) { inEscape = false; continue; }
                if (inString && input[i] == '\\') { inEscape = true; continue; }
                if (input[i] == '"') { inString = !inString; continue; }

                if (!inString)
                {
                    if (input[i] == '#') return input.Substring(0, i).Trim();
                    if (input[i] == '/' && i + 1 < input.Length && input[i + 1] == '/')
                        return input.Substring(0, i).Trim();
                }
            }

            return input;
        }

        private static string UnescapeJsonString(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '\\' && i + 1 < input.Length)
                {
                    sb.Append(UnescapeChar(input, ref i));
                }
                else
                {
                    sb.Append(input[i]);
                }
            }

            return sb.ToString();
        }

        private static char UnescapeChar(string input, ref int i)
        {
            char next = input[i + 1];
            switch (next)
            {
                case '"':
                    i++;
                    return '"';
                case '\\':
                    i++;
                    return '\\';
                case '/':
                    i++;
                    return '/';
                case 'b':
                    i++;
                    return '\b';
                case 'f':
                    i++;
                    return '\f';
                case 'n':
                    i++;
                    return '\n';
                case 'r':
                    i++;
                    return '\r';
                case 't':
                    i++;
                    return '\t';
                case 'u':
                    if (i + 5 < input.Length)
                    {
                        int code;
                        if (int.TryParse(input.Substring(i + 2, 4), System.Globalization.NumberStyles.HexNumber, null, out code))
                        {
                            i += 5;
                            return (char)code;
                        }
                    }
                    return input[i];
                default:
                    return input[i];
            }
        }

        private static bool IsEnvironmentObject(JsonObject obj)
        {
            return obj?.Count > 0 && obj.Keys.All(k => 
                Enum.TryParse<ExecutionEnvironment>(k, true, out _));
        }

        private static void CollectKeys(JsonValue node, string prefix, List<string> result, bool includeObjects)
        {
            if (node == null) return;

            if (node is JsonObject obj)
            {
                if (IsEnvironmentObject(obj))
                {
                    if (!string.IsNullOrEmpty(prefix))
                        result.Add(prefix);
                    return;
                }

                if (includeObjects && !string.IsNullOrEmpty(prefix))
                    result.Add(prefix);

                foreach (var kv in obj)
                {
                    string childPrefix = string.IsNullOrEmpty(prefix) 
                        ? kv.Key 
                        : $"{prefix}{PathSeparator}{kv.Key}";
                    CollectKeys(kv.Value, childPrefix, result, includeObjects);
                }
            }
            else if (!string.IsNullOrEmpty(prefix))
            {
                result.Add(prefix);
            }
        }
    }
}