// =============================================================================
// TextTranslitBack.cs — активность «Текст: Транслитерация».
//
// Преобразует текст между кириллицей и латиницей по выбранной схеме.
//
// Поддерживаемые схемы (TranslitScheme):
//   GOST7792000  — ГОСТ 7.79-2000 сист. Б (официальный стандарт РФ)
//   Passport2013 — Загранпаспорта РФ с 2013 года (Приказ МВД №827)
//   ICAOPassport — ИКАО Doc 9303 (авиабилеты, машиносчитываемые документы)
//   ISO9         — ISO 9:1995 (однозначная обратимая транслитерация)
//   BGN_PCGN     — BGN/PCGN 1947 (английские географические названия)
//   Simplified   — Упрощённая ASCII (логины, имена файлов, URL)
//
// Направления (TranslitDirection):
//   CyrillicToLatin — Кириллица → Латиница
//   LatinToCyrillic — Латиница → Кириллица
//   AutoDetect      — Определить по большинству символов
//
// Постобработка:
//   PreserveCase      — сохранить регистр оригинала
//   PreserveNonAlpha  — оставить нетранслитерируемые символы
//   ToUpperCase       — привести к верхнему регистру
//   ToLowerCase       — привести к нижнему регистру
//   ReplaceSpaces     — заменить пробелы указанным символом
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LTools.Common.Model;
using LTools.Common.Model.Serialization;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Текст: Транслитерация».
    /// Преобразует текст между кириллицей и латиницей по выбранной схеме.
    /// </summary>
    public class TextTranslitBack : PrimoComponentTO<TextTranslit>
    {
        // =====================================================================
        // Таблицы транслитерации — Кириллица → Латиница
        // Все таблицы содержат только строчные буквы.
        // Заглавные восстанавливаются через PreserveCase в методе Transliterate.
        // =====================================================================

        /// <summary>ГОСТ 7.79-2000, система Б — без диакритических знаков.</summary>
        private static readonly Dictionary<char, string> GOST7792000Map =
            new Dictionary<char, string>
            {
                { 'а', "a"   }, { 'б', "b"   }, { 'в', "v"   }, { 'г', "g"   },
                { 'д', "d"   }, { 'е', "e"   }, { 'ё', "yo"  }, { 'ж', "zh"  },
                { 'з', "z"   }, { 'и', "i"   }, { 'й', "j"   }, { 'к', "k"   },
                { 'л', "l"   }, { 'м', "m"   }, { 'н', "n"   }, { 'о', "o"   },
                { 'п', "p"   }, { 'р', "r"   }, { 'с', "s"   }, { 'т', "t"   },
                { 'у', "u"   }, { 'ф', "f"   }, { 'х', "kh"  }, { 'ц', "cz"  },
                { 'ч', "ch"  }, { 'ш', "sh"  }, { 'щ', "shh" }, { 'ъ', "``"  },
                { 'ы', "y`"  }, { 'ь', "`"   }, { 'э', "e`"  }, { 'ю', "yu"  },
                { 'я', "ya"  }
            };

        /// <summary>Загранпаспорта РФ с 2013 года (Приказ МВД России №827).</summary>
        private static readonly Dictionary<char, string> Passport2013Map =
            new Dictionary<char, string>
            {
                { 'а', "a"    }, { 'б', "b"    }, { 'в', "v"    }, { 'г', "g"    },
                { 'д', "d"    }, { 'е', "e"    }, { 'ё', "e"    }, { 'ж', "zh"   },
                { 'з', "z"    }, { 'и', "i"    }, { 'й', "i"    }, { 'к', "k"    },
                { 'л', "l"    }, { 'м', "m"    }, { 'н', "n"    }, { 'о', "o"    },
                { 'п', "p"    }, { 'р', "r"    }, { 'с', "s"    }, { 'т', "t"    },
                { 'у', "u"    }, { 'ф', "f"    }, { 'х', "kh"   }, { 'ц', "ts"   },
                { 'ч', "ch"   }, { 'ш', "sh"   }, { 'щ', "shch" }, { 'ъ', ""     },
                { 'ы', "y"    }, { 'ь', ""     }, { 'э', "e"    }, { 'ю', "yu"   },
                { 'я', "ya"   }
            };

        /// <summary>ИКАО Doc 9303 — машиносчитываемые документы и авиабилеты.</summary>
        private static readonly Dictionary<char, string> ICAOMap =
            new Dictionary<char, string>
            {
                { 'а', "a"    }, { 'б', "b"    }, { 'в', "v"    }, { 'г', "g"    },
                { 'д', "d"    }, { 'е', "e"    }, { 'ё', "e"    }, { 'ж', "zh"   },
                { 'з', "z"    }, { 'и', "i"    }, { 'й', "i"    }, { 'к', "k"    },
                { 'л', "l"    }, { 'м', "m"    }, { 'н', "n"    }, { 'о', "o"    },
                { 'п', "p"    }, { 'р', "r"    }, { 'с', "s"    }, { 'т', "t"    },
                { 'у', "u"    }, { 'ф', "f"    }, { 'х', "kh"   }, { 'ц', "ts"   },
                { 'ч', "ch"   }, { 'ш', "sh"   }, { 'щ', "shch" }, { 'ъ', "ie"   },
                { 'ы', "y"    }, { 'ь', ""     }, { 'э', "e"    }, { 'ю', "iu"   },
                { 'я', "ia"   }
            };

        /// <summary>
        /// ISO 9:1995 — единственная схема с полной поддержкой обратной транслитерации.
        /// Использует ASCII-аппроксимацию вместо диакритических знаков.
        /// </summary>
        private static readonly Dictionary<char, string> ISO9Map =
            new Dictionary<char, string>
            {
                { 'а', "a"    }, { 'б', "b"    }, { 'в', "v"    }, { 'г', "g"    },
                { 'д', "d"    }, { 'е', "e"    }, { 'ё', "yo"   }, { 'ж', "zh"   },
                { 'з', "z"    }, { 'и', "i"    }, { 'й', "j"    }, { 'к', "k"    },
                { 'л', "l"    }, { 'м', "m"    }, { 'н', "n"    }, { 'о', "o"    },
                { 'п', "p"    }, { 'р', "r"    }, { 'с', "s"    }, { 'т', "t"    },
                { 'у', "u"    }, { 'ф', "f"    }, { 'х', "x"    }, { 'ц', "cz"   },
                { 'ч', "ch"   }, { 'ш', "sh"   }, { 'щ', "shh"  }, { 'ъ', "hh"   },
                { 'ы', "y"    }, { 'ь', "h"    }, { 'э', "eh"   }, { 'ю', "yu"   },
                { 'я', "ya"   }
            };

        /// <summary>
        /// Обратная таблица ISO 9 — Латиница → Кириллица.
        /// Строится один раз из ISO9Map путём инверсии.
        /// Многосимвольные замены хранятся как строки-ключи.
        /// </summary>
        private static readonly Dictionary<string, char> ISO9ReverseMap =
            new Dictionary<string, char>(StringComparer.OrdinalIgnoreCase)
            {
                { "a",   'а' }, { "b",   'б' }, { "v",   'в' }, { "g",   'г' },
                { "d",   'д' }, { "e",   'е' }, { "yo",  'ё' }, { "zh",  'ж' },
                { "z",   'з' }, { "i",   'и' }, { "j",   'й' }, { "k",   'к' },
                { "l",   'л' }, { "m",   'м' }, { "n",   'н' }, { "o",   'о' },
                { "p",   'п' }, { "r",   'р' }, { "s",   'с' }, { "t",   'т' },
                { "u",   'у' }, { "f",   'ф' }, { "x",   'х' }, { "cz",  'ц' },
                { "ch",  'ч' }, { "sh",  'ш' }, { "shh", 'щ' }, { "hh",  'ъ' },
                { "y",   'ы' }, { "h",   'ь' }, { "eh",  'э' }, { "yu",  'ю' },
                { "ya",  'я' }
            };

        /// <summary>BGN/PCGN 1947 — английские географические названия.</summary>
        private static readonly Dictionary<char, string> BGN_PCGNMap =
            new Dictionary<char, string>
            {
                { 'а', "a"    }, { 'б', "b"    }, { 'в', "v"    }, { 'г', "g"    },
                { 'д', "d"    }, { 'е', "e"    }, { 'ё', "yo"   }, { 'ж', "zh"   },
                { 'з', "z"    }, { 'и', "i"    }, { 'й', "y"    }, { 'к', "k"    },
                { 'л', "l"    }, { 'м', "m"    }, { 'н', "n"    }, { 'о', "o"    },
                { 'п', "p"    }, { 'р', "r"    }, { 'с', "s"    }, { 'т', "t"    },
                { 'у', "u"    }, { 'ф', "f"    }, { 'х', "kh"   }, { 'ц', "ts"   },
                { 'ч', "ch"   }, { 'ш', "sh"   }, { 'щ', "shch" }, { 'ъ', ""     },
                { 'ы', "y"    }, { 'ь', ""     }, { 'э', "e"    }, { 'ю', "yu"   },
                { 'я', "ya"   }
            };

        /// <summary>
        /// Упрощённая схема — только ASCII, без диакритики.
        /// Ъ и Ь удаляются полностью. Рекомендуется для логинов, файлов, URL.
        /// </summary>
        private static readonly Dictionary<char, string> SimplifiedMap =
            new Dictionary<char, string>
            {
                { 'а', "a"    }, { 'б', "b"    }, { 'в', "v"    }, { 'г', "g"    },
                { 'д', "d"    }, { 'е', "e"    }, { 'ё', "yo"   }, { 'ж', "zh"   },
                { 'з', "z"    }, { 'и', "i"    }, { 'й', "y"    }, { 'к', "k"    },
                { 'л', "l"    }, { 'м', "m"    }, { 'н', "n"    }, { 'о', "o"    },
                { 'п', "p"    }, { 'р', "r"    }, { 'с', "s"    }, { 'т', "t"    },
                { 'у', "u"    }, { 'ф', "f"    }, { 'х', "kh"   }, { 'ц', "ts"   },
                { 'ч', "ch"   }, { 'ш', "sh"   }, { 'щ', "shch" }, { 'ъ', ""     },
                { 'ы', "y"    }, { 'ь', ""     }, { 'э', "e"    }, { 'ю', "yu"   },
                { 'я', "ya"   }
            };

        /// <summary>
        /// Фабрика таблиц: схема → словарь для направления CyrillicToLatin.
        /// Используется вместо switch-case.
        /// </summary>
        private static readonly Dictionary<TranslitScheme, Dictionary<char, string>> SchemeMaps =
            new Dictionary<TranslitScheme, Dictionary<char, string>>
            {
                { TranslitScheme.GOST7792000,  GOST7792000Map  },
                { TranslitScheme.Passport2013, Passport2013Map },
                { TranslitScheme.ICAOPassport, ICAOMap         },
                { TranslitScheme.ISO9,         ISO9Map         },
                { TranslitScheme.BGN_PCGN,     BGN_PCGNMap     },
                { TranslitScheme.Simplified,   SimplifiedMap   }
            };

        // =====================================================================
        // Свойства
        // =====================================================================

        #region Prop_InputText
        private string _propInputText;
        /// <summary>Входной текст для транслитерации.</summary>
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

        #region Prop_Direction
        private TranslitDirection _propDirection = TranslitDirection.CyrillicToLatin;
        /// <summary>Направление: CyrillicToLatin / LatinToCyrillic / AutoDetect.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_TranslitDirection)]
        public TranslitDirection Prop_Direction
        {
            get => _propDirection;
            set { _propDirection = value; InvokePropertyChanged(this, nameof(Prop_Direction)); }
        }
        #endregion

        #region Prop_Scheme
        private TranslitScheme _propScheme = TranslitScheme.Simplified;
        /// <summary>Схема транслитерации.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_TranslitScheme)]
        public TranslitScheme Prop_Scheme
        {
            get => _propScheme;
            set { _propScheme = value; InvokePropertyChanged(this, nameof(Prop_Scheme)); }
        }
        #endregion

        #region Prop_PreserveCase
        private bool _propPreserveCase = true;
        /// <summary>
        /// Сохранять регистр оригинального символа в результате.
        /// При включении: "Иванов" → "Ivanov" (не "IVANOV" и не "ivanov").
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_PreserveCase)]
        public bool Prop_PreserveCase
        {
            get => _propPreserveCase;
            set { _propPreserveCase = value; InvokePropertyChanged(this, nameof(Prop_PreserveCase)); }
        }
        #endregion

        #region Prop_PreserveNonAlpha
        private bool _propPreserveNonAlpha = true;
        /// <summary>
        /// Оставлять нетранслитерируемые символы без изменений.
        /// При включении: цифры, пунктуация, пробелы — остаются.
        /// При выключении — удаляются.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_PreserveNonAlpha)]
        public bool Prop_PreserveNonAlpha
        {
            get => _propPreserveNonAlpha;
            set { _propPreserveNonAlpha = value; InvokePropertyChanged(this, nameof(Prop_PreserveNonAlpha)); }
        }
        #endregion

        #region Prop_ToUpperCase
        private bool _propToUpperCase = false;
        /// <summary>Привести весь результат к верхнему регистру после транслитерации.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ToUpperCase)]
        public bool Prop_ToUpperCase
        {
            get => _propToUpperCase;
            set { _propToUpperCase = value; InvokePropertyChanged(this, nameof(Prop_ToUpperCase)); }
        }
        #endregion

        #region Prop_ToLowerCase
        private bool _propToLowerCase = false;
        /// <summary>Привести весь результат к нижнему регистру после транслитерации.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ToLowerCase)]
        public bool Prop_ToLowerCase
        {
            get => _propToLowerCase;
            set { _propToLowerCase = value; InvokePropertyChanged(this, nameof(Prop_ToLowerCase)); }
        }
        #endregion

        #region Prop_ReplaceSpaces
        private string _propReplaceSpaces = string.Empty;
        /// <summary>
        /// Заменить пробелы в результате на указанную строку.
        /// Пусто — оставить пробелы без изменений.
        /// Примеры: "_" для имён файлов, "-" для slug, "." для логинов.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ReplaceSpaces)]
        public string Prop_ReplaceSpaces
        {
            get => _propReplaceSpaces;
            set { _propReplaceSpaces = value; InvokePropertyChanged(this, nameof(Prop_ReplaceSpaces)); }
        }
        #endregion

        #region Prop_Result (Выходной)
        private string _propResult;
        /// <summary>Имя переменной скрипта для записи транслитерированного текста.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_OutputVariable)]
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); }
        }
        #endregion

        #region Prop_DetectedDirection (Выходной)
        private string _propDetectedDirection;
        /// <summary>
        /// Имя переменной скрипта для записи определённого направления (string).
        /// Заполняется при Direction = AutoDetect.
        /// Значение: "CyrillicToLatin" или "LatinToCyrillic".
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_DetectedDirection)]
        public string Prop_DetectedDirection
        {
            get => _propDetectedDirection;
            set { _propDetectedDirection = value; InvokePropertyChanged(this, nameof(Prop_DetectedDirection)); }
        }
        #endregion

        #region Prop_ChangedChars (Выходной)
        private string _propChangedChars;
        /// <summary>
        /// Имя переменной скрипта для записи количества изменённых символов (int).
        /// Полезно для контроля — если 0, возможно не та схема или направление.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ChangedChars)]
        public string Prop_ChangedChars
        {
            get => _propChangedChars;
            set { _propChangedChars = value; InvokePropertyChanged(this, nameof(Prop_ChangedChars)); }
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

        public TextTranslitBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_TextTranslit;
            sdkComponentHelp =
                "Преобразует текст между кириллицей и латиницей.\n" +
                "\n" +
                "── Схемы транслитерации ────────────────────────\n" +
                "Simplified   — ASCII, без диакритики (логины, файлы, URL)\n" +
                "Passport2013 — Загранпаспорта РФ с 2013 года\n" +
                "ICAOPassport — Авиабилеты и машиносчитываемые документы\n" +
                "GOST7792000  — ГОСТ 7.79-2000 система Б\n" +
                "ISO9         — ISO 9:1995, обратимая транслитерация\n" +
                "BGN_PCGN     — Английские географические названия\n" +
                "\n" +
                "── Направления ─────────────────────────────────\n" +
                "CyrillicToLatin — Кириллица → Латиница\n" +
                "LatinToCyrillic — Латиница → Кириллица (только ISO9 без потерь)\n" +
                "AutoDetect      — Определить по большинству символов\n" +
                "\n" +
                "── Постобработка ───────────────────────────────\n" +
                "Сохранять регистр — \"Иванов\" → \"Ivanov\" (не \"IVANOV\")\n" +
                "Замена пробелов   — \"_\" для файлов, \".\" для логинов\n" +
                "К верхнему / нижнему регистру\n" +
                "\n" +
                "── Примеры ─────────────────────────────────────\n" +
                "\"Иванов Иван\" + Simplified + ToLower + ReplaceSpaces=\".\" → \"ivanov.ivan\"\n" +
                "\"ЩЕРБАКОВ\" + Passport2013 → \"SHCHERBAKOV\"\n" +
                "\"г. Москва\" + BGN_PCGN → \"g. Moskva\"";

            sdkComponentIcon = ActivityIcons.TextTranslit;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                // Основные
                PropertyBuilder.Script<string>("Prop_InputText",      ActivityStrings.Field_InputString),
                PropertyBuilder.Enum<TranslitDirection>("Prop_Direction", ActivityStrings.Field_TranslitDirection),
                PropertyBuilder.Enum<TranslitScheme>("Prop_Scheme",   ActivityStrings.Field_TranslitScheme),
                // Постобработка
                PropertyBuilder.BooleanObject("Prop_PreserveCase",    ActivityStrings.Field_PreserveCase),
                PropertyBuilder.BooleanObject("Prop_PreserveNonAlpha",ActivityStrings.Field_PreserveNonAlpha),
                PropertyBuilder.BooleanObject("Prop_ToUpperCase",     ActivityStrings.Field_ToUpperCase),
                PropertyBuilder.BooleanObject("Prop_ToLowerCase",     ActivityStrings.Field_ToLowerCase),
                PropertyBuilder.Script<string>("Prop_ReplaceSpaces",  ActivityStrings.Field_ReplaceSpaces),
                // Выходные
                PropertyBuilder.Variable<string>("Prop_Result",       ActivityStrings.Field_OutputVariable),
                PropertyBuilder.Variable<string>("Prop_DetectedDirection", ActivityStrings.Field_DetectedDirection),
                PropertyBuilder.Variable<int>("Prop_ChangedChars",    ActivityStrings.Field_ChangedChars)
            };

            InitClass(container);

            this.Prop_Direction        = TranslitDirection.CyrillicToLatin;
            this.Prop_Scheme           = TranslitScheme.Simplified;
            this.Prop_PreserveCase     = true;
            this.Prop_PreserveNonAlpha = true;
            this.Prop_ToUpperCase      = false;
            this.Prop_ToLowerCase      = false;
            this.Prop_ReplaceSpaces    = string.Empty;
        }

        // =====================================================================
        // Выполнение
        // =====================================================================

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var logic = new TextTranslitLogic();
                string inputText = GetPropertyValue<string>(
                    this.Prop_InputText, nameof(Prop_InputText), sd);

                if (inputText == null)
                    return Fail(ActivityStrings.Error_InputStringRequired);

                var translitResult = logic.Transliterate(
                    inputText,
                    this.Prop_Scheme,
                    this.Prop_Direction,
                    this.Prop_PreserveCase,
                    this.Prop_PreserveNonAlpha);

                TranslitDirection direction = translitResult.DetectedDirection;
                SetVariableValue(this.Prop_DetectedDirection, direction.ToString(), sd);

                // ── Постобработка ──────────────────────────────────────────

                // Замена пробелов (до смены регистра — важен порядок)
                string replaceSpaces = GetPropertyValue<string>(
                    this.Prop_ReplaceSpaces, nameof(Prop_ReplaceSpaces), sd);
                string result = translitResult.Text;
                if (!string.IsNullOrEmpty(replaceSpaces))
                    result = logic.ReplaceSpaces(result, replaceSpaces);

                // Приведение регистра (взаимоисключающие флаги — ToUpper имеет приоритет)
                if (this.Prop_ToUpperCase)
                    result = logic.ToUpperCase(result);
                else if (this.Prop_ToLowerCase)
                    result = logic.ToLowerCase(result);

                // ── Записываем результаты ──────────────────────────────────

                SetVariableValue(this.Prop_Result,       result,       sd);
                SetVariableValue(this.Prop_ChangedChars, translitResult.ChangedChars, sd);

                string directionMsg = direction == TranslitDirection.CyrillicToLatin
                    ? "Кир → Лат"
                    : "Лат → Кир";

                return new ExecutionResult
                {
                    IsSuccess      = true,
                    SuccessMessage = $"Транслитерировано ({this.Prop_Scheme}, {directionMsg}): " +
                                     $"изменено {translitResult.ChangedChars} символов"
                };
            }
            catch (Exception ex)
            {
                return Fail($"Ошибка транслитерации: {ex.Message}");
            }
        }

        // =====================================================================
        // Транслитерация Кириллица → Латиница
        // =====================================================================

        /// <summary>
        /// Выполняет транслитерацию кириллицы в латиницу посимвольно.
        /// Каждый строчный кириллический символ ищется в таблице схемы.
        /// Регистр оригинала восстанавливается через PreserveCase.
        /// </summary>
        private static string TransliterateCyrillicToLatin(
            string text, TranslitScheme scheme,
            bool preserveCase, bool preserveNonAlpha,
            out int changedChars)
        {
            var map = SchemeMaps[scheme];
            var sb  = new StringBuilder(text.Length * 2);
            changedChars = 0;

            foreach (char c in text)
            {
                char lower = char.ToLowerInvariant(c);

                if (map.TryGetValue(lower, out string replacement))
                {
                    // Восстанавливаем регистр: если оригинал заглавный —
                    // делаем первую букву замены заглавной
                    if (preserveCase && char.IsUpper(c) && replacement.Length > 0)
                        replacement = char.ToUpperInvariant(replacement[0])
                                    + replacement.Substring(1);

                    sb.Append(replacement);
                    // Считаем изменение только если результат отличается от оригинала
                    if (replacement != c.ToString() && replacement != lower.ToString())
                        changedChars++;
                }
                else
                {
                    // Нетранслитерируемый символ — оставляем или удаляем
                    if (preserveNonAlpha)
                        sb.Append(c);
                }
            }

            return sb.ToString();
        }

        // =====================================================================
        // Транслитерация Латиница → Кириллица
        // =====================================================================

        /// <summary>
        /// Выполняет обратную транслитерацию латиницы в кириллицу.
        /// Использует жадный поиск: сначала пробует двух- и трёхсимвольные комбинации,
        /// затем одиночный символ. Это необходимо для правильного разбора "shch" → "щ".
        ///
        /// Для ISO9 — использует точную обратную таблицу ISO9ReverseMap.
        /// Для других схем — использует инвертированную таблицу схемы.
        /// </summary>
        private static string TransliterateLatinToCyrillic(
            string text, TranslitScheme scheme,
            bool preserveCase, bool preserveNonAlpha,
            out int changedChars)
        {
            // Строим обратную таблицу: латинская строка → кириллический символ
            Dictionary<string, char> reverseMap;

            if (scheme == TranslitScheme.ISO9)
            {
                reverseMap = ISO9ReverseMap;
            }
            else
            {
                // Строим обратный словарь из прямой таблицы схемы через LINQ
                // Пропускаем пустые значения (Ъ и Ь в некоторых схемах → "")
                var forward = SchemeMaps[scheme];
                reverseMap  = forward
                    .Where(kv => !string.IsNullOrEmpty(kv.Value))
                    .GroupBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g   => g.Key,
                        g   => g.First().Key,
                        StringComparer.OrdinalIgnoreCase
                    );
            }

            var sb  = new StringBuilder(text.Length);
            int i   = 0;
            changedChars = 0;

            while (i < text.Length)
            {
                bool found = false;

                // Жадный поиск: пробуем длинные комбинации первыми (4, 3, 2, 1)
                // Это критично для "shch" (щ) — без этого "sh" + "ch" дало бы "шч"
                foreach (int len in new[] { 4, 3, 2, 1 })
                {
                    if (i + len > text.Length) continue;

                    string chunk = text.Substring(i, len);
                    if (reverseMap.TryGetValue(chunk, out char cyrChar))
                    {
                        // Восстанавливаем регистр: если первая буква куска заглавная
                        char result = preserveCase && char.IsUpper(chunk[0])
                            ? char.ToUpperInvariant(cyrChar)
                            : cyrChar;

                        sb.Append(result);
                        changedChars++;
                        i    += len;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // Символ не найден в обратной таблице
                    if (preserveNonAlpha)
                        sb.Append(text[i]);
                    i++;
                }
            }

            return sb.ToString();
        }

        // =====================================================================
        // Вспомогательные методы
        // =====================================================================

        /// <summary>
        /// Автоопределение направления транслитерации по большинству букв в тексте.
        /// Если кириллических букв >= латинских → CyrillicToLatin, иначе → LatinToCyrillic.
        /// </summary>
        private static TranslitDirection DetectDirection(string text)
        {
            // Подсчёт через LINQ — компактно и читаемо
            int cyrillic = text.Count(c =>
                (c >= 'А' && c <= 'Я') || (c >= 'а' && c <= 'я') || c == 'Ё' || c == 'ё');
            int latin    = text.Count(c =>
                (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'));

            return cyrillic >= latin
                ? TranslitDirection.CyrillicToLatin
                : TranslitDirection.LatinToCyrillic;
        }

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

            // ToUpperCase и ToLowerCase взаимоисключающи
            if (this.Prop_ToUpperCase && this.Prop_ToLowerCase)
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_ToUpperCase),
                    Error        = "Нельзя одновременно включить «К верхнему регистру» и «К нижнему регистру»"
                });

            return ret;
        }
    }
}
