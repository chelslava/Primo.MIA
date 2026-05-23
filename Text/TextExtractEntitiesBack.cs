// =============================================================================
// TextExtractEntitiesBack.cs — активность «Текст: Извлечь сущности».
//
// Находит и извлекает из текста типовые структурированные данные без написания
// регулярных выражений вручную. Каждый тип — проверенный паттерн с нормализацией
// и опциональной дополнительной валидацией.
//
// Поддерживаемые типы (Prop_EntityTypes — через запятую, пусто = все):
//   Email      — адреса электронной почты
//   Phone      — российские номера телефонов (+7, 8)
//   Date       — даты в форматах dd.mm.yyyy, yyyy-mm-dd, «15 марта 2026»
//   Money      — денежные суммы с валютой (руб., USD, EUR, €, $, ¥)
//   INN        — ИНН юридических (10 цифр) и физических (12 цифр) лиц
//   SNILS      — СНИЛС в формате XXX-XXX-XXX XX
//   Passport   — серия и номер паспорта РФ
//   CarNumber  — российские номера автомобилей (А123ВС77, стандарт ГОСТ Р 50577-93)
//   URL        — http/https/ftp URL
//   IP         — IPv4-адреса
//   GUID       — UUID/GUID в формате xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
//   Custom     — пользовательский Regex-паттерн (Prop_CustomPattern)
//
// Валидация ИНН — проверка контрольных цифр по алгоритму ФНС.
// Нормализация — телефоны приводятся к +7 (XXX) XXX-XX-XX,
//                номера авто к стандартному виду с кириллицей.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LTools.Common.Model;
using LTools.Common.Model.Serialization;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Текст: Извлечь сущности».
    /// Находит типовые структурированные данные в тексте без написания regex вручную.
    /// </summary>
    public class TextExtractEntitiesBack : PrimoComponentTO<TextExtractEntities>
    {
        // =====================================================================
        // Внутренняя модель паттерна сущности
        // =====================================================================

        /// <summary>
        /// Описание одного типа сущности: regex-паттерн, нормализатор, валидатор.
        /// </summary>
        private class EntityPattern
        {
            /// <summary>Имя типа — ключ в результирующем словаре.</summary>
            public string TypeName { get; set; }

            /// <summary>Скомпилированный regex для поиска в тексте.</summary>
            public Regex Pattern { get; set; }

            /// <summary>
            /// Уровень уверенности (0–100).
            /// Высокий — у Email, ИНН с валидацией.
            /// Средний — у Phone, Date (много ложных срабатываний возможно).
            /// </summary>
            public int Confidence { get; set; }

            /// <summary>
            /// Функция нормализации найденного значения.
            /// null — нормализация не применяется.
            /// </summary>
            public Func<string, string> Normalize { get; set; }

            /// <summary>
            /// Дополнительная функция валидации.
            /// null — дополнительная проверка не нужна.
            /// Используется для проверки контрольных цифр ИНН.
            /// </summary>
            public Func<string, bool> Validate { get; set; }
        }

        // =====================================================================
        // Реестр паттернов — инициализируется один раз при загрузке класса
        // =====================================================================

        /// <summary>
        /// Все встроенные паттерны сущностей.
        /// Порядок важен: более специфичные паттерны (ИНН, СНИЛС) идут раньше
        /// чем общие (числа), чтобы избежать ложных захватов.
        /// </summary>
        private static readonly List<EntityPattern> BuiltinPatterns =
            new List<EntityPattern>
            {
                // ── Email ──────────────────────────────────────────────────
                new EntityPattern
                {
                    TypeName   = "Email",
                    Confidence = 95,
                    Pattern    = new Regex(
                        @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase),
                    Normalize  = s => s.ToLowerInvariant().Trim()
                },

                // ── URL ────────────────────────────────────────────────────
                new EntityPattern
                {
                    TypeName   = "URL",
                    Confidence = 90,
                    Pattern    = new Regex(
                        @"(?:https?|ftp)://[^\s""'<>]+",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase),
                    Normalize  = s => s.TrimEnd('.', ',', ';', ')')
                },

                // ── GUID / UUID ────────────────────────────────────────────
                new EntityPattern
                {
                    TypeName   = "GUID",
                    Confidence = 99,
                    Pattern    = new Regex(
                        @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b",
                        RegexOptions.Compiled)
                },

                // ── IP-адрес (IPv4) ────────────────────────────────────────
                new EntityPattern
                {
                    TypeName   = "IP",
                    Confidence = 90,
                    Pattern    = new Regex(
                        @"\b(?:(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\b",
                        RegexOptions.Compiled),
                    // Исключаем зарезервированные и маловероятные в тексте адреса
                    Validate   = s => s != "0.0.0.0" && s != "255.255.255.255"
                },

                // ── Телефон (Россия: +7 / 8) ───────────────────────────────
                // Охватывает: +7(495)1234567, +7 495 123-45-67, 84951234567 и т.д.
                new EntityPattern
                {
                    TypeName   = "Phone",
                    Confidence = 80,
                    Pattern    = new Regex(
                        @"(?:\+7|8)[\s\-]?\(?(\d{3})\)?[\s\-]?(\d{3})[\s\-]?(\d{2})[\s\-]?(\d{2})",
                        RegexOptions.Compiled),
                    Normalize  = NormalizePhone
                },

                // ── Дата (множество форматов) ──────────────────────────────
                new EntityPattern
                {
                    TypeName   = "Date",
                    Confidence = 75,
                    Pattern    = new Regex(
                        @"\b(?:\d{1,2}[./\-]\d{1,2}[./\-]\d{2,4}|" +       // 15.03.2026 / 15/03/26
                        @"\d{4}[.\-]\d{2}[.\-]\d{2}|" +                      // 2026-03-15
                        @"\d{1,2}\s+(?:января|февраля|марта|апреля|мая|июня|" +
                        @"июля|августа|сентября|октября|ноября|декабря)" +
                        @"(?:\s+\d{4})?)\b",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase)
                },

                // ── Денежная сумма ─────────────────────────────────────────
                // Охватывает: 14 500,00 руб., USD 1 200.50, € 850, 1000 ₽
                new EntityPattern
                {
                    TypeName   = "Money",
                    Confidence = 75,
                    Pattern    = new Regex(
                        @"(?:USD|EUR|GBP|CNY|RUB|руб\.?|р\.)\s*[\d\s]+(?:[.,]\d{1,2})?" +
                        @"|[\d\s]+(?:[.,]\d{1,2})?\s*(?:USD|EUR|GBP|CNY|RUB|руб\.?|р\.)" +
                        @"|[$€£¥₽]\s*[\d\s]+(?:[.,]\d{1,2})?",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase),
                    Normalize  = s => Regex.Replace(s.Trim(), @"\s{2,}", " ")
                },

                // ── СНИЛС ─────────────────────────────────────────────────
                // Формат: XXX-XXX-XXX XX или XXXXXXXXXXX (11 цифр)
                new EntityPattern
                {
                    TypeName   = "SNILS",
                    Confidence = 92,
                    Pattern    = new Regex(
                        @"\b\d{3}[\-\s]\d{3}[\-\s]\d{3}[\s]\d{2}\b|\b\d{11}\b",
                        RegexOptions.Compiled),
                    Normalize  = NormalizeSNILS
                },

                // ── ИНН ───────────────────────────────────────────────────
                // 10 цифр (юрлицо) или 12 цифр (физлицо)
                // Предшествующий текст «ИНН» повышает точность
                new EntityPattern
                {
                    TypeName   = "INN",
                    Confidence = 85,
                    Pattern    = new Regex(
                        @"(?:ИНН\s*[:№]?\s*)?(?<!\d)(\d{10}|\d{12})(?!\d)",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase),
                    Validate   = ValidateINN
                },

                // ── Паспорт РФ ────────────────────────────────────────────
                // Серия 4 цифры + номер 6 цифр, с предшествующим «паспорт» или без
                new EntityPattern
                {
                    TypeName   = "Passport",
                    Confidence = 70,
                    Pattern    = new Regex(
                        @"(?:паспорт\s+)?(?<!\d)(\d{4})\s*(\d{6})(?!\d)",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase),
                    Normalize  = s => NormalizePassport(s)
                },

                // ── Номер автомобиля (Россия, ГОСТ Р 50577-93) ────────────
                // Стандарт: Б000ББ00(0) — буква-три цифры-две буквы-2/3 цифры региона
                // Поддерживает кириллицу (АВЕКМНОРСТУХ) и латинские замены (для OCR)
                new EntityPattern
                {
                    TypeName   = "CarNumber",
                    Confidence = 88,
                    Pattern    = new Regex(
                        // Кириллические и латинские омоглифы (OCR-дружественный)
                        @"\b[АВЕКМНОРСТУХавекмнорстухABEKMHOPCTYXabekmhopctyx]" +
                        @"\d{3}" +
                        @"[АВЕКМНОРСТУХавекмнорстухABEKMHOPCTYXabekmhopctyx]{2}" +
                        @"\d{2,3}\b",
                        RegexOptions.Compiled),
                    Normalize  = NormalizeCarNumber
                },
            };

        // =====================================================================
        // Словарь для O(1) поиска паттерна по имени типа
        // =====================================================================

        /// <summary>
        /// Словарь: имя типа (upper) → EntityPattern.
        /// Строится один раз из BuiltinPatterns через LINQ.
        /// </summary>
        private static readonly Dictionary<string, EntityPattern> PatternByType =
            BuiltinPatterns.ToDictionary(
                p  => p.TypeName.ToUpperInvariant(),
                p  => p
            );

        // =====================================================================
        // Свойства
        // =====================================================================

        #region Prop_InputText
        private string _propInputText;
        /// <summary>Входной текст для анализа.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_InputString)]
        public string Prop_InputText
        {
            get => _propInputText;
            set { _propInputText = value; InvokePropertyChanged(this, nameof(Prop_InputText)); }
        }
        #endregion

        // ── Чекбоксы выбора типов сущностей ──────────────────────────────────
        // Каждый тип — отдельное булево свойство. Все false = искать все типы.

        #region Prop_ExtractEmail
        private bool _propExtractEmail = true;
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Email")]
        public bool Prop_ExtractEmail
        {
            get => _propExtractEmail;
            set { _propExtractEmail = value; InvokePropertyChanged(this, nameof(Prop_ExtractEmail)); }
        }
        #endregion

        #region Prop_ExtractPhone
        private bool _propExtractPhone = true;
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Phone")]
        public bool Prop_ExtractPhone
        {
            get => _propExtractPhone;
            set { _propExtractPhone = value; InvokePropertyChanged(this, nameof(Prop_ExtractPhone)); }
        }
        #endregion

        #region Prop_ExtractDate
        private bool _propExtractDate = true;
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Date")]
        public bool Prop_ExtractDate
        {
            get => _propExtractDate;
            set { _propExtractDate = value; InvokePropertyChanged(this, nameof(Prop_ExtractDate)); }
        }
        #endregion

        #region Prop_ExtractMoney
        private bool _propExtractMoney = true;
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Money")]
        public bool Prop_ExtractMoney
        {
            get => _propExtractMoney;
            set { _propExtractMoney = value; InvokePropertyChanged(this, nameof(Prop_ExtractMoney)); }
        }
        #endregion

        #region Prop_ExtractINN
        private bool _propExtractINN = true;
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("INN")]
        public bool Prop_ExtractINN
        {
            get => _propExtractINN;
            set { _propExtractINN = value; InvokePropertyChanged(this, nameof(Prop_ExtractINN)); }
        }
        #endregion

        #region Prop_ExtractSNILS
        private bool _propExtractSNILS = true;
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("SNILS")]
        public bool Prop_ExtractSNILS
        {
            get => _propExtractSNILS;
            set { _propExtractSNILS = value; InvokePropertyChanged(this, nameof(Prop_ExtractSNILS)); }
        }
        #endregion

        #region Prop_ExtractPassport
        private bool _propExtractPassport = false;
        /// <summary>Паспорт отключён по умолчанию — много ложных срабатываний без контекста.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Passport")]
        public bool Prop_ExtractPassport
        {
            get => _propExtractPassport;
            set { _propExtractPassport = value; InvokePropertyChanged(this, nameof(Prop_ExtractPassport)); }
        }
        #endregion

        #region Prop_ExtractCarNumber
        private bool _propExtractCarNumber = false;
        /// <summary>Номер авто отключён по умолчанию — включается явно когда нужен.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("CarNumber")]
        public bool Prop_ExtractCarNumber
        {
            get => _propExtractCarNumber;
            set { _propExtractCarNumber = value; InvokePropertyChanged(this, nameof(Prop_ExtractCarNumber)); }
        }
        #endregion

        #region Prop_ExtractURL
        private bool _propExtractURL = true;
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("URL")]
        public bool Prop_ExtractURL
        {
            get => _propExtractURL;
            set { _propExtractURL = value; InvokePropertyChanged(this, nameof(Prop_ExtractURL)); }
        }
        #endregion

        #region Prop_ExtractIP
        private bool _propExtractIP = false;
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("IP")]
        public bool Prop_ExtractIP
        {
            get => _propExtractIP;
            set { _propExtractIP = value; InvokePropertyChanged(this, nameof(Prop_ExtractIP)); }
        }
        #endregion

        #region Prop_ExtractGUID
        private bool _propExtractGUID = false;
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("GUID")]
        public bool Prop_ExtractGUID
        {
            get => _propExtractGUID;
            set { _propExtractGUID = value; InvokePropertyChanged(this, nameof(Prop_ExtractGUID)); }
        }
        #endregion

        #region Prop_ExtractCustom
        private bool _propExtractCustom = false;
        /// <summary>Включить пользовательский паттерн из Prop_CustomPattern.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Custom (свой паттерн)")]
        public bool Prop_ExtractCustom
        {
            get => _propExtractCustom;
            set { _propExtractCustom = value; InvokePropertyChanged(this, nameof(Prop_ExtractCustom)); }
        }
        #endregion

        #region Prop_CustomPattern
        private string _propCustomPattern;
        /// <summary>
        /// Regex-паттерн для типа Custom.
        /// Используется только если "Custom" включён в Prop_EntityTypes.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CustomPattern)]
        public string Prop_CustomPattern
        {
            get => _propCustomPattern;
            set { _propCustomPattern = value; InvokePropertyChanged(this, nameof(Prop_CustomPattern)); }
        }
        #endregion

        #region Prop_CustomTypeName
        private string _propCustomTypeName;
        /// <summary>
        /// Имя типа для Custom-паттерна в результирующем словаре.
        /// По умолчанию "Custom".
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CustomTypeName)]
        public string Prop_CustomTypeName
        {
            get => _propCustomTypeName;
            set { _propCustomTypeName = value; InvokePropertyChanged(this, nameof(Prop_CustomTypeName)); }
        }
        #endregion

        #region Prop_NormalizeResults
        private bool _propNormalizeResults = true;
        /// <summary>
        /// Нормализовать найденные значения (телефон → +7 (XXX) XXX-XX-XX,
        /// email → нижний регистр и т.д.).
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_NormalizeResults)]
        public bool Prop_NormalizeResults
        {
            get => _propNormalizeResults;
            set { _propNormalizeResults = value; InvokePropertyChanged(this, nameof(Prop_NormalizeResults)); }
        }
        #endregion

        #region Prop_ValidateINN
        private bool _propValidateINN = true;
        /// <summary>
        /// Проверять контрольные цифры ИНН по алгоритму ФНС.
        /// При отключении — включаются все 10/12-значные числа.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ValidateINN)]
        public bool Prop_ValidateINN
        {
            get => _propValidateINN;
            set { _propValidateINN = value; InvokePropertyChanged(this, nameof(Prop_ValidateINN)); }
        }
        #endregion

        #region Prop_MinConfidence
        private int _propMinConfidence = 0;
        /// <summary>
        /// Минимальный уровень уверенности паттерна (0–100).
        /// Паттерны с уверенностью ниже порога пропускаются.
        /// 0 = использовать все паттерны.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_MinConfidence)]
        public int Prop_MinConfidence
        {
            get => _propMinConfidence;
            set { _propMinConfidence = value; InvokePropertyChanged(this, nameof(Prop_MinConfidence)); }
        }
        #endregion

        #region Prop_Results (Выходной)
        private string _propResults;
        /// <summary>
        /// Имя переменной скрипта для записи словаря найденных сущностей.
        /// Тип: Dictionary&lt;string, List&lt;string&gt;&gt;.
        /// Ключ = тип сущности ("Email", "Phone" и т.д.),
        /// значение = список найденных строк.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(
            DataType = typeof(Dictionary<string, List<string>>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_EntityResults)]
        public string Prop_Results
        {
            get => _propResults;
            set { _propResults = value; InvokePropertyChanged(this, nameof(Prop_Results)); }
        }
        #endregion

        #region Prop_AllFound (Выходной)
        private string _propAllFound;
        /// <summary>
        /// Имя переменной скрипта для записи плоского списка всех найденных значений.
        /// Тип: List&lt;string&gt;.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_AllFound)]
        public string Prop_AllFound
        {
            get => _propAllFound;
            set { _propAllFound = value; InvokePropertyChanged(this, nameof(Prop_AllFound)); }
        }
        #endregion

        #region Prop_TotalCount (Выходной)
        private string _propTotalCount;
        /// <summary>Имя переменной скрипта для записи общего количества найденных сущностей (int).</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_EntityTotalCount)]
        public string Prop_TotalCount
        {
            get => _propTotalCount;
            set { _propTotalCount = value; InvokePropertyChanged(this, nameof(Prop_TotalCount)); }
        }
        #endregion

        #region Prop_HasMatches (Выходной)
        private string _propHasMatches;
        /// <summary>Имя переменной скрипта для записи флага наличия хотя бы одного совпадения (bool).</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_HasMatches)]
        public string Prop_HasMatches
        {
            get => _propHasMatches;
            set { _propHasMatches = value; InvokePropertyChanged(this, nameof(Prop_HasMatches)); }
        }
        #endregion

        #region Prop_FoundTypes (Выходной)
        private string _propFoundTypes;
        /// <summary>
        /// Имя переменной скрипта для записи списка типов с хотя бы одним совпадением.
        /// Тип: List&lt;string&gt;.
        /// Пример: ["Email", "Phone", "INN"].
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_FoundTypes)]
        public string Prop_FoundTypes
        {
            get => _propFoundTypes;
            set { _propFoundTypes = value; InvokePropertyChanged(this, nameof(Prop_FoundTypes)); }
        }
        #endregion

        // =====================================================================
        // Служебные свойства
        // =====================================================================

        public override string GroupName
        {
            get => ActivityCategories.Utilities;
            protected set { }
        }

        protected override int sdkTimeOut
        {
            get => 30000;
            set { }
        }

        // =====================================================================
        // Конструктор
        // =====================================================================

        public TextExtractEntitiesBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_TextExtractEntities;
            sdkComponentHelp =
                "Находит и извлекает из текста типовые структурированные данные.\n" +
                "\n" +
                "── Поддерживаемые типы ────────────────────────\n" +
                "Email      — адреса электронной почты\n" +
                "Phone      — российские телефоны (+7, 8)\n" +
                "Date       — даты (dd.mm.yyyy, yyyy-mm-dd, «15 марта 2026»)\n" +
                "Money      — суммы с валютой (руб., USD, €, $ и т.д.)\n" +
                "INN        — ИНН юрлиц (10 цифр) и физлиц (12 цифр)\n" +
                "SNILS      — СНИЛС (XXX-XXX-XXX XX)\n" +
                "Passport   — паспорт РФ (серия + номер)\n" +
                "CarNumber  — номера авто РФ (А123ВС77)\n" +
                "URL        — http/https/ftp ссылки\n" +
                "IP         — IPv4-адреса\n" +
                "GUID       — UUID/GUID\n" +
                "Custom     — пользовательский Regex\n" +
                "\n" +
                "── Параметры ──────────────────────────────────\n" +
                "Типы сущностей — через запятую (пусто = все)\n" +
                "Нормализация   — телефон → +7 (XXX) XXX-XX-XX\n" +
                "Валидация ИНН  — проверка контрольных цифр ФНС\n" +
                "Мин. уверенность — отфильтровать ненадёжные паттерны\n" +
                "\n" +
                "── Выходные данные ────────────────────────────\n" +
                "Результаты     — Dict<string, List<string>> по типу\n" +
                "Все найденные  — плоский List<string>\n" +
                "Количество     — int общее число\n" +
                "Есть совпадения — bool\n" +
                "Найденные типы — List<string> типов с результатами";

            sdkComponentIcon = ActivityIcons.Regex;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                // Основные
                PropertyBuilder.Script<string>("Prop_InputText",       ActivityStrings.Field_InputString),
                // Типы — чекбоксы
                PropertyBuilder.BooleanObject("Prop_ExtractEmail",     "Email"),
                PropertyBuilder.BooleanObject("Prop_ExtractPhone",     "Phone"),
                PropertyBuilder.BooleanObject("Prop_ExtractDate",      "Date"),
                PropertyBuilder.BooleanObject("Prop_ExtractMoney",     "Money"),
                PropertyBuilder.BooleanObject("Prop_ExtractINN",       "INN"),
                PropertyBuilder.BooleanObject("Prop_ExtractSNILS",     "SNILS"),
                PropertyBuilder.BooleanObject("Prop_ExtractPassport",  "Passport"),
                PropertyBuilder.BooleanObject("Prop_ExtractCarNumber", "CarNumber"),
                PropertyBuilder.BooleanObject("Prop_ExtractURL",       "URL"),
                PropertyBuilder.BooleanObject("Prop_ExtractIP",        "IP"),
                PropertyBuilder.BooleanObject("Prop_ExtractGUID",      "GUID"),
                PropertyBuilder.BooleanObject("Prop_ExtractCustom",    "Custom (свой паттерн)"),
                // Настройки Custom
                PropertyBuilder.Script<string>("Prop_CustomPattern",   ActivityStrings.Field_CustomPattern),
                PropertyBuilder.Script<string>("Prop_CustomTypeName",  ActivityStrings.Field_CustomTypeName),
                // Прочие настройки
                PropertyBuilder.BooleanObject("Prop_NormalizeResults", ActivityStrings.Field_NormalizeResults),
                PropertyBuilder.BooleanObject("Prop_ValidateINN",      ActivityStrings.Field_ValidateINN),
                PropertyBuilder.Script<int>("Prop_MinConfidence",      ActivityStrings.Field_MinConfidence),
                // Выходные
                PropertyBuilder.Variable<Dictionary<string,List<string>>>(
                    "Prop_Results",    ActivityStrings.Field_EntityResults),
                PropertyBuilder.Variable<List<string>>("Prop_AllFound",    ActivityStrings.Field_AllFound),
                PropertyBuilder.Variable<int>("Prop_TotalCount",           ActivityStrings.Field_EntityTotalCount),
                PropertyBuilder.Variable<bool>("Prop_HasMatches",          ActivityStrings.Field_HasMatches),
                PropertyBuilder.Variable<List<string>>("Prop_FoundTypes",  ActivityStrings.Field_FoundTypes)
            };

            InitClass(container);

            // По умолчанию включены наиболее часто нужные типы
            this.Prop_ExtractEmail     = true;
            this.Prop_ExtractPhone     = true;
            this.Prop_ExtractDate      = true;
            this.Prop_ExtractMoney     = true;
            this.Prop_ExtractINN       = true;
            this.Prop_ExtractSNILS     = true;
            this.Prop_ExtractPassport  = false; // много ложных срабатываний
            this.Prop_ExtractCarNumber = false; // включается явно
            this.Prop_ExtractURL       = true;
            this.Prop_ExtractIP        = false;
            this.Prop_ExtractGUID      = false;
            this.Prop_ExtractCustom    = false;
            this.Prop_CustomTypeName   = "Custom";
            this.Prop_NormalizeResults = true;
            this.Prop_ValidateINN      = true;
            this.Prop_MinConfidence    = 0;
        }

        // =====================================================================
        // Выполнение
        // =====================================================================

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                string inputText = GetPropertyValue<string>(
                    this.Prop_InputText, nameof(Prop_InputText), sd);

                if (inputText == null)
                    return Fail(ActivityStrings.Error_InputStringRequired);

                // ── Определяем набор паттернов для поиска ─────────────────

                // Строим HashSet из булевых свойств-чекбоксов через LINQ
                // Словарь: имя типа → включён ли чекбокс
                var typeFlags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Email",     this.Prop_ExtractEmail     },
                    { "Phone",     this.Prop_ExtractPhone     },
                    { "Date",      this.Prop_ExtractDate      },
                    { "Money",     this.Prop_ExtractMoney     },
                    { "INN",       this.Prop_ExtractINN       },
                    { "SNILS",     this.Prop_ExtractSNILS     },
                    { "Passport",  this.Prop_ExtractPassport  },
                    { "CarNumber", this.Prop_ExtractCarNumber },
                    { "URL",       this.Prop_ExtractURL       },
                    { "IP",        this.Prop_ExtractIP        },
                    { "GUID",      this.Prop_ExtractGUID      },
                };

                // Если ни один тип не выбран — активируем все встроенные
                bool allTypes = typeFlags.Values.All(v => !v);

                HashSet<string> requestedTypes = allTypes
                    ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    : new HashSet<string>(
                        typeFlags
                            .Where(kv => kv.Value)
                            .Select(kv => kv.Key),
                        StringComparer.OrdinalIgnoreCase
                      );

                bool customRequested = this.Prop_ExtractCustom;

                // Собираем список активных паттернов через LINQ
                List<EntityPattern> activePatterns = BuiltinPatterns
                    .Where(p => allTypes || requestedTypes.Contains(p.TypeName.ToUpperInvariant()))
                    .Where(p => p.Confidence >= this.Prop_MinConfidence)
                    .ToList();

                // Добавляем Custom-паттерн если запрошен
                if (customRequested && !string.IsNullOrWhiteSpace(this.Prop_CustomPattern))
                {
                    string customTypeName = string.IsNullOrWhiteSpace(this.Prop_CustomTypeName)
                        ? "Custom"
                        : this.Prop_CustomTypeName;

                    try
                    {
                        activePatterns.Add(new EntityPattern
                        {
                            TypeName   = customTypeName,
                            Confidence = 100,
                            Pattern    = new Regex(
                                this.Prop_CustomPattern,
                                RegexOptions.Compiled | RegexOptions.IgnoreCase)
                        });
                    }
                    catch (ArgumentException ex)
                    {
                        return Fail(
                            $"Некорректный пользовательский паттерн: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        return Fail(
                            $"Некорректный пользовательский паттерн: {ex.Message}");
                    }
                }

                // ── Выполняем поиск по каждому паттерну ───────────────────

                bool   validateINN  = this.Prop_ValidateINN;
                bool   normalize    = this.Prop_NormalizeResults;
                var    results      = new Dictionary<string, List<string>>();

                foreach (EntityPattern ep in activePatterns)
                {
                    List<string> found = ep.Pattern
                        .Matches(inputText)
                        .Cast<Match>()
                        .Select(m => m.Value)
                        // Дополнительная валидация (контрольные цифры ИНН)
                        .Where(v => ep.Validate == null
                            || (ep.TypeName == "INN" ? validateINN : true)
                                ? ep.Validate == null || ep.Validate(v)
                                : true)
                        // Нормализация — применяем если включена и нормализатор есть
                        .Select(v => normalize && ep.Normalize != null
                            ? ep.Normalize(v)
                            : v)
                        // Дедупликация — одинаковые значения не дублируем
                        .Distinct()
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .ToList();

                    if (found.Count > 0)
                        results[ep.TypeName] = found;
                }

                // ── Формируем выходные параметры ───────────────────────────

                // Плоский список всех найденных значений через LINQ
                List<string> allFound = results.Values
                    .SelectMany(list => list)
                    .ToList();

                int          totalCount = allFound.Count;
                bool         hasMatches = totalCount > 0;
                List<string> foundTypes = results.Keys.ToList();

                SetVariableValue(this.Prop_Results,    results,    sd);
                SetVariableValue(this.Prop_AllFound,   allFound,   sd);
                SetVariableValue(this.Prop_TotalCount, totalCount, sd);
                SetVariableValue(this.Prop_HasMatches, hasMatches, sd);
                SetVariableValue(this.Prop_FoundTypes, foundTypes, sd);

                // ── Сообщение о результатах ────────────────────────────────

                if (!hasMatches)
                    return new ExecutionResult
                    {
                        IsSuccess      = true,
                        SuccessMessage = "Сущностей не найдено"
                    };

                // Краткая сводка: "Найдено 5: Email×2, Phone×1, INN×2"
                string summary = string.Join(", ", results
                    .Select(kv => $"{kv.Key}×{kv.Value.Count}"));

                return new ExecutionResult
                {
                    IsSuccess      = true,
                    SuccessMessage = $"Найдено {totalCount}: {summary}"
                };
            }
            catch (ArgumentException ex)
            {
                return Fail($"Неверный аргумент: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return Fail($"Недопустимая операция: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail($"Ошибка извлечения сущностей: {ex.Message}");
            }
        }

        // =====================================================================
        // Функции нормализации
        // =====================================================================

        /// <summary>
        /// Нормализует российский номер телефона к формату +7 (XXX) XXX-XX-XX.
        /// Убирает все разделители, добавляет единый формат.
        /// </summary>
        private static string NormalizePhone(string raw)
        {
            // Оставляем только цифры
            string digits = new string(raw.Where(char.IsDigit).ToArray());

            // Заменяем ведущую 8 на 7
            if (digits.StartsWith("8") && digits.Length == 11)
                digits = "7" + digits.Substring(1);

            if (digits.Length != 11 || !digits.StartsWith("7"))
                return raw; // не смогли нормализовать — возвращаем как есть

            return $"+7 ({digits.Substring(1, 3)}) {digits.Substring(4, 3)}-" +
                   $"{digits.Substring(7, 2)}-{digits.Substring(9, 2)}";
        }

        /// <summary>
        /// Нормализует СНИЛС к формату XXX-XXX-XXX XX.
        /// </summary>
        private static string NormalizeSNILS(string raw)
        {
            string digits = new string(raw.Where(char.IsDigit).ToArray());
            if (digits.Length != 11) return raw;
            return $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-" +
                   $"{digits.Substring(6, 3)} {digits.Substring(9, 2)}";
        }

        /// <summary>
        /// Нормализует номер паспорта к формату «XXXX XXXXXX».
        /// </summary>
        private static string NormalizePassport(string raw)
        {
            string digits = new string(raw.Where(char.IsDigit).ToArray());
            if (digits.Length == 10)
                return $"{digits.Substring(0, 4)} {digits.Substring(4, 6)}";
            return raw;
        }

        /// <summary>
        /// Нормализует номер автомобиля РФ:
        /// — приводит латинские омоглифы к кириллице (для OCR-текста)
        /// — убирает пробелы внутри номера
        /// — приводит к верхнему регистру.
        /// Латино-кириллические омоглифы: A→А, B→В, E→Е, K→К, M→М,
        /// H→Н, O→О, P→Р, C→С, T→Т, Y→У, X→Х.
        /// </summary>
        private static string NormalizeCarNumber(string raw)
        {
            // Словарь замены латинских омоглифов на кириллицу
            var latinToCyr = new Dictionary<char, char>
            {
                { 'A', 'А' }, { 'B', 'В' }, { 'E', 'Е' }, { 'K', 'К' },
                { 'M', 'М' }, { 'H', 'Н' }, { 'O', 'О' }, { 'P', 'Р' },
                { 'C', 'С' }, { 'T', 'Т' }, { 'Y', 'У' }, { 'X', 'Х' },
                { 'a', 'а' }, { 'b', 'в' }, { 'e', 'е' }, { 'k', 'к' },
                { 'm', 'м' }, { 'h', 'н' }, { 'o', 'о' }, { 'p', 'р' },
                { 'c', 'с' }, { 't', 'т' }, { 'y', 'у' }, { 'x', 'х' }
            };

            string upper = raw.ToUpperInvariant().Replace(" ", "");

            var sb = new StringBuilder(upper.Length);
            foreach (char c in upper)
                sb.Append(latinToCyr.TryGetValue(c, out char cyr) ? cyr : c);

            return sb.ToString();
        }

        // =====================================================================
        // Валидация ИНН (алгоритм ФНС)
        // =====================================================================

        /// <summary>
        /// Проверяет контрольные цифры ИНН по алгоритму ФНС России.
        /// 10-значный ИНН (юрлицо): одна контрольная цифра.
        /// 12-значный ИНН (физлицо): две контрольные цифры.
        /// Использует LINQ для вычисления взвешенных сумм.
        /// </summary>
        private static bool ValidateINN(string inn)
        {
            if (inn == null) return false;

            // Весовые коэффициенты для 10-значного ИНН
            int[] w10 = { 2, 4, 10, 3, 5, 9, 4, 6, 8 };
            // Весовые коэффициенты для 12-значного (11-я и 12-я контрольные)
            int[] w11 = { 7, 2, 4, 10, 3, 5, 9, 4, 6, 8 };
            int[] w12 = { 3, 7, 2, 4,  10, 3, 5, 9, 4, 6, 8 };

            if (inn.Length == 10)
            {
                int check = inn.Take(9)
                    .Select((c, i) => (c - '0') * w10[i])
                    .Sum() % 11 % 10;
                return check == (inn[9] - '0');
            }

            if (inn.Length == 12)
            {
                int check11 = inn.Take(10)
                    .Select((c, i) => (c - '0') * w11[i])
                    .Sum() % 11 % 10;
                int check12 = inn.Take(11)
                    .Select((c, i) => (c - '0') * w12[i])
                    .Sum() % 11 % 10;
                return check11 == (inn[10] - '0') && check12 == (inn[11] - '0');
            }

            return false;
        }

        // =====================================================================
        // Вспомогательные методы
        // =====================================================================

        private static ExecutionResult Fail(string msg) =>
            new ExecutionResult { IsSuccess = false, ErrorMessage = msg };

        // =====================================================================
        // Валидация
        // =====================================================================

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            if (string.IsNullOrWhiteSpace(this.Prop_InputText))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_InputText),
                    Error        = ActivityStrings.Error_InputStringRequired
                });

            // Если выбран Custom — паттерн обязателен
            if (this.Prop_ExtractCustom
                && string.IsNullOrWhiteSpace(this.Prop_CustomPattern))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_CustomPattern),
                    Error        = "Пользовательский паттерн обязателен при включённом Custom"
                });

            // Уверенность должна быть 0–100
            if (this.Prop_MinConfidence < 0 || this.Prop_MinConfidence > 100)
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_MinConfidence),
                    Error        = "Минимальная уверенность должна быть от 0 до 100"
                });

            return ret;
        }
    }
}
