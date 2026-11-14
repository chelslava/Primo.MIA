// =============================================================================
// Активность ReadTomlConfig — универсальное чтение конфигурационных файлов TOML.
//
// ВАЖНО — совместимость:
//   Используется Tomlyn версии 2.4.0 (netstandard2.0), совместима с .NET Framework 4.8.1.
//
//   В .csproj укажите:
//     <package id="Tomlyn" version="2.4.0" targetFramework="net481" />
//
//   API v2.x замена для v0.16.x:
//     - Toml.ToModel(string)        → TomlSerializer.Deserialize<TomlTable>(string, default)
//     - Toml.FromModel(TomlTable)    → TomlSerializer.Serialize(TomlTable)
//     - TomlTable, TomlArray, TomlTableArray — без изменений
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
            set { _propFilePath = value; InvokePropertyChanged(this, nameof(Prop_FilePath)); }
        }

        private TomlReadMode _readMode = TomlReadMode.SingleValue;
        /// <summary>Режим чтения: SingleValue / SectionToDictionary / FullFileToDictionary / ReadProfile</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_ReadMode)]
        public TomlReadMode ReadMode
        {
            get => _readMode;
            set { _readMode = value; InvokePropertyChanged(this, nameof(ReadMode)); }
        }

        private string _propKeyPath;
        /// <summary>Путь к ключу через точку: "section.subsection.key". Обязателен в режиме SingleValue.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_KeyPath)]
        public string Prop_KeyPath
        {
            get => _propKeyPath;
            set { _propKeyPath = value; InvokePropertyChanged(this, nameof(Prop_KeyPath)); }
        }

        private string _propSectionName;
        /// <summary>Имя секции для режима SectionToDictionary. Поддерживает вложенность: "app.database".</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SectionName)]
        public string Prop_SectionName
        {
            get => _propSectionName;
            set { _propSectionName = value; InvokePropertyChanged(this, nameof(Prop_SectionName)); }
        }

        private string _propDefaultValue;
        /// <summary>Значение по умолчанию для режима SingleValue, если ключ не найден.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_DefaultValue)]
        public string Prop_DefaultValue
        {
            get => _propDefaultValue;
            set { _propDefaultValue = value; InvokePropertyChanged(this, nameof(Prop_DefaultValue)); }
        }

        private bool _throwIfKeyNotFound = false;
        /// <summary>Выбросить исключение если ключ/секция/профиль не найдены. Если false — вернуть DefaultValue или пустой словарь.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_ThrowIfNotFound)]
        public bool Prop_ThrowIfKeyNotFound
        {
            get => _throwIfKeyNotFound;
            set { _throwIfKeyNotFound = value; InvokePropertyChanged(this, nameof(Prop_ThrowIfKeyNotFound)); }
        }

        private string _propEncoding;
        /// <summary>Кодировка файла. По умолчанию UTF-8.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_File), System.ComponentModel.DisplayName(ActivityStrings.Field_Encoding)]
        public string Prop_Encoding
        {
            get => _propEncoding;
            set { _propEncoding = value; InvokePropertyChanged(this, nameof(Prop_Encoding)); }
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
            set { _propProfileName = value; InvokePropertyChanged(this, nameof(Prop_ProfileName)); }
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
            set { _propDefaultProfileName = value; InvokePropertyChanged(this, nameof(Prop_DefaultProfileName)); }
        }

        private ProfileMergeStrategy _mergeStrategy = ProfileMergeStrategy.DefaultThenProfile;
        /// <summary>Стратегия слияния: DefaultThenProfile / ProfileOnly / ProfileThenDefault</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Profile), System.ComponentModel.DisplayName(ActivityStrings.Field_MergeStrategy)]
        public ProfileMergeStrategy MergeStrategy
        {
            get => _mergeStrategy;
            set { _mergeStrategy = value; InvokePropertyChanged(this, nameof(MergeStrategy)); }
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
            set { _includeNestedSections = value; InvokePropertyChanged(this, nameof(Prop_IncludeNestedSections)); }
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
            set { _propStringValue = value; InvokePropertyChanged(this, nameof(Prop_StringValue)); }
        }

        private string _propDictionary;
        /// <summary>Итоговый словарь Dictionary&lt;string,string&gt; (режимы SectionToDictionary, FullFileToDictionary, ReadProfile)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_DictionaryValues)]
        public string Prop_Dictionary
        {
            get => _propDictionary;
            set { _propDictionary = value; InvokePropertyChanged(this, nameof(Prop_Dictionary)); }
        }

        private string _propKeyExists;
        /// <summary>Флаг: найден ли ключ (SingleValue) или профиль (ReadProfile)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_KeyProfileFound)]
        public string Prop_KeyExists
        {
            get => _propKeyExists;
            set { _propKeyExists = value; InvokePropertyChanged(this, nameof(Prop_KeyExists)); }
        }

        private string _propKeysCount;
        /// <summary>Общее количество ключей в итоговом словаре</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_KeysCount)]
        public string Prop_KeysCount
        {
            get => _propKeysCount;
            set { _propKeysCount = value; InvokePropertyChanged(this, nameof(Prop_KeysCount)); }
        }

        private string _propAvailableProfiles;
        /// <summary>Список всех секций файла верхнего уровня (режим ReadProfile)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_AvailableProfiles)]
        public string Prop_AvailableProfiles
        {
            get => _propAvailableProfiles;
            set { _propAvailableProfiles = value; InvokePropertyChanged(this, nameof(Prop_AvailableProfiles)); }
        }

        private string _propProfileKeysCount;
        /// <summary>Количество ключей из профиля в итоговом словаре (режим ReadProfile)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ProfileKeysCount)]
        public string Prop_ProfileKeysCount
        {
            get => _propProfileKeysCount;
            set { _propProfileKeysCount = value; InvokePropertyChanged(this, nameof(Prop_ProfileKeysCount)); }
        }

        private string _propDefaultKeysCount;
        /// <summary>Количество ключей из секции default в итоговом словаре (режим ReadProfile)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_DefaultKeysCount)]
        public string Prop_DefaultKeysCount
        {
            get => _propDefaultKeysCount;
            set { _propDefaultKeysCount = value; InvokePropertyChanged(this, nameof(Prop_DefaultKeysCount)); }
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
                var logic = new ReadTomlConfigLogic();
                var parameters = GetAndValidateParameters(sd);

                switch (parameters.ReadMode)
                {
                    case TomlReadMode.SingleValue:
                    {
                        var (found, value) = logic.ReadSingleValue(
                            parameters.FilePath,
                            parameters.KeyPath,
                            parameters.DefaultValue,
                            parameters.Encoding);

                        if (!found && parameters.ThrowIfKeyNotFound)
                            throw new KeyNotFoundException($"Ключ '{parameters.KeyPath}' не найден в TOML-файле");

                        SetVariableValue(this.Prop_StringValue, value, sd);
                        SetVariableValue(this.Prop_KeyExists, found, sd);
                        SetVariableValue(this.Prop_KeysCount, found ? 1 : 0, sd);
                        break;
                    }
                    case TomlReadMode.SectionToDictionary:
                    {
                        var dict = logic.ReadSectionToDictionary(
                            parameters.FilePath,
                            parameters.SectionName,
                            parameters.Encoding);

                        if (dict.Count == 0 && parameters.ThrowIfKeyNotFound)
                            throw new KeyNotFoundException($"Секция '{parameters.SectionName}' не найдена в TOML-файле");

                        SetVariableValue(this.Prop_Dictionary, dict, sd);
                        SetVariableValue(this.Prop_KeysCount, dict.Count, sd);
                        break;
                    }
                    case TomlReadMode.FullFileToDictionary:
                    {
                        var dict = logic.ReadFullFileToDictionary(parameters.FilePath, parameters.Encoding);
                        SetVariableValue(this.Prop_Dictionary, dict, sd);
                        SetVariableValue(this.Prop_KeysCount, dict.Count, sd);
                        break;
                    }
                    case TomlReadMode.ReadProfile:
                    {
                        try
                        {
                            var (merged, fromProfile, fromDefault, availableProfiles) = logic.ReadProfile(
                                parameters.FilePath,
                                parameters.ProfileName,
                                parameters.DefaultProfileName,
                                parameters.MergeStrategy,
                                parameters.IncludeNestedSections,
                                parameters.Encoding);

                            SetVariableValue(this.Prop_AvailableProfiles, availableProfiles, sd);
                            SetVariableValue(this.Prop_KeyExists, true, sd);
                            SetVariableValue(this.Prop_Dictionary, merged, sd);
                            SetVariableValue(this.Prop_KeysCount, merged.Count, sd);
                            SetVariableValue(this.Prop_ProfileKeysCount, fromProfile, sd);
                            SetVariableValue(this.Prop_DefaultKeysCount, fromDefault, sd);
                        }
                        catch (KeyNotFoundException) when (!parameters.ThrowIfKeyNotFound)
                        {
                            var fallback = new Dictionary<string, string>();
                            int fromDefault = 0;
                            List<string> availableProfiles;

                            try
                            {
                                var fullFile = logic.ReadFullFileToDictionary(parameters.FilePath, parameters.Encoding);
                                availableProfiles = fullFile.Keys
                                    .Select(key => key.Split('.')[0])
                                    .Distinct()
                                    .Where(key => !string.IsNullOrWhiteSpace(key))
                                    .OrderBy(key => key)
                                    .ToList();
                            }
                            catch (Exception)
                            {
                                // Профили не извлечь (файл повреждён/недоступен) —
                                // режим "ключ не найден" корректно продолжит работу с пустым списком
                                availableProfiles = new List<string>();
                            }

                            if (parameters.MergeStrategy != ProfileMergeStrategy.ProfileOnly)
                            {
                                try
                                {
                                    var (defaultOnly, _, defaultCount, _) = logic.ReadProfile(
                                        parameters.FilePath,
                                        parameters.DefaultProfileName,
                                        parameters.DefaultProfileName,
                                        ProfileMergeStrategy.ProfileOnly,
                                        parameters.IncludeNestedSections,
                                        parameters.Encoding);
                                    fallback = defaultOnly;
                                    fromDefault = defaultCount;
                                }
                                catch (KeyNotFoundException)
                                {
                                    // Секция профиля по умолчанию отсутствует — это не ошибка:
                                    // возвращаем пустой fallback без генерации исключения
                                }
                            }

                            SetVariableValue(this.Prop_AvailableProfiles, availableProfiles, sd);
                            SetVariableValue(this.Prop_KeyExists, false, sd);
                            SetVariableValue(this.Prop_Dictionary, fallback, sd);
                            SetVariableValue(this.Prop_KeysCount, fallback.Count, sd);
                            SetVariableValue(this.Prop_ProfileKeysCount, 0, sd);
                            SetVariableValue(this.Prop_DefaultKeysCount, fromDefault, sd);
                        }
                        break;
                    }
                    default:
                        throw new InvalidOperationException($"Неизвестный режим чтения: {parameters.ReadMode}");
                }

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"TOML успешно прочитан в режиме: {parameters.ReadMode}"
                };
            }
            catch (System.IO.IOException ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка ввода-вывода: {ex.Message}"
                };
            }
            catch (ArgumentException ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Неверный аргумент: {ex.Message}"
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

        // Режимы чтения реализованы в ReadTomlConfigLogic (issue #38,
        // разбиение монолита); устаревшие приватные копии удалены.

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
