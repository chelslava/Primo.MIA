// =============================================================================
// TextTranslitLogic.cs — бизнес-логика транслитерации.
//
// Извлечена из TextTranslitBack.cs для unit-тестирования без зависимостей
// от Primo RPA SDK.
//
// Поддерживаемые схемы:
//   GOST7792000  — ГОСТ 7.79-2000 сист. Б (официальный стандарт РФ)
//   Passport2013 — Загранпаспорта РФ с 2013 года (Приказ МВД №827)
//   ICAOPassport — ИКАО Doc 9303 (авиабилеты, машиносчитываемые документы)
//   ISO9         — ISO 9:1995 (однозначная обратимая транслитерация)
//   BGN_PCGN     — BGN/PCGN 1947 (английские географические названия)
//   Simplified   — Упрощённая ASCII (логины, имена файлов, URL)
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Primo.MIA.Tests.Logic
{
    /// <summary>Схема транслитерации.</summary>
    public enum TranslitScheme
    {
        GOST7792000,
        Passport2013,
        ICAOPassport,
        ISO9,
        BGN_PCGN,
        Simplified
    }

    /// <summary>Направление транслитерации.</summary>
    public enum TranslitDirection
    {
        CyrillicToLatin,
        LatinToCyrillic,
        AutoDetect
    }

    /// <summary>
    /// Результат транслитерации.
    /// </summary>
    public class TranslitResult
    {
        /// <summary>Транслитерированный текст.</summary>
        public string Text { get; set; }

        /// <summary>Количество изменённых символов.</summary>
        public int ChangedChars { get; set; }

        /// <summary>Определённое направление (при AutoDetect).</summary>
        public TranslitDirection DetectedDirection { get; set; }
    }

    /// <summary>
    /// Бизнес-логика транслитерации.
    /// </summary>
    public class TextTranslitLogic
    {
        // =====================================================================
        // Таблицы транслитерации — Кириллица → Латиница
        // =====================================================================

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

        /// <summary>
        /// Выполняет транслитерацию текста.
        /// </summary>
        /// <param name="text">Исходный текст.</param>
        /// <param name="scheme">Схема транслитерации.</param>
        /// <param name="direction">Направление (CyrillicToLatin, LatinToCyrillic, AutoDetect).</param>
        /// <param name="preserveCase">Сохранять регистр оригинала.</param>
        /// <param name="preserveNonAlpha">Оставлять нетранслитерируемые символы.</param>
        /// <returns>Результат транслитерации.</returns>
        public TranslitResult Transliterate(
            string text,
            TranslitScheme scheme,
            TranslitDirection direction,
            bool preserveCase = true,
            bool preserveNonAlpha = true)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            // Определяем направление
            TranslitDirection actualDirection = direction;
            if (direction == TranslitDirection.AutoDetect)
                actualDirection = DetectDirection(text);

            // Выполняем транслитерацию
            int changedChars;
            string result;

            if (actualDirection == TranslitDirection.LatinToCyrillic)
                result = TransliterateLatinToCyrillic(text, scheme, preserveCase, preserveNonAlpha, out changedChars);
            else
                result = TransliterateCyrillicToLatin(text, scheme, preserveCase, preserveNonAlpha, out changedChars);

            return new TranslitResult
            {
                Text = result,
                ChangedChars = changedChars,
                DetectedDirection = actualDirection
            };
        }

        /// <summary>
        /// Определяет направление транслитерации по большинству букв в тексте.
        /// </summary>
        /// <param name="text">Исходный текст.</param>
        /// <returns>CyrillicToLatin если кириллицы >= латиницы, иначе LatinToCyrillic.</returns>
        public TranslitDirection DetectDirection(string text)
        {
            if (string.IsNullOrEmpty(text))
                return TranslitDirection.CyrillicToLatin;

            int cyrillic = text.Count(c =>
                (c >= 'А' && c <= 'Я') || (c >= 'а' && c <= 'я') || c == 'Ё' || c == 'ё');
            int latin = text.Count(c =>
                (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'));

            return cyrillic >= latin
                ? TranslitDirection.CyrillicToLatin
                : TranslitDirection.LatinToCyrillic;
        }

        /// <summary>
        /// Транслитерация кириллицы в латиницу.
        /// </summary>
        private static string TransliterateCyrillicToLatin(
            string text, TranslitScheme scheme,
            bool preserveCase, bool preserveNonAlpha,
            out int changedChars)
        {
            var map = SchemeMaps[scheme];
            var sb = new StringBuilder(text.Length * 2);
            changedChars = 0;

            foreach (char c in text)
            {
                char lower = char.ToLowerInvariant(c);

                if (map.TryGetValue(lower, out string replacement))
                {
                    // Восстанавливаем регистр
                    if (preserveCase && char.IsUpper(c) && replacement.Length > 0)
                        replacement = char.ToUpperInvariant(replacement[0])
                                    + replacement.Substring(1);

                    sb.Append(replacement);
                    if (replacement != c.ToString() && replacement != lower.ToString())
                        changedChars++;
                }
                else
                {
                    if (preserveNonAlpha)
                        sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Транслитерация латиницы в кириллицу.
        /// </summary>
        private static string TransliterateLatinToCyrillic(
            string text, TranslitScheme scheme,
            bool preserveCase, bool preserveNonAlpha,
            out int changedChars)
        {
            Dictionary<string, char> reverseMap;

            if (scheme == TranslitScheme.ISO9)
            {
                reverseMap = ISO9ReverseMap;
            }
            else
            {
                var forward = SchemeMaps[scheme];
                reverseMap = forward
                    .Where(kv => !string.IsNullOrEmpty(kv.Value))
                    .GroupBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().Key,
                        StringComparer.OrdinalIgnoreCase
                    );
            }

            var sb = new StringBuilder(text.Length);
            int i = 0;
            changedChars = 0;

            while (i < text.Length)
            {
                bool found = false;

                // Жадный поиск: пробуем длинные комбинации первыми
                foreach (int len in new[] { 4, 3, 2, 1 })
                {
                    if (i + len > text.Length) continue;

                    string chunk = text.Substring(i, len);
                    if (reverseMap.TryGetValue(chunk, out char cyrChar))
                    {
                        char result = preserveCase && char.IsUpper(chunk[0])
                            ? char.ToUpperInvariant(cyrChar)
                            : cyrChar;

                        sb.Append(result);
                        changedChars++;
                        i += len;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    if (preserveNonAlpha)
                        sb.Append(text[i]);
                    i++;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Заменяет пробелы в тексте.
        /// </summary>
        /// <param name="text">Исходный текст.</param>
        /// <param name="replacement">Строка замены.</param>
        /// <returns>Текст с заменёнными пробелами.</returns>
        public string ReplaceSpaces(string text, string replacement)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            if (string.IsNullOrEmpty(replacement))
                return text;

            return text.Replace(" ", replacement);
        }

        /// <summary>
        /// Приводит текст к верхнему регистру.
        /// </summary>
        public string ToUpperCase(string text)
        {
            return text?.ToUpperInvariant();
        }

        /// <summary>
        /// Приводит текст к нижнему регистру.
        /// </summary>
        public string ToLowerCase(string text)
        {
            return text?.ToLowerInvariant();
        }
    }
}
