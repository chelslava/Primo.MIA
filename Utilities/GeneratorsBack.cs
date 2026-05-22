// =============================================================================
// GeneratorsBack.cs — активность «Генераторы».
//
// Объединённая активность для генерации различных значений в одном компоненте.
// Выбор типа генерации осуществляется через перечисление GeneratorType.
//
// Поддерживаемые типы генерации:
//   Guid          — глобальный уникальный идентификатор (UUID) в форматах N/D/B/P/X
//   FileName      — уникальное имя файла с проверкой существующих в директории
//   RandomNumber  — случайное целое число в диапазоне [Min, Max]
//   Timestamp     — текущая дата/время в выбранном или кастомном формате
//   Counter       — последовательный счётчик с настраиваемым шагом (thread-safe)
//   HashId        — детерминированный ID на основе SHA-256 от входной строки
//   Username      — email-адрес в формате имя.фамилия@домен
//   Template      — заполнение строки-шаблона {key} значениями из словаря
//
// ВАЖНО:
//   Counter использует статическое поле — значение сохраняется между вызовами
//   в рамках одного процесса Primo RPA. Для сброса перезапустите процесс.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Генераторы».
    /// Объединяет восемь генераторов в одном компоненте:
    /// GUID, имя файла, случайное число, временная метка,
    /// счётчик, хеш-ID, email-имя пользователя, шаблон.
    /// Результат всегда строка — записывается в Prop_OutputVariable.
    /// </summary>
    public class GeneratorsBack : PrimoComponentTO<Generators>
    {
        /// <inheritdoc/>
        public override string GroupName { get => ActivityCategories.Utilities; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // =========================================================================
        // СТАТИЧЕСКИЙ СЧЁТЧИК (thread-safe через Interlocked)
        // =========================================================================

        /// <summary>
        /// Текущее значение счётчика. static — сохраняется между вызовами активности.
        /// Используется только в режиме Counter.
        /// Для сброса перезапустите процесс Primo RPA.
        /// </summary>
        // Счётчик хранится в RepoDict.IntDict по ключу Prop_CounterKey.
        // Поля экземпляра не нужны — состояние живёт в глобальном репозитории.

        // =========================================================================
        // INPUT: ОСНОВНЫЕ
        // =========================================================================

        private GeneratorType _type = GeneratorType.Guid;
        /// <summary>Тип генерируемого значения. Определяет активный набор параметров.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_GenerationType)]
        public GeneratorType Type
        {
            get => _type;
            set { _type = value; InvokePropertyChanged(this, "Type"); }
        }

        // =========================================================================
        // INPUT: GUID (GeneratorType.Guid)
        // =========================================================================

        private GuidFormat _propGuidFormat = GuidFormat.D;
        /// <summary>
        /// Формат строкового представления GUID.
        /// D — стандартный с дефисами (рекомендуется).
        /// N — без дефисов (компактный, для URL).
        /// B/P — со скобками (для конфигов).
        /// X — C-style (для кода).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Guid), System.ComponentModel.DisplayName(ActivityStrings.Field_GuidFormat)]
        public GuidFormat Prop_GuidFormat
        {
            get => _propGuidFormat;
            set { _propGuidFormat = value; InvokePropertyChanged(this, "Prop_GuidFormat"); }
        }

        // =========================================================================
        // INPUT: ИМЯ ФАЙЛА (GeneratorType.FileName)
        // =========================================================================

        private string _propDirectory;
        /// <summary>
        /// Путь к директории где будет создан файл.
        /// Если директория не указана — возвращается только имя файла без пути.
        /// Пример: C:\Reports\Output
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_FileName), System.ComponentModel.DisplayName(ActivityStrings.Field_Directory)]
        public string Prop_Directory
        {
            get => _propDirectory;
            set { _propDirectory = value; InvokePropertyChanged(this, "Prop_Directory"); }
        }

        private string _propBaseName;
        /// <summary>
        /// Базовое имя файла без расширения.
        /// Если файл с таким именем уже существует — автоматически добавляется счётчик:
        /// report.xlsx → report(1).xlsx → report(2).xlsx
        /// Пример: "report", "export_2025", "invoice"
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_FileName), System.ComponentModel.DisplayName(ActivityStrings.Field_BaseName)]
        public string Prop_BaseName
        {
            get => _propBaseName;
            set { _propBaseName = value; InvokePropertyChanged(this, "Prop_BaseName"); }
        }

        private string _propExtension;
        /// <summary>
        /// Расширение файла. Точка добавляется автоматически если отсутствует.
        /// Примеры: ".xlsx", ".pdf", "txt", ".log"
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_FileName), System.ComponentModel.DisplayName(ActivityStrings.Field_Extension)]
        public string Prop_Extension
        {
            get => _propExtension;
            set { _propExtension = value; InvokePropertyChanged(this, "Prop_Extension"); }
        }

        // =========================================================================
        // INPUT: СЛУЧАЙНОЕ ЧИСЛО (GeneratorType.RandomNumber)
        // =========================================================================

        private string _propMinValue;
        /// <summary>
        /// Минимальное значение диапазона (включительно).
        /// Значение по умолчанию: 0.
        /// Должно быть меньше или равно Prop_MaxValue.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_RandomNumber), System.ComponentModel.DisplayName(ActivityStrings.Field_MinValue)]
        public string Prop_MinValue
        {
            get => _propMinValue;
            set { _propMinValue = value; InvokePropertyChanged(this, "Prop_MinValue"); }
        }

        private string _propMaxValue;
        /// <summary>
        /// Максимальное значение диапазона (включительно).
        /// Значение по умолчанию: 100.
        /// Должно быть больше или равно Prop_MinValue.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_RandomNumber), System.ComponentModel.DisplayName(ActivityStrings.Field_MaxValue)]
        public string Prop_MaxValue
        {
            get => _propMaxValue;
            set { _propMaxValue = value; InvokePropertyChanged(this, "Prop_MaxValue"); }
        }

        // =========================================================================
        // INPUT: ВРЕМЕННАЯ МЕТКА (GeneratorType.Timestamp)
        // =========================================================================

        private TimestampFormat _propTimestampFormat = TimestampFormat.yyyyMMdd_HHmmss;
        /// <summary>
        /// Предустановленный формат даты/времени.
        /// При выборе Custom используется значение Prop_CustomTimestampFormat.
        /// Форматы сгруппированы по назначению:
        ///   Компактные    — для имён файлов: yyyyMMdd_HHmmss, yyyyMMdd_HHmm, yyyyMMdd
        ///   ISO 8601      — для БД и API: yyyy-MM-dd, yyyy-MM-ddTHH:mm:ss
        ///   Российские    — для отчётов: dd.MM.yyyy, dd.MM.yyyy HH:mm:ss
        ///   Американские  — для интеграций: MM/dd/yyyy
        ///   Только время  — HH:mm:ss, HH:mm
        ///   Специальные   — Quarter, WeekNumber, UnixTimestamp
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Timestamp), System.ComponentModel.DisplayName(ActivityStrings.Field_TimestampFormatField)]
        public TimestampFormat Prop_TimestampFormat
        {
            get => _propTimestampFormat;
            set { _propTimestampFormat = value; InvokePropertyChanged(this, "Prop_TimestampFormat"); }
        }

        private string _propCustomTimestampFormat;
        /// <summary>
        /// Пользовательский формат даты — используется только при Prop_TimestampFormat = Custom.
        /// Стандартные спецификаторы C# DateTime.ToString():
        ///   yyyy — год 4 цифры,  yy — год 2 цифры
        ///   MM   — месяц (01-12), MMM — сокр. название (янв), MMMM — полное (январь)
        ///   dd   — день (01-31),  ddd — сокр. (Пн),           dddd — полный (Понедельник)
        ///   HH   — часы 24ч,     hh — часы 12ч
        ///   mm   — минуты,       ss — секунды,                fff — миллисекунды
        ///   tt   — AM/PM
        /// Пример: "dd.MM.yyyy HH:mm:ss" → "15.02.2025 14:30:45"
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Timestamp), System.ComponentModel.DisplayName(ActivityStrings.Field_CustomTimestampFormat)]
        public string Prop_CustomTimestampFormat
        {
            get => _propCustomTimestampFormat;
            set { _propCustomTimestampFormat = value; InvokePropertyChanged(this, "Prop_CustomTimestampFormat"); }
        }

        // =========================================================================
        // INPUT: СЧЁТЧИК (GeneratorType.Counter)
        // =========================================================================

        private string _propCounterKey;
        /// <summary>
        /// Ключ счётчика в RepoDict.IntDict.
        /// Позволяет иметь несколько независимых счётчиков в одном проекте:
        /// каждая активность с разным ключом ведёт свой счёт.
        /// Если счётчик с таким ключом уже существует в RepoDict.IntDict —
        /// начальное значение игнорируется, используется только шаг.
        /// Примеры: "Invoice", "OrderNum", "RowIndex"
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Counter), System.ComponentModel.DisplayName(ActivityStrings.Field_CounterKey)]
        public string Prop_CounterKey
        {
            get => _propCounterKey;
            set { _propCounterKey = value; InvokePropertyChanged(this, "Prop_CounterKey"); }
        }

        private string _propCounterStart;
        /// <summary>
        /// Начальное значение счётчика.
        /// Применяется ТОЛЬКО при первом обращении к данному ключу в RepoDict.IntDict.
        /// Если счётчик с таким ключом уже есть в RepoDict — это значение игнорируется.
        /// Для сброса счётчика используйте активность «Сбросить счётчик» или
        /// вручную удалите ключ из RepoDict.IntDict в начале процесса.
        /// По умолчанию: 1.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Counter), System.ComponentModel.DisplayName(ActivityStrings.Field_CounterStart)]
        public string Prop_CounterStart
        {
            get => _propCounterStart;
            set { _propCounterStart = value; InvokePropertyChanged(this, "Prop_CounterStart"); }
        }

        private string _propCounterStep;
        /// <summary>
        /// Шаг увеличения счётчика при каждом вызове.
        /// По умолчанию: 1 (счёт 1, 2, 3...).
        /// Отрицательный шаг — убывающий счётчик (10, 9, 8...).
        /// Шаг 10 — счёт десятками (10, 20, 30...).
        /// Применяется при каждом вызове независимо от того первый он или нет.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Counter), System.ComponentModel.DisplayName(ActivityStrings.Field_CounterStep)]
        public string Prop_CounterStep
        {
            get => _propCounterStep;
            set { _propCounterStep = value; InvokePropertyChanged(this, "Prop_CounterStep"); }
        }

        // =========================================================================
        // INPUT: ХЕШ-ID (GeneratorType.HashId)
        // =========================================================================

        private string _propInputString;
        /// <summary>
        /// Входная строка для хеширования через SHA-256.
        /// Одинаковая строка → всегда одинаковый хеш (детерминированность).
        /// Разные строки → разные хеши (уникальность).
        /// Используйте для создания воспроизводимых ID из известных данных:
        /// например, хеш от "ИНН + дата" как уникальный ключ документа.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_HashId), System.ComponentModel.DisplayName(ActivityStrings.Field_InputString)]
        public string Prop_InputString
        {
            get => _propInputString;
            set { _propInputString = value; InvokePropertyChanged(this, "Prop_InputString"); }
        }

        private string _propHashPrefix;
        /// <summary>
        /// Необязательный префикс добавляемый перед хешем.
        /// Удобен для визуальной категоризации ID.
        /// Пример: "DOC-" → "DOC-a3f5b2c1..."
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_HashId), System.ComponentModel.DisplayName(ActivityStrings.Field_HashPrefix)]
        public string Prop_HashPrefix
        {
            get => _propHashPrefix;
            set { _propHashPrefix = value; InvokePropertyChanged(this, "Prop_HashPrefix"); }
        }

        // =========================================================================
        // INPUT: ИМЯ ПОЛЬЗОВАТЕЛЯ (GeneratorType.Username)
        // =========================================================================

        private string _propFirstName;
        /// <summary>
        /// Имя пользователя (транслитерация или латиница).
        /// Автоматически приводится к нижнему регистру.
        /// Пример: "Ivan" → "ivan"
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Username), System.ComponentModel.DisplayName(ActivityStrings.Field_FirstName)]
        public string Prop_FirstName
        {
            get => _propFirstName;
            set { _propFirstName = value; InvokePropertyChanged(this, "Prop_FirstName"); }
        }

        private string _propLastName;
        /// <summary>
        /// Фамилия пользователя (транслитерация или латиница).
        /// Автоматически приводится к нижнему регистру.
        /// Пример: "Petrov" → "petrov"
        /// Результат: ivan.petrov@example.com
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Username), System.ComponentModel.DisplayName(ActivityStrings.Field_LastName)]
        public string Prop_LastName
        {
            get => _propLastName;
            set { _propLastName = value; InvokePropertyChanged(this, "Prop_LastName"); }
        }

        private string _propDomain;
        /// <summary>
        /// Домен для формирования email-адреса.
        /// Не включает символ @  — он добавляется автоматически.
        /// По умолчанию: "example.com"
        /// Примеры: "company.ru", "corp.internal", "gmail.com"
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Username), System.ComponentModel.DisplayName(ActivityStrings.Field_Domain)]
        public string Prop_Domain
        {
            get => _propDomain;
            set { _propDomain = value; InvokePropertyChanged(this, "Prop_Domain"); }
        }

        // =========================================================================
        // INPUT: ШАБЛОН (GeneratorType.Template)
        // =========================================================================

        private string _propTemplate;
        /// <summary>
        /// Строка-шаблон с плейсхолдерами в формате {ключ}.
        /// Плейсхолдеры заменяются значениями из словаря Prop_Variables.
        /// Если ключ не найден в словаре — плейсхолдер остаётся без изменений.
        /// Пример: "Привет, {name}! Ваш номер заказа: {orderId}"
        /// Результат: "Привет, Иван! Ваш номер заказа: ORD-12345"
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Template), System.ComponentModel.DisplayName(ActivityStrings.Field_Template)]
        public string Prop_Template
        {
            get => _propTemplate;
            set { _propTemplate = value; InvokePropertyChanged(this, "Prop_Template"); }
        }

        private string _propVariables;
        /// <summary>
        /// Словарь Dictionary&lt;string, object&gt; с заменами для шаблона.
        /// Ключи соответствуют плейсхолдерам {key} в шаблоне.
        /// Значения преобразуются в строку через ToString().
        /// Пример: { "name": "Иван", "orderId": "ORD-12345", "date": DateTime.Today }
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, object>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Template), System.ComponentModel.DisplayName(ActivityStrings.Field_Variables)]
        public string Prop_Variables
        {
            get => _propVariables;
            set { _propVariables = value; InvokePropertyChanged(this, "Prop_Variables"); }
        }

        // =========================================================================
        // OUTPUT
        // =========================================================================

        private string _propOutputVariable;
        /// <summary>
        /// Выходная переменная — строка с результатом генерации.
        /// Все типы генераторов возвращают строку.
        /// Числовые результаты (RandomNumber, Counter) также возвращаются как строка.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_OutputVariable)]
        public string Prop_OutputVariable
        {
            get => _propOutputVariable;
            set { _propOutputVariable = value; InvokePropertyChanged(this, "Prop_OutputVariable"); }
        }

        // =========================================================================
        // КОНСТРУКТОР
        // =========================================================================

        /// <summary>
        /// Инициализация компонента: имя, справка, иконка, список свойств, значения по умолчанию.
        /// </summary>
        public GeneratorsBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Генераторы";
            sdkComponentHelp =
                "Компонент «Генераторы» — восемь генераторов в одной активности.\n" +
                "Результат всегда строка, записывается в поле «Результат».\n\n" +
                "── Типы генерации ─────────────────────────────────────────────\n" +
                "GUID         — уникальный идентификатор UUID.\n" +
                "              Форматы: N (без дефисов), D (с дефисами), B ({...}),\n" +
                "              P ((...)), X (C-style с 0x).\n\n" +
                "Имя файла    — уникальное имя файла в директории.\n" +
                "              Если файл существует — добавляется (1), (2) и т.д.\n" +
                "              Параметры: Директория, Базовое имя*, Расширение.\n\n" +
                "Случайное число — целое число в диапазоне [Мин, Макс] включительно.\n\n" +
                "Временная метка — текущая дата/время в выбранном формате.\n" +
                "              Компактные (для файлов): yyyyMMdd_HHmmss, yyyyMMdd...\n" +
                "              ISO 8601 (для БД/API): yyyy-MM-dd, ISO8601...\n" +
                "              Российские (для отчётов): dd.MM.yyyy, dd.MM.yyyy HH:mm:ss\n" +
                "              Американские: MM/dd/yyyy, MM/dd/yyyy HH:mm:ss\n" +
                "              Только время: HH:mm:ss, HH:mm, HH:mm:ss.fff\n" +
                "              Специальные: Quarter (Q1-2025), WeekNumber (W07-2025),\n" +
                "                           UnixTimestamp (секунды с 01.01.1970 UTC)\n" +
                "              Custom: собственный формат C# DateTime.\n\n" +
                "Счётчик      — последовательное число сохраняемое в RepoDict.IntDict.\n" +
                "              Ключ счётчика — уникальное имя в RepoDict (например Invoice).\n" +
                "              Разные ключи = независимые счётчики в одном процессе.\n" +
                "              Начальное значение применяется ТОЛЬКО при первом вызове\n" +
                "              (когда ключ отсутствует в RepoDict.IntDict).\n" +
                "              При повторных вызовах используется только шаг.\n" +
                "              Для сброса: RepoDict.IntDict.Remove(\"ключ\") в начале процесса.\n\n" +
                "Хеш ID      — SHA-256 от входной строки → воспроизводимый хеш-ID.\n" +
                "              Одна строка = всегда один хеш. Опциональный Префикс.\n\n" +
                "Имя пользователя — email: имя.фамилия@домен (нижний регистр).\n\n" +
                "Шаблон       — заполнение {плейсхолдеров} из словаря переменных.\n" +
                "              Пример: 'Привет, {name}!' + {name: 'Иван'} → 'Привет, Иван!'";

            sdkComponentIcon = ActivityIcons.Generator;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                // Тип генерации
                PropertyBuilder.Enum<GeneratorType>("Type", "Выбор типа генерации"),

                // GUID
                PropertyBuilder.Enum<GuidFormat>("Prop_GuidFormat", "N=без дефисов, D=стандарт, B={...}, P=(...), X=C-style"),

                // Имя файла
                PropertyBuilder.FolderSelector("Prop_Directory", "Директория для проверки уникальности имени файла"),
                PropertyBuilder.Script<string>("Prop_BaseName", "Базовое имя файла без расширения (например: report)"),
                PropertyBuilder.Script<string>("Prop_Extension", "Расширение файла (например: .xlsx, .pdf, txt)"),

                // Случайное число
                PropertyBuilder.Script<int>("Prop_MinValue", "Минимальное значение диапазона (включительно)"),
                PropertyBuilder.Script<int>("Prop_MaxValue", "Максимальное значение диапазона (включительно)"),

                // Временная метка
                PropertyBuilder.Enum<TimestampFormat>("Prop_TimestampFormat", "Предустановленный формат даты/времени. Custom — использует Кастомный формат"),
                PropertyBuilder.Script<string>("Prop_CustomTimestampFormat", "Формат C# DateTime (только при выборе Custom). Пример: dd.MM.yyyy HH:mm:ss"),

                // Счётчик
                PropertyBuilder.Script<string>("Prop_CounterKey", "Ключ в RepoDict.IntDict. Разные ключи = независимые счётчики"),
                PropertyBuilder.Script<int>("Prop_CounterStart", "Начальное значение (только при первом вызове для данного ключа)"),
                PropertyBuilder.Script<int>("Prop_CounterStep", "Шаг (1=стандартный, 10=десятки, -1=убывающий)"),

                // Хеш ID
                PropertyBuilder.Script<string>("Prop_InputString", "Строка для вычисления SHA-256 хеша"),
                PropertyBuilder.Script<string>("Prop_HashPrefix", "Префикс перед хешем (необязательно). Пример: DOC-"),

                // Имя пользователя
                PropertyBuilder.Script<string>("Prop_FirstName", "Имя пользователя (латиница или транслит)"),
                PropertyBuilder.Script<string>("Prop_LastName", "Фамилия пользователя (латиница или транслит)"),
                PropertyBuilder.Script<string>("Prop_Domain", "Домен email без @. Пример: company.ru"),

                // Шаблон
                PropertyBuilder.Script<string>("Prop_Template", "Шаблон с {плейсхолдерами}. Пример: Привет, {name}!"),
                PropertyBuilder.Script<Dictionary<string, object>>("Prop_Variables", "Dictionary<string,object> с заменами для шаблона"),

                // Выход
                PropertyBuilder.Variable<string>("Prop_OutputVariable", "Переменная для записи результата")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_Extension = "\".txt\"";
            this.Prop_Domain = "\"example.com\"";
            this.Prop_CustomTimestampFormat = "\"dd.MM.yyyy HH:mm:ss\"";
            this.Prop_MinValue = "0";
            this.Prop_MaxValue = "100";
            this.Prop_CounterKey = "\"Counter\"";
            this.Prop_CounterStart = "1";
            this.Prop_CounterStep = "1";
        }

        // =========================================================================
        // ОСНОВНОЕ ДЕЙСТВИЕ
        // =========================================================================

        /// <summary>
        /// Точка входа активности. Делегирует выполнение нужному генератору
        /// в зависимости от значения свойства Type.
        /// </summary>
        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                string result;

                switch (this.Type)
                {
                    case GeneratorType.Guid:
                        result = GenerateGuid();
                        break;
                    case GeneratorType.FileName:
                        result = GenerateFileName(sd);
                        break;
                    case GeneratorType.RandomNumber:
                        result = GenerateRandomNumber(sd);
                        break;
                    case GeneratorType.Timestamp:
                        result = GenerateTimestamp(sd);
                        break;
                    case GeneratorType.Counter:
                        result = GenerateCounter(sd);
                        break;
                    case GeneratorType.HashId:
                        result = GenerateHashId(sd);
                        break;
                    case GeneratorType.Username:
                        result = GenerateUsername(sd);
                        break;
                    case GeneratorType.Template:
                        result = GenerateTemplate(sd);
                        break;
                    default:
                        throw new InvalidOperationException($"Неизвестный тип генерации: {this.Type}");
                }

                SetVariableValue(this.Prop_OutputVariable, result, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[{this.Type}] Сгенерировано: {result}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка генерации [{this.Type}]: {ex.Message}"
                };
            }
        }

        // =========================================================================
        // ГЕНЕРАТОРЫ — РЕАЛИЗАЦИЯ
        // =========================================================================

        /// <summary>
        /// Генерирует новый GUID и форматирует его согласно Prop_GuidFormat.
        /// Все форматы криптографически уникальны — отличается только запись.
        /// </summary>
        private string GenerateGuid()
        {
            var guid = Guid.NewGuid();

            switch (this.Prop_GuidFormat)
            {
                case GuidFormat.N: return guid.ToString("N"); // 32 цифры без дефисов
                case GuidFormat.B: return guid.ToString("B"); // {с фигурными скобками}
                case GuidFormat.P: return guid.ToString("P"); // (с круглыми скобками)
                case GuidFormat.X: return guid.ToString("X"); // {0x...,0x...,{0x...}}
                case GuidFormat.D:
                default: return guid.ToString("D"); // стандарт с дефисами
            }
        }

        /// <summary>
        /// Генерирует уникальное полное имя файла.
        /// Если файл с базовым именем уже существует в директории —
        /// автоматически добавляет числовой суффикс: file(1).txt, file(2).txt...
        /// </summary>
        private string GenerateFileName(ScriptingData sd)
        {
            string dir = GetPropertyValue<string>(this.Prop_Directory, "Prop_Directory", sd) ?? string.Empty;
            string baseName = GetPropertyValue<string>(this.Prop_BaseName, "Prop_BaseName", sd) ?? "file";
            string ext = GetPropertyValue<string>(this.Prop_Extension, "Prop_Extension", sd) ?? ".txt";

            // Нормализуем расширение: добавляем точку если отсутствует
            if (!string.IsNullOrEmpty(ext) && !ext.StartsWith("."))
                ext = "." + ext;

            // Если директория не указана — работаем только с именем файла
            string fullPath = string.IsNullOrEmpty(dir)
                ? baseName + ext
                : Path.Combine(dir, baseName + ext);

            return FileHelper.GenerateUniqueFilePath(
    Path.GetDirectoryName(fullPath),
    Path.GetFileNameWithoutExtension(fullPath),
    Path.GetExtension(fullPath));
        }

        /// <summary>
        /// Генерирует случайное целое число в диапазоне [Min, Max] включительно.
        /// Нечисловые значения в полях Min/Max заменяются значениями по умолчанию (0 и 100).
        /// </summary>
        private string GenerateRandomNumber(ScriptingData sd)
        {
            string minStr = GetPropertyValue<string>(this.Prop_MinValue, "Prop_MinValue", sd) ?? "0";
            string maxStr = GetPropertyValue<string>(this.Prop_MaxValue, "Prop_MaxValue", sd) ?? "100";

            // При ошибке парсинга используем значения по умолчанию
            if (!int.TryParse(minStr, out int min)) min = 0;
            if (!int.TryParse(maxStr, out int max)) max = 100;

            if (min > max)
                throw new ArgumentException($"Минимальное ({min}) не может быть больше максимального ({max})");

            // Каждый вызов создаёт новый Random — достаточно для большинства сценариев.
            // Для криптографической случайности используйте режим HashId с GUID.
            return new Random().Next(min, max + 1).ToString();
        }

        /// <summary>
        /// Форматирует текущую дату/время согласно выбранному Prop_TimestampFormat.
        /// Специальные форматы (Quarter, WeekNumber, UnixTimestamp) вычисляются вручную.
        /// При Custom используется строка из Prop_CustomTimestampFormat.
        /// </summary>
        private string GenerateTimestamp(ScriptingData sd)
        {
            DateTime now = DateTime.Now;

            switch (this.Prop_TimestampFormat)
            {
                // ── Компактные (для имён файлов) ──────────────────────────────────
                case TimestampFormat.yyyyMMdd_HHmmss: return now.ToString("yyyyMMdd_HHmmss");
                case TimestampFormat.yyyyMMdd_HHmm: return now.ToString("yyyyMMdd_HHmm");
                case TimestampFormat.yyyyMMdd: return now.ToString("yyyyMMdd");
                case TimestampFormat.yyyyMM: return now.ToString("yyyyMM");
                case TimestampFormat.yyyyMMddHHmmssfff: return now.ToString("yyyyMMddHHmmssfff");

                // ── ISO 8601 (для БД и API) ────────────────────────────────────────
                case TimestampFormat.yyyy_MM_dd: return now.ToString("yyyy-MM-dd");
                case TimestampFormat.yyyy_MM_dd_HH_mm_ss: return now.ToString("yyyy-MM-dd HH:mm:ss");
                case TimestampFormat.ISO8601: return now.ToString("yyyy-MM-ddTHH:mm:ss");
                case TimestampFormat.ISO8601_ms: return now.ToString("yyyy-MM-ddTHH:mm:ss.fff");

                // ── Российские (для отчётов) ───────────────────────────────────────
                case TimestampFormat.dd_MM_yyyy: return now.ToString("dd.MM.yyyy");
                case TimestampFormat.dd_MM_yyyy_HH_mm_ss: return now.ToString("dd.MM.yyyy HH:mm:ss");
                case TimestampFormat.dd_MM_yyyy_HH_mm: return now.ToString("dd.MM.yyyy HH:mm");
                case TimestampFormat.dd_MM_yyyy_slash: return now.ToString("dd/MM/yyyy");

                // ── Американские ──────────────────────────────────────────────────
                case TimestampFormat.MM_dd_yyyy: return now.ToString("MM/dd/yyyy");
                case TimestampFormat.MM_dd_yyyy_HH_mm_ss: return now.ToString("MM/dd/yyyy HH:mm:ss");

                // ── Только время ──────────────────────────────────────────────────
                case TimestampFormat.HHmmss: return now.ToString("HH:mm:ss");
                case TimestampFormat.HHmm: return now.ToString("HH:mm");
                case TimestampFormat.HHmmss_fff: return now.ToString("HH:mm:ss.fff");

                // ── Специальные ───────────────────────────────────────────────────
                case TimestampFormat.FullDate:
                    // Полная дата с днём недели — зависит от культуры системы
                    return now.ToString("D");

                case TimestampFormat.Quarter:
                    // Q1-2025, Q2-2025, Q3-2025, Q4-2025
                    int quarter = (now.Month - 1) / 3 + 1;
                    return $"Q{quarter}-{now.Year}";

                case TimestampFormat.WeekNumber:
                    // W07-2025 — номер недели по ISO 8601 (понедельник = начало недели)
                    int weekNum = System.Globalization.CultureInfo.CurrentCulture.Calendar
                        .GetWeekOfYear(now,
                            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                            DayOfWeek.Monday);
                    return $"W{weekNum:D2}-{now.Year}";

                case TimestampFormat.UnixTimestamp:
                    // Секунды с 01.01.1970 00:00:00 UTC
                    long unixSeconds = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                    return unixSeconds.ToString();

                case TimestampFormat.Custom:
                    string customFmt = GetPropertyValue<string>(
                        this.Prop_CustomTimestampFormat, "Prop_CustomTimestampFormat", sd) ?? "yyyy-MM-dd";
                    if (string.IsNullOrWhiteSpace(customFmt))
                        throw new ArgumentException("Кастомный формат даты не может быть пустым при выборе Custom");
                    return now.ToString(customFmt);

                default:
                    return now.ToString("yyyyMMdd_HHmmss");
            }
        }

        /// <summary>
        /// Возвращает текущее значение счётчика из RepoDict.IntDict и сдвигает его на шаг.
        ///
        /// Логика работы:
        ///   1. Ключ берётся из Prop_CounterKey (например "Invoice", "OrderNum").
        ///   2. Если ключ отсутствует в RepoDict.IntDict — счётчик инициализируется
        ///      значением Prop_CounterStart и сразу записывается в RepoDict.IntDict.
        ///   3. Если ключ уже есть в RepoDict.IntDict — начальное значение игнорируется,
        ///      берётся текущее значение из словаря.
        ///   4. Текущее значение возвращается, затем счётчик сдвигается на Prop_CounterStep
        ///      и записывается обратно в RepoDict.IntDict.
        ///
        /// Пример при start=10, step=3, key="Invoice":
        ///   Вызов 1 → RepoDict.IntDict["Invoice"] отсутствует → вернёт 10, запишет 13
        ///   Вызов 2 → RepoDict.IntDict["Invoice"] = 13       → вернёт 13, запишет 16
        ///   Вызов 3 → RepoDict.IntDict["Invoice"] = 16       → вернёт 16, запишет 19
        ///
        /// Для сброса счётчика в начале процесса добавьте:
        ///   RepoDict.IntDict.Remove("Invoice")  — удалить ключ
        ///   RepoDict.IntDict["Invoice"] = 1     — установить произвольное значение
        /// </summary>
        private string GenerateCounter(ScriptingData sd)
        {
            // ── Читаем и нормализуем параметры ────────────────────────────────

            // Ключ счётчика в RepoDict.IntDict
            string key = (GetPropertyValue<string>(this.Prop_CounterKey, "Prop_CounterKey", sd)
                          ?? "Counter").Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(key)) key = "Counter";

            // Начальное значение — применяется только при первом обращении к ключу
            string rawStart = (GetPropertyValue<string>(this.Prop_CounterStart, "Prop_CounterStart", sd)
                               ?? "1").Trim().Trim('"');

            // Шаг — применяется при каждом вызове
            string rawStep = (GetPropertyValue<string>(this.Prop_CounterStep, "Prop_CounterStep", sd)
                              ?? "1").Trim().Trim('"');

            if (!int.TryParse(rawStart, out int start)) start = 1;
            if (!int.TryParse(rawStep, out int step)) step = 1;

            // ── Читаем текущее значение из RepoDict ───────────────────────────

            int current;

            if (RepoDict.IntDict.ContainsKey(key))
            {
                // Ключ уже существует — берём текущее значение, начальное игнорируем
                current = RepoDict.IntDict[key];
            }
            else
            {
                // Первый вызов для данного ключа — инициализируем начальным значением
                current = start;
            }

            // ── Записываем следующее значение в RepoDict ──────────────────────

            // Следующее значение = текущее + шаг
            RepoDict.IntDict[key] = current + step;

            // Возвращаем текущее значение (до сдвига)
            return current.ToString();
        }

        /// <summary>
        /// Вычисляет SHA-256 хеш от входной строки и возвращает его в виде
        /// шестнадцатеричной строки (64 символа) с необязательным префиксом.
        /// Детерминирован: одинаковый вход → всегда одинаковый выход.
        /// </summary>
        private string GenerateHashId(ScriptingData sd)
        {
            string input = GetPropertyValue<string>(this.Prop_InputString, "Prop_InputString", sd) ?? string.Empty;
            string prefix = GetPropertyValue<string>(this.Prop_HashPrefix, "Prop_HashPrefix", sd) ?? string.Empty;

            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Собираем hex-строку через StringBuilder — эффективнее чем конкатенация
                var sb = new StringBuilder(hashBytes.Length * 2);
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2"));

                return prefix + sb.ToString();
            }
        }

        /// <summary>
        /// Формирует email-адрес в формате: имя.фамилия@домен.
        /// Всё приводится к нижнему регистру для соответствия стандартам email.
        /// </summary>
        private string GenerateUsername(ScriptingData sd)
        {
            string firstName = GetPropertyValue<string>(this.Prop_FirstName, "Prop_FirstName", sd) ?? string.Empty;
            string lastName = GetPropertyValue<string>(this.Prop_LastName, "Prop_LastName", sd) ?? string.Empty;
            string domain = GetPropertyValue<string>(this.Prop_Domain, "Prop_Domain", sd) ?? "example.com";

            // Формат: имя.фамилия@домен — всё в нижнем регистре
            return $"{firstName.ToLower()}.{lastName.ToLower()}@{domain.ToLower()}";
        }

        /// <summary>
        /// Заполняет шаблонную строку значениями из словаря.
        /// Плейсхолдеры вида {key} заменяются значением variables[key].ToString().
        /// Нераспознанные плейсхолдеры остаются без изменений.
        /// Реализация через Regex.Replace — обрабатывает все плейсхолдеры за один проход.
        /// </summary>
        private string GenerateTemplate(ScriptingData sd)
        {
            string template = GetPropertyValue<string>(this.Prop_Template, "Prop_Template", sd) ?? string.Empty;
            var variables = GetPropertyValue<Dictionary<string, object>>(
                this.Prop_Variables, "Prop_Variables", sd)
                ?? new Dictionary<string, object>();

            // Регулярное выражение захватывает имя ключа из {key}
            return Regex.Replace(template, @"\{(\w+)\}", match =>
            {
                string key = match.Groups[1].Value;
                // Если ключ найден — подставляем значение, иначе оставляем {key} как есть
                return variables.ContainsKey(key)
                    ? variables[key]?.ToString() ?? string.Empty
                    : match.Value;
            });
        }



        // =========================================================================
        // ВАЛИДАЦИЯ
        // =========================================================================

        /// <summary>
        /// Проверяет корректность заполнения свойств в дизайнере.
        /// Проверяет только обязательные поля для выбранного типа генератора.
        /// </summary>
        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            // Выходная переменная обязательна для всех режимов
            ret.ValidateRequired(this.Prop_OutputVariable, ActivityStrings.Field_OutputVariable, "Выходная переменная обязательна");

            // Валидация специфичная для режима
            switch (this.Type)
            {
                case GeneratorType.FileName:
                    ret.ValidateRequired(this.Prop_BaseName, ActivityStrings.Field_BaseName, ActivityStrings.Error_BaseFileNameRequired);
                    break;

                case GeneratorType.HashId:
                    ret.ValidateRequired(this.Prop_InputString, ActivityStrings.Field_InputString, ActivityStrings.Error_InputStringRequired);
                    break;

                case GeneratorType.Template:
                    ret.ValidateRequired(this.Prop_Template, ActivityStrings.Field_Template, ActivityStrings.Error_TemplateRequired);
                    break;

                case GeneratorType.Timestamp:
                    if (this.Prop_TimestampFormat == TimestampFormat.Custom)
                        ret.ValidateRequired(this.Prop_CustomTimestampFormat, ActivityStrings.Field_CustomTimestampFormat, "Формат даты обязателен при выборе Custom");
                    break;
            }

            return ret;
        }
    }
}
