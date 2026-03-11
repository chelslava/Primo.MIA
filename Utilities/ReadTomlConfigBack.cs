// =============================================================================
// Активность ReadTomlConfig — универсальное чтение конфигурационных файлов TOML.
//
// ВАЖНО — совместимость:
//   Используется Tomlyn версии 0.16.2, которая поддерживает:
//     - netstandard2.0  → совместима с .NET Framework 4.7.2+
//     - net6.0          → совместима с современными рантаймами
//   Версии 0.16+ требуют .NET 8 и НЕ работают в Primo RPA на .NET Framework.
//
//   В .csproj укажите:
//     <PackageReference Include="Tomlyn" Version="0.10.1" />
//
// Отличия Tomlyn 0.10.x от 0.16.x (важно для совместимости):
//   - Toml.ToModel(string) — есть в обеих версиях, API совпадает
//   - TomlTable, TomlArray, TomlTableArray — есть в обеих, API совпадает
//   - DateTimeOffset — в 0.10.x НЕТ, вместо него Tomlyn.Syntax.TomlDateTime
//     (структура с полями DateTime и Kind: Local / LocalDate / LocalTime / Offset)
//   - В 0.10.x TomlTableArray реализует IList<TomlTable>, перебираем через foreach
//
// Поддерживаемые режимы (TomlReadMode):
//   1. SingleValue         — одно значение по ключу с вложенностью (a.b.c)
//   2. SectionToDictionary — все ключи секции → Dictionary<string, string>
//   3. FullFileToDictionary — весь файл → плоский словарь (a.b.c = value)
//   4. ReadProfile         — мёрж профилей [default] + [production/staging/...]
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tomlyn;
using Tomlyn.Model;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для чтения TOML-файлов через Tomlyn 0.9.1 (.NET Framework 4.7.2+).
    ///
    /// Четыре режима работы:
    ///   SingleValue        — одно значение по пути с вложенностью
    ///   SectionToDictionary — секция → словарь
    ///   FullFileToDictionary — весь файл → плоский словарь
    ///   ReadProfile        — мёрж профилей окружений
    /// </summary>
    public class ReadTomlConfigBack : PrimoComponentTO<ReadTomlConfig>
    {
        public override string GroupName
        {
            get => ActivityCategories.Utilities;
            protected set { }
        }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // =========================================================================
        // INPUT PROPERTIES — ОСНОВНЫЕ
        // =========================================================================

        private string _propFilePath;
        /// <summary>Путь к TOML-файлу конфигурации</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_FilePath)]
        public string Prop_FilePath
        {
            get => _propFilePath;
            set { _propFilePath = value; InvokePropertyChanged(this, "Prop_FilePath"); }
        }

        private TomlReadMode _readMode = TomlReadMode.SingleValue;
        /// <summary>Режим чтения: SingleValue / SectionToDictionary / FullFileToDictionary / ReadProfile</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_ReadMode)]
        public TomlReadMode ReadMode
        {
            get => _readMode;
            set { _readMode = value; InvokePropertyChanged(this, "ReadMode"); }
        }

        private string _propKeyPath;
        /// <summary>Путь к ключу через точку: "section.subsection.key". Обязателен в режиме SingleValue.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_KeyPath)]
        public string Prop_KeyPath
        {
            get => _propKeyPath;
            set { _propKeyPath = value; InvokePropertyChanged(this, "Prop_KeyPath"); }
        }

        private string _propSectionName;
        /// <summary>Имя секции для режима SectionToDictionary. Поддерживает вложенность: "app.database".</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SectionName)]
        public string Prop_SectionName
        {
            get => _propSectionName;
            set { _propSectionName = value; InvokePropertyChanged(this, "Prop_SectionName"); }
        }

        private string _propDefaultValue;
        /// <summary>Значение по умолчанию для режима SingleValue, если ключ не найден.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_DefaultValue)]
        public string Prop_DefaultValue
        {
            get => _propDefaultValue;
            set { _propDefaultValue = value; InvokePropertyChanged(this, "Prop_DefaultValue"); }
        }

        private bool _throwIfKeyNotFound = false;
        /// <summary>Выбросить исключение если ключ/секция/профиль не найдены. Если false — вернуть DefaultValue или пустой словарь.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_ThrowIfNotFound)]
        public bool Prop_ThrowIfKeyNotFound
        {
            get => _throwIfKeyNotFound;
            set { _throwIfKeyNotFound = value; InvokePropertyChanged(this, "Prop_ThrowIfKeyNotFound"); }
        }

        private string _propEncoding;
        /// <summary>Кодировка файла. По умолчанию UTF-8.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_File), System.ComponentModel.DisplayName(ActivityStrings.Field_Encoding)]
        public string Prop_Encoding
        {
            get => _propEncoding;
            set { _propEncoding = value; InvokePropertyChanged(this, "Prop_Encoding"); }
        }

        // =========================================================================
        // INPUT PROPERTIES — ПРОФИЛЬ
        // =========================================================================

        private string _propProfileName;
        /// <summary>
        /// Имя профиля окружения: "production", "staging", "development" и т.д.
        /// Обязателен в режиме ReadProfile.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Profile), System.ComponentModel.DisplayName(ActivityStrings.Field_ProfileName)]
        public string Prop_ProfileName
        {
            get => _propProfileName;
            set { _propProfileName = value; InvokePropertyChanged(this, "Prop_ProfileName"); }
        }

        private string _propDefaultProfileName;
        /// <summary>
        /// Имя базовой секции. По умолчанию "default".
        /// При стратегии ProfileOnly секция default не читается.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Profile), System.ComponentModel.DisplayName(ActivityStrings.Field_DefaultProfileName)]
        public string Prop_DefaultProfileName
        {
            get => _propDefaultProfileName;
            set { _propDefaultProfileName = value; InvokePropertyChanged(this, "Prop_DefaultProfileName"); }
        }

        private ProfileMergeStrategy _mergeStrategy = ProfileMergeStrategy.DefaultThenProfile;
        /// <summary>Стратегия слияния: DefaultThenProfile / ProfileOnly / ProfileThenDefault</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Profile), System.ComponentModel.DisplayName(ActivityStrings.Field_MergeStrategy)]
        public ProfileMergeStrategy MergeStrategy
        {
            get => _mergeStrategy;
            set { _mergeStrategy = value; InvokePropertyChanged(this, "MergeStrategy"); }
        }

        private bool _includeNestedSections = true;
        /// <summary>
        /// Если true — вложенные секции [production.database] тоже мёржатся рекурсивно.
        /// Если false — только плоские ключи верхнего уровня секции профиля.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Profile), System.ComponentModel.DisplayName(ActivityStrings.Field_IncludeNestedSections)]
        public bool Prop_IncludeNestedSections
        {
            get => _includeNestedSections;
            set { _includeNestedSections = value; InvokePropertyChanged(this, "Prop_IncludeNestedSections"); }
        }

        // =========================================================================
        // OUTPUT PROPERTIES
        // =========================================================================

        private string _propStringValue;
        /// <summary>Прочитанное строковое значение (режим SingleValue)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_StringValue)]
        public string Prop_StringValue
        {
            get => _propStringValue;
            set { _propStringValue = value; InvokePropertyChanged(this, "Prop_StringValue"); }
        }

        private string _propDictionary;
        /// <summary>Итоговый словарь Dictionary&lt;string,string&gt; (режимы SectionToDictionary, FullFileToDictionary, ReadProfile)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_DictionaryValues)]
        public string Prop_Dictionary
        {
            get => _propDictionary;
            set { _propDictionary = value; InvokePropertyChanged(this, "Prop_Dictionary"); }
        }

        private string _propKeyExists;
        /// <summary>Флаг: найден ли ключ (SingleValue) или профиль (ReadProfile)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_KeyProfileFound)]
        public string Prop_KeyExists
        {
            get => _propKeyExists;
            set { _propKeyExists = value; InvokePropertyChanged(this, "Prop_KeyExists"); }
        }

        private string _propKeysCount;
        /// <summary>Общее количество ключей в итоговом словаре</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_KeysCount)]
        public string Prop_KeysCount
        {
            get => _propKeysCount;
            set { _propKeysCount = value; InvokePropertyChanged(this, "Prop_KeysCount"); }
        }

        private string _propAvailableProfiles;
        /// <summary>Список всех секций файла верхнего уровня (режим ReadProfile)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_AvailableProfiles)]
        public string Prop_AvailableProfiles
        {
            get => _propAvailableProfiles;
            set { _propAvailableProfiles = value; InvokePropertyChanged(this, "Prop_AvailableProfiles"); }
        }

        private string _propProfileKeysCount;
        /// <summary>Количество ключей из профиля в итоговом словаре (режим ReadProfile)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ProfileKeysCount)]
        public string Prop_ProfileKeysCount
        {
            get => _propProfileKeysCount;
            set { _propProfileKeysCount = value; InvokePropertyChanged(this, "Prop_ProfileKeysCount"); }
        }

        private string _propDefaultKeysCount;
        /// <summary>Количество ключей из секции default в итоговом словаре (режим ReadProfile)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_DefaultKeysCount)]
        public string Prop_DefaultKeysCount
        {
            get => _propDefaultKeysCount;
            set { _propDefaultKeysCount = value; InvokePropertyChanged(this, "Prop_DefaultKeysCount"); }
        }

        // =========================================================================
        // КОНСТРУКТОР
        // =========================================================================

        /// <summary>
        /// Конструктор компонента ReadTomlConfig (Tomlyn 0.10.1 / netstandard2.0)
        /// </summary>
        public ReadTomlConfigBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Чтение TOML конфигурации";
            sdkComponentHelp = @"Компонент ""Чтение TOML конфигурации"" (ReadTomlConfigBack)
Универсальная активность чтения TOML-файлов через Tomlyn 0.10.1 (netstandard2.0).

── Режимы чтения ──────────────────────────────────────
SingleValue         — одно значение по ключу (section.subsection.key)
SectionToDictionary — секция → Dictionary<string,string>
FullFileToDictionary — весь файл → плоский словарь
ReadProfile         — мёрж [default] + [production/staging/...]

── Профиль (только ReadProfile) ───────────────────────
Имя профиля: 'production', 'staging', 'development' и т.д.
Имя секции default: базовая секция (по умолчанию 'default')
Стратегия слияния:
  DefaultThenProfile — default + override профилем (стандарт)
  ProfileOnly        — только секция профиля
  ProfileThenDefault — профиль + добавить недостающее из default
Включать вложенные секции — рекурсивный мёрж [production.database]

── Выходные данные ─────────────────────────────────────
Строковое значение  — результат SingleValue
Словарь значений    — результат словарных режимов
Ключ/профиль найден — bool флаг
Кол-во ключей       — размер итогового словаря
Доступные профили   — List<string> секций файла (ReadProfile)
Ключей из профиля   — статистика мёржа (ReadProfile)
Ключей из default   — статистика мёржа (ReadProfile)

── NuGet зависимость ───────────────────────────────────
<PackageReference Include=""Tomlyn"" Version=""0.10.1"" />";

            sdkComponentIcon = ActivityIcons.Config;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.FileSelector("Prop_FilePath", "Полный путь к TOML-файлу конфигурации"),
                PropertyBuilder.Enum<TomlReadMode>("ReadMode", "Режим чтения: SingleValue / SectionToDictionary / FullFileToDictionary / ReadProfile"),
                PropertyBuilder.Script<string>("Prop_KeyPath", "Путь к ключу через точку (режим SingleValue)"),
                PropertyBuilder.Script<string>("Prop_SectionName", "Имя секции (режим SectionToDictionary)"),
                PropertyBuilder.Script<string>("Prop_DefaultValue", "Значение по умолчанию если ключ не найден"),
                PropertyBuilder.Script<string>("Prop_Encoding", "Кодировка файла (по умолчанию UTF-8)"),
                PropertyBuilder.Script<string>("Prop_ProfileName", "Имя профиля окружения: production, staging (режим ReadProfile)"),
                PropertyBuilder.Script<string>("Prop_DefaultProfileName", "Имя базовой секции (по умолчанию 'default')"),
                PropertyBuilder.Enum<ProfileMergeStrategy>("MergeStrategy", "Стратегия слияния профиля с default"),
                PropertyBuilder.Variable<string>("Prop_StringValue", "Прочитанное строковое значение (режим SingleValue)"),
                PropertyBuilder.Variable<Dictionary<string, string>>("Prop_Dictionary", "Итоговый словарь ключ-значение"),
                PropertyBuilder.Variable<bool>("Prop_KeyExists", "Найден ли ключ (SingleValue) или профиль (ReadProfile)"),
                PropertyBuilder.Variable<int>("Prop_KeysCount", "Количество ключей в итоговом словаре"),
                PropertyBuilder.Variable<List<string>>("Prop_AvailableProfiles", "Список доступных профилей (секций) файла (режим ReadProfile)"),
                PropertyBuilder.Variable<int>("Prop_ProfileKeysCount", "Количество ключей из секции профиля (режим ReadProfile)"),
                PropertyBuilder.Variable<int>("Prop_DefaultKeysCount", "Количество ключей из секции default (режим ReadProfile)")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_Encoding = "\"UTF-8\"";
            this.Prop_DefaultValue = "\"\"";
            this.Prop_DefaultProfileName = "\"default\"";
        }

        // =========================================================================
        // ОСНОВНОЕ ДЕЙСТВИЕ
        // =========================================================================

        /// <summary>
        /// Основное действие — читает TOML-файл согласно выбранному режиму
        /// </summary>
        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var parameters = GetAndValidateParameters(sd);
                TomlTable rootTable = ParseTomlFile(parameters.FilePath, parameters.Encoding);

                switch (parameters.ReadMode)
                {
                    case TomlReadMode.SingleValue:
                        ExecuteSingleValueMode(sd, rootTable, parameters);
                        break;
                    case TomlReadMode.SectionToDictionary:
                        ExecuteSectionToDictionaryMode(sd, rootTable, parameters);
                        break;
                    case TomlReadMode.FullFileToDictionary:
                        ExecuteFullFileToDictionaryMode(sd, rootTable);
                        break;
                    case TomlReadMode.ReadProfile:
                        ExecuteReadProfileMode(sd, rootTable, parameters);
                        break;
                    default:
                        throw new InvalidOperationException($"Неизвестный режим чтения: {parameters.ReadMode}");
                }

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"TOML успешно прочитан в режиме: {parameters.ReadMode}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка чтения TOML-конфигурации: {ex.Message}"
                };
            }
        }

        // =========================================================================
        // ВНУТРЕННИЙ КЛАСС ПАРАМЕТРОВ
        // =========================================================================

        private class TomlReadParameters
        {
            public string FilePath { get; set; }
            public TomlReadMode ReadMode { get; set; }
            public string KeyPath { get; set; }
            public string SectionName { get; set; }
            public string DefaultValue { get; set; }
            public bool ThrowIfKeyNotFound { get; set; }
            public string Encoding { get; set; }
            public string ProfileName { get; set; }
            public string DefaultProfileName { get; set; }
            public ProfileMergeStrategy MergeStrategy { get; set; }
            public bool IncludeNestedSections { get; set; }
        }

        // =========================================================================
        // ПОЛУЧЕНИЕ И ВАЛИДАЦИЯ ПАРАМЕТРОВ
        // =========================================================================

        private TomlReadParameters GetAndValidateParameters(ScriptingData sd)
        {
            string filePath = GetPropertyValue<string>(this.Prop_FilePath, "Prop_FilePath", sd);
            string keyPath = GetPropertyValue<string>(this.Prop_KeyPath, "Prop_KeyPath", sd);
            string sectionName = GetPropertyValue<string>(this.Prop_SectionName, "Prop_SectionName", sd);
            string defaultVal = GetPropertyValue<string>(this.Prop_DefaultValue, "Prop_DefaultValue", sd);
            string encoding = GetPropertyValue<string>(this.Prop_Encoding, "Prop_Encoding", sd);
            string profileName = GetPropertyValue<string>(this.Prop_ProfileName, "Prop_ProfileName", sd);
            string defaultProfileName = GetPropertyValue<string>(this.Prop_DefaultProfileName, "Prop_DefaultProfileName", sd);

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым");
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"TOML-файл не найден: {filePath}");
            if (this.ReadMode == TomlReadMode.SingleValue && string.IsNullOrWhiteSpace(keyPath))
                throw new ArgumentException("Ключ (section.key) обязателен в режиме SingleValue");
            if (this.ReadMode == TomlReadMode.SectionToDictionary && string.IsNullOrWhiteSpace(sectionName))
                throw new ArgumentException("Имя секции обязательно в режиме SectionToDictionary");
            if (this.ReadMode == TomlReadMode.ReadProfile && string.IsNullOrWhiteSpace(profileName))
                throw new ArgumentException("Имя профиля обязательно в режиме ReadProfile");

            if (string.IsNullOrWhiteSpace(encoding)) encoding = "UTF-8";
            if (string.IsNullOrWhiteSpace(defaultProfileName)) defaultProfileName = "default";

            return new TomlReadParameters
            {
                FilePath = filePath,
                ReadMode = this.ReadMode,
                KeyPath = keyPath,
                SectionName = sectionName,
                DefaultValue = defaultVal ?? string.Empty,
                ThrowIfKeyNotFound = this.Prop_ThrowIfKeyNotFound,
                Encoding = encoding,
                ProfileName = profileName,
                DefaultProfileName = defaultProfileName,
                MergeStrategy = this.MergeStrategy,
                IncludeNestedSections = this.Prop_IncludeNestedSections
            };
        }

        // =========================================================================
        // ПАРСИНГ ФАЙЛА ЧЕРЕЗ TOMLYN 0.10.1
        // =========================================================================

        /// <summary>
        /// Читает файл и парсит через Toml.ToModel() в TomlTable.
        /// API совместимо между Tomlyn 0.10.x и 0.16.x.
        /// </summary>
        private TomlTable ParseTomlFile(string filePath, string encodingName)
        {
            Encoding fileEncoding = ResolveEncoding(encodingName);
            string tomlContent = File.ReadAllText(filePath, fileEncoding);

            // Toml.ToModel присутствует в обеих версиях Tomlyn
            return Toml.ToModel(tomlContent);
        }

        /// <summary>
        /// Разрешает имя кодировки в Encoding. При ошибке возвращает UTF-8.
        /// </summary>
        private Encoding ResolveEncoding(string encodingName)
        {
            try { return Encoding.GetEncoding(encodingName); }
            catch { return Encoding.UTF8; }
        }

        // =========================================================================
        // РЕЖИМ 1: ОДНО ЗНАЧЕНИЕ (SingleValue)
        // =========================================================================

        private void ExecuteSingleValueMode(ScriptingData sd, TomlTable rootTable, TomlReadParameters p)
        {
            string[] keyParts = p.KeyPath.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            bool found = TryGetNestedValue(rootTable, keyParts, out string value);

            if (!found)
            {
                if (p.ThrowIfKeyNotFound)
                    throw new KeyNotFoundException($"Ключ '{p.KeyPath}' не найден в TOML-файле");
                value = p.DefaultValue;
            }

            SetVariableValue(this.Prop_StringValue, value, sd);
            SetVariableValue(this.Prop_KeyExists, found, sd);
            SetVariableValue(this.Prop_KeysCount, found ? 1 : 0, sd);
        }

        /// <summary>
        /// Рекурсивно ищет значение по массиву частей пути.
        /// Промежуточные части — TomlTable, финальная — любое значение.
        /// LINQ Aggregate используется для прохода по промежуточным секциям.
        /// </summary>
        private bool TryGetNestedValue(TomlTable table, string[] keyParts, out string value)
        {
            value = string.Empty;
            if (keyParts == null || keyParts.Length == 0) return false;

            // Спускаемся по промежуточным секциям: все части кроме последней
            TomlTable targetTable = keyParts
                .Take(keyParts.Length - 1)
                .Aggregate(table, (current, part) =>
                    current != null && current.ContainsKey(part)
                        ? current[part] as TomlTable
                        : null);

            TomlTable searchIn = keyParts.Length == 1 ? table : targetTable;
            string finalKey = keyParts[keyParts.Length - 1];

            if (searchIn == null || !searchIn.ContainsKey(finalKey)) return false;

            value = TomlValueToString(searchIn[finalKey]);
            return true;
        }

        // =========================================================================
        // РЕЖИМ 2: СЕКЦИЯ В СЛОВАРЬ (SectionToDictionary)
        // =========================================================================

        private void ExecuteSectionToDictionaryMode(ScriptingData sd, TomlTable rootTable, TomlReadParameters p)
        {
            string[] parts = p.SectionName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            TomlTable section = FindNestedSection(rootTable, parts);

            if (section == null)
            {
                if (p.ThrowIfKeyNotFound)
                    throw new KeyNotFoundException($"Секция '{p.SectionName}' не найдена в TOML-файле");

                SetVariableValue(this.Prop_Dictionary, new Dictionary<string, string>(), sd);
                SetVariableValue(this.Prop_KeysCount, 0, sd);
                return;
            }

            // Фильтруем только скалярные значения и обычные массивы — без подсекций
            var dict = section
                .Where(pair => !(pair.Value is TomlTable) && !(pair.Value is TomlTableArray))
                .ToDictionary(pair => pair.Key, pair => TomlValueToString(pair.Value));

            SetVariableValue(this.Prop_Dictionary, dict, sd);
            SetVariableValue(this.Prop_KeysCount, dict.Count, sd);
        }

        // =========================================================================
        // РЕЖИМ 3: ВЕСЬ ФАЙЛ В СЛОВАРЬ (FullFileToDictionary)
        // =========================================================================

        private void ExecuteFullFileToDictionaryMode(ScriptingData sd, TomlTable rootTable)
        {
            var result = new Dictionary<string, string>();
            FlattenTomlTable(rootTable, string.Empty, result);

            SetVariableValue(this.Prop_Dictionary, result, sd);
            SetVariableValue(this.Prop_KeysCount, result.Count, sd);
        }

        // =========================================================================
        // РЕЖИМ 4: ПРОФИЛИ (ReadProfile)
        // =========================================================================

        private void ExecuteReadProfileMode(ScriptingData sd, TomlTable rootTable, TomlReadParameters p)
        {
            // Собираем список всех секций верхнего уровня — это доступные профили
            List<string> availableProfiles = rootTable.Keys
                .Where(key => rootTable[key] is TomlTable)
                .OrderBy(key => key)
                .ToList();

            SetVariableValue(this.Prop_AvailableProfiles, availableProfiles, sd);

            // Ищем секцию запрошенного профиля
            bool profileFound = rootTable.ContainsKey(p.ProfileName)
                                  && rootTable[p.ProfileName] is TomlTable;
            TomlTable profileTable = profileFound ? rootTable[p.ProfileName] as TomlTable : null;

            SetVariableValue(this.Prop_KeyExists, profileFound, sd);

            if (!profileFound)
            {
                if (p.ThrowIfKeyNotFound)
                    throw new KeyNotFoundException(
                        $"Профиль '[{p.ProfileName}]' не найден. " +
                        $"Доступные: {string.Join(", ", availableProfiles)}");

                // Профиль не найден — возвращаем только default (если применимо)
                var fallback = new Dictionary<string, string>();
                int fromDefault = 0;

                if (p.MergeStrategy != ProfileMergeStrategy.ProfileOnly
                    && rootTable.ContainsKey(p.DefaultProfileName)
                    && rootTable[p.DefaultProfileName] is TomlTable defaultFallback)
                {
                    FlattenTomlTable(defaultFallback, string.Empty, fallback);
                    fromDefault = fallback.Count;
                }

                SetVariableValue(this.Prop_Dictionary, fallback, sd);
                SetVariableValue(this.Prop_KeysCount, fallback.Count, sd);
                SetVariableValue(this.Prop_ProfileKeysCount, 0, sd);
                SetVariableValue(this.Prop_DefaultKeysCount, fromDefault, sd);
                return;
            }

            // Плоский словарь из секции профиля
            var profileFlat = new Dictionary<string, string>();
            if (p.IncludeNestedSections)
                FlattenTomlTable(profileTable, string.Empty, profileFlat);
            else
                FlattenShallowTomlTable(profileTable, profileFlat);

            // При ProfileOnly — только профиль, без default
            if (p.MergeStrategy == ProfileMergeStrategy.ProfileOnly)
            {
                SetVariableValue(this.Prop_Dictionary, profileFlat, sd);
                SetVariableValue(this.Prop_KeysCount, profileFlat.Count, sd);
                SetVariableValue(this.Prop_ProfileKeysCount, profileFlat.Count, sd);
                SetVariableValue(this.Prop_DefaultKeysCount, 0, sd);
                return;
            }

            // Ищем секцию default
            TomlTable defaultTable = null;
            if (rootTable.ContainsKey(p.DefaultProfileName)
                && rootTable[p.DefaultProfileName] is TomlTable dt)
                defaultTable = dt;

            // Плоский словарь из default
            var defaultFlat = new Dictionary<string, string>();
            if (defaultTable != null)
            {
                if (p.IncludeNestedSections)
                    FlattenTomlTable(defaultTable, string.Empty, defaultFlat);
                else
                    FlattenShallowTomlTable(defaultTable, defaultFlat);
            }

            // Мёрж двух словарей по стратегии
            var (merged, fromProfile, fromDef) =
                MergeProfileDictionaries(defaultFlat, profileFlat, p.MergeStrategy);

            SetVariableValue(this.Prop_Dictionary, merged, sd);
            SetVariableValue(this.Prop_KeysCount, merged.Count, sd);
            SetVariableValue(this.Prop_ProfileKeysCount, fromProfile, sd);
            SetVariableValue(this.Prop_DefaultKeysCount, fromDef, sd);
        }

        /// <summary>
        /// Мёрж двух плоских словарей по выбранной стратегии.
        /// Возвращает итоговый словарь и статистику: сколько ключей из профиля и из default.
        /// </summary>
        private (Dictionary<string, string> merged, int fromProfile, int fromDefault)
            MergeProfileDictionaries(
                Dictionary<string, string> defaultDict,
                Dictionary<string, string> profileDict,
                ProfileMergeStrategy strategy)
        {
            var merged = new Dictionary<string, string>();

            if (strategy == ProfileMergeStrategy.DefaultThenProfile)
            {
                // Шаг 1: копируем default как базу
                foreach (var pair in defaultDict)
                    merged[pair.Key] = pair.Value;

                // Шаг 2: профиль перекрывает default (все ключи профиля идут поверх)
                foreach (var pair in profileDict)
                    merged[pair.Key] = pair.Value;

                // Ключи только из default — те что профиль не перекрыл
                int fromDefault = defaultDict.Keys.Count(k => !profileDict.ContainsKey(k));
                return (merged, profileDict.Count, fromDefault);
            }
            else // ProfileThenDefault
            {
                // Шаг 1: берём всё из профиля
                foreach (var pair in profileDict)
                    merged[pair.Key] = pair.Value;

                // Шаг 2: из default добавляем только отсутствующие в профиле ключи
                var addedFromDefault = defaultDict
                    .Where(pair => !merged.ContainsKey(pair.Key))
                    .ToList();

                foreach (var pair in addedFromDefault)
                    merged[pair.Key] = pair.Value;

                return (merged, profileDict.Count, addedFromDefault.Count);
            }
        }

        // =========================================================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ОБХОДА TOML
        // =========================================================================

        /// <summary>
        /// Ищет вложенную секцию по массиву частей пути через LINQ Aggregate.
        /// Возвращает null если хотя бы одна часть пути не найдена.
        /// </summary>
        private TomlTable FindNestedSection(TomlTable rootTable, string[] sectionParts)
        {
            return sectionParts.Aggregate(rootTable, (current, part) =>
                current != null && current.ContainsKey(part)
                    ? current[part] as TomlTable
                    : null);
        }

        /// <summary>
        /// Рекурсивно обходит TomlTable и собирает все листовые значения в плоский словарь.
        /// Ключи вложенных секций: "server.database.host"
        /// Массивы таблиц [[array]]: "products[0].name", "products[1].name"
        ///
        /// СОВМЕСТИМОСТЬ Tomlyn 0.10.x:
        ///   TomlTableArray реализует IList&lt;TomlTable&gt; — перебираем через foreach с индексом.
        /// </summary>
        private void FlattenTomlTable(TomlTable table, string prefix, Dictionary<string, string> result)
        {
            foreach (var pair in table)
            {
                string fullKey = string.IsNullOrEmpty(prefix) ? pair.Key : $"{prefix}.{pair.Key}";

                switch (pair.Value)
                {
                    case TomlTable nestedTable:
                        // Вложенная секция [section] — рекурсивный спуск
                        FlattenTomlTable(nestedTable, fullKey, result);
                        break;

                    case TomlTableArray tableArray:
                        // Массив таблиц [[array]] — нумеруем через LINQ Select с индексом
                        // В Tomlyn 0.10.x TomlTableArray : IList<TomlTable> — Select работает корректно
                        foreach (var indexedItem in tableArray.Select((t, i) => new { t, i }))
                            FlattenTomlTable(indexedItem.t, $"{fullKey}[{indexedItem.i}]", result);
                        break;

                    default:
                        // Скалярное значение или TomlArray (скалярный массив)
                        result[fullKey] = TomlValueToString(pair.Value);
                        break;
                }
            }
        }

        /// <summary>
        /// Читает только первый уровень TomlTable без рекурсии.
        /// Используется в ReadProfile при IncludeNestedSections=false.
        /// </summary>
        private void FlattenShallowTomlTable(TomlTable table, Dictionary<string, string> result)
        {
            table
                .Where(pair => !(pair.Value is TomlTable) && !(pair.Value is TomlTableArray))
                .ToList()
                .ForEach(pair => result[pair.Key] = TomlValueToString(pair.Value));
        }

        // =========================================================================
        // КОНВЕРТАЦИЯ object → string (СОВМЕСТИМОСТЬ с Tomlyn 0.10.x)
        // =========================================================================

        /// <summary>
        /// Конвертирует значение TOML-узла в строку.
        ///
        /// ВАЖНО — отличие Tomlyn 0.10.x от 0.16.x:
        ///   В 0.10.x даты с временной зоной (offset datetime) представлены как
        ///   Tomlyn.Syntax.TomlDateTime — структура с полями DateTime и Kind.
        ///   В 0.16.x вместо неё используется стандартный DateTimeOffset.
        ///   Здесь мы используем object-паттерн: если тип не совпал ни с чем известным —
        ///   вызываем ToString(), что корректно работает для TomlDateTime в 0.10.x.
        ///
        /// Поддерживаемые типы Tomlyn 0.10.x:
        ///   string        → строка
        ///   bool          → "true" / "false"
        ///   long          → целое (все int в Tomlyn — long)
        ///   double        → дробное (InvariantCulture)
        ///   DateTime      → ISO 8601 (local datetime без зоны)
        ///   TomlDateTime  → ToString() (offset datetime с зоной, специфично для 0.10.x)
        ///   TomlArray     → "[val1, val2, ...]" (скалярный массив)
        ///   TomlTable     → "{key=val, ...}" (inline-таблица)
        /// </summary>
        private string TomlValueToString(object value)
        {
            if (value == null) return string.Empty;

            switch (value)
            {
                case string s:
                    return s;

                case bool b:
                    // TOML-стиль: нижний регистр
                    return b.ToString().ToLower();

                case long l:
                    // Целые числа в Tomlyn всегда long (не int!)
                    return l.ToString();

                case double d:
                    // Дробные с точкой как разделителем
                    return d.ToString(System.Globalization.CultureInfo.InvariantCulture);

                case DateTime dt:
                    // Local datetime (без временной зоны) — ISO 8601
                    return dt.ToString("O");

                case TomlArray array:
                    // Скалярный массив [1, 2, 3] или ["a", "b"]
                    // Рекурсивно конвертируем каждый элемент через LINQ Select
                    return "[" + string.Join(", ", array.Select(item => TomlValueToString(item))) + "]";

                case TomlTable tbl:
                    // Inline-таблица { key = "val" } → "{key=val, key2=val2}"
                    return "{" + string.Join(", ", tbl.Select(p => $"{p.Key}={TomlValueToString(p.Value)}")) + "}";

                default:
                    // Для Tomlyn 0.16.x: TomlDateTime (offset datetime), TomlFloat и прочие типы.
                    // ToString() корректно работает для всех нераспознанных типов Tomlyn.
                    return value.ToString() ?? string.Empty;
            }
        }

        // =========================================================================
        // ВАЛИДАЦИЯ В ДИЗАЙНЕРЕ
        // =========================================================================

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            ret.ValidateRequired(this.Prop_FilePath, ActivityStrings.Field_FilePath, ActivityStrings.Error_FilePathRequired);

            switch (this.ReadMode)
            {
                case TomlReadMode.SingleValue:
                    ret.ValidateRequired(this.Prop_KeyPath, ActivityStrings.Field_KeyPath, ActivityStrings.Error_KeyPathRequired);
                    break;
                case TomlReadMode.SectionToDictionary:
                    ret.ValidateRequired(this.Prop_SectionName, ActivityStrings.Field_SectionName, ActivityStrings.Error_SectionNameRequired);
                    break;
                case TomlReadMode.ReadProfile:
                    ret.ValidateRequired(this.Prop_ProfileName, ActivityStrings.Field_ProfileName, ActivityStrings.Error_ProfileNameRequired);
                    break;
            }

            return ret;
        }
    }
}