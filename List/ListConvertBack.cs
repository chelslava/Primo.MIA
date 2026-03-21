// =============================================================================
// ListConvertBack.cs — три активности:
//
//   ListGroupBack   — «Список: Группировка»
//     Группирует элементы List<string> по ключу (первый символ, длина,
//     regex-группа, кастомный префикс) → Dictionary<string, List<string>>.
//     Дополнительно: топ-N самых часто встречающихся элементов.
//
//   ListConvertBack — «Список: Конвертация»
//     ToDict         — List<"key=value"> → Dictionary<string,string>
//     ToDictIndexed  — List<string> → Dictionary<string(индекс),string>
//     ToCSVRow       — List<string> → одна CSV-строка
//     FromCSVRow     — CSV-строка → List<string>
//     ZipWithList    — zip двух List<string> → Dictionary<string,string>
//     Flatten        — List<string> где каждый элемент разделитель → плоский List
//     Chunk          — List<string> → List<List<string>> батчами по N
//
//   ListInspectBack — «Список: Анализ»
//     Полная диагностика: дубли, статистика длин,
//     наличие элемента, индексы, частотность.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Список: Конвертация».
    /// Конвертирует List&lt;string&gt; в различные структуры данных и обратно.
    /// </summary>
    public class ListConvertBack : PrimoComponentTO<ListConvert>
    {
        public override string GroupName { get => ActivityCategories.Lists; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        private string _propList;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_List)]
        public string Prop_List
        {
            get => _propList;
            set { _propList = value; InvokePropertyChanged(this, "Prop_List"); }
        }

        private ListConvertMode _mode = ListConvertMode.ToDict;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName("Тип конвертации")]
        public ListConvertMode Mode
        {
            get => _mode;
            set { _mode = value; InvokePropertyChanged(this, "Mode"); }
        }

        private string _propListB;
        /// <summary>Второй список (режим ZipToDict — значения словаря)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category("ZipToDict"), System.ComponentModel.DisplayName("Список значений (Zip)")]
        public string Prop_ListB
        {
            get => _propListB;
            set { _propListB = value; InvokePropertyChanged(this, "Prop_ListB"); }
        }

        private string _propSeparator;
        /// <summary>Разделитель для режимов ToDict (default "="), Flatten и FromCSVRow/ToCSVRow</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters), System.ComponentModel.DisplayName(ActivityStrings.Field_Separator)]
        public string Prop_Separator
        {
            get => _propSeparator;
            set { _propSeparator = value; InvokePropertyChanged(this, "Prop_Separator"); }
        }

        private string _propChunkSize;
        /// <summary>Размер батча (режим Chunk)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Chunk"), System.ComponentModel.DisplayName("Размер батча")]
        public string Prop_ChunkSize
        {
            get => _propChunkSize;
            set { _propChunkSize = value; InvokePropertyChanged(this, "Prop_ChunkSize"); }
        }

        private string _propCsvInput;
        /// <summary>Входная CSV-строка для режима FromCSVRow</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("CSV"), System.ComponentModel.DisplayName("CSV строка (вход)")]
        public string Prop_CsvInput
        {
            get => _propCsvInput;
            set { _propCsvInput = value; InvokePropertyChanged(this, "Prop_CsvInput"); }
        }

        // — OUTPUT —

        private string _propResultList;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_List)]
        public string Prop_ResultList
        {
            get => _propResultList;
            set { _propResultList = value; InvokePropertyChanged(this, "Prop_ResultList"); }
        }

        private string _propResultDict;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Dictionary)]
        public string Prop_ResultDict
        {
            get => _propResultDict;
            set { _propResultDict = value; InvokePropertyChanged(this, "Prop_ResultDict"); }
        }

        private string _propResultString;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName("Строка")]
        public string Prop_ResultString
        {
            get => _propResultString;
            set { _propResultString = value; InvokePropertyChanged(this, "Prop_ResultString"); }
        }

        private string _propCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Count)]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, "Prop_Count"); }
        }

        public ListConvertBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Список: Конвертация";
            sdkComponentHelp =
                "Конвертирует List<string> в другие структуры данных.\n\n" +
                "ToDict       — List<\"key=val\"> → Dictionary\n" +
                "ToDictIndexed — List → Dictionary<\"0\",val>, <\"1\",val>...\n" +
                "ToCSVRow     — List → CSV-строка \"a\",\"b\",\"c\"\n" +
                "FromCSVRow   — CSV-строка → List\n" +
                "ZipToDict    — zip двух List → Dictionary (ключи=ListA, знач=ListB)\n" +
                "Flatten      — разбить каждый элемент по разделителю → плоский List\n" +
                "Chunk        — разбить List на батчи по N элементов";
            sdkComponentIcon = ActivityIcons.List;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<List<string>>("Prop_List", "Входной список"),
                PropertyBuilder.Enum<ListConvertMode>("Mode", "Тип конвертации"),
                PropertyBuilder.Script<List<string>>("Prop_ListB", "Второй список (ZipToDict — значения)"),
                PropertyBuilder.Script<string>("Prop_Separator", "Разделитель: '=' для ToDict, ';' для Flatten"),
                PropertyBuilder.Script<int>("Prop_ChunkSize", "Размер батча для Chunk"),
                PropertyBuilder.Script<string>("Prop_CsvInput", "Входная CSV-строка для FromCSVRow"),
                PropertyBuilder.Variable<List<string>>("Prop_ResultList", "Список-результат"),
                PropertyBuilder.Variable<Dictionary<string, string>>("Prop_ResultDict", "Словарь-результат"),
                PropertyBuilder.Variable<string>("Prop_ResultString", "Строка-результат (CSV)"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество в результате")
            };

            InitClass(container);
            this.Prop_Separator = "\"=\"";
            this.Prop_ChunkSize = "10";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var list = GetPropertyValue<List<string>>(this.Prop_List, "Prop_List", sd);
                var listB = GetPropertyValue<List<string>>(this.Prop_ListB, "Prop_ListB", sd);
                string sep = GetPropertyValue<string>(this.Prop_Separator, "Prop_Separator", sd) ?? "=";
                string csv = GetPropertyValue<string>(this.Prop_CsvInput, "Prop_CsvInput", sd) ?? string.Empty;
                int chunkSize = int.TryParse(GetPropertyValue<string>(this.Prop_ChunkSize, "Prop_ChunkSize", sd), out int cs) ? cs : 10;

                switch (this.Mode)
                {
                    case ListConvertMode.ToDict:
                        {
                            if (list == null) throw new ArgumentNullException("Prop_List");
                            // Разбиваем каждую строку по первому вхождению разделителя
                            var dict = list
                                .Where(s => !string.IsNullOrWhiteSpace(s) && s.Contains(sep))
                                .Select(s => s.Split(new[] { sep }, 2, StringSplitOptions.None))
                                .GroupBy(parts => parts[0].Trim())
                                .ToDictionary(g => g.Key, g => g.Last()[1].Trim());
                            SetVariableValue(this.Prop_ResultDict, dict, sd);
                            SetVariableValue(this.Prop_Count, dict.Count, sd);
                            break;
                        }

                    case ListConvertMode.ToDictIndexed:
                        {
                            if (list == null) throw new ArgumentNullException("Prop_List");
                            var dict = list
                                .Select((item, idx) => new { idx, item = item ?? string.Empty })
                                .ToDictionary(x => x.idx.ToString(), x => x.item);
                            SetVariableValue(this.Prop_ResultDict, dict, sd);
                            SetVariableValue(this.Prop_Count, dict.Count, sd);
                            break;
                        }

                    case ListConvertMode.ToCSVRow:
                        {
                            if (list == null) throw new ArgumentNullException("Prop_List");
                            // Каждый элемент оборачиваем в кавычки, внутренние кавычки экранируем удвоением
                            string csvRow = string.Join(",",
                                list.Select(s => "\"" + (s ?? string.Empty).Replace("\"", "\"\"") + "\""));
                            SetVariableValue(this.Prop_ResultString, csvRow, sd);
                            SetVariableValue(this.Prop_Count, list.Count, sd);
                            break;
                        }

                    case ListConvertMode.FromCSVRow:
                        {
                            // Простой CSV-парсер с поддержкой кавычек
                            var result = ParseCsvRow(csv);
                            SetVariableValue(this.Prop_ResultList, result, sd);
                            SetVariableValue(this.Prop_Count, result.Count, sd);
                            break;
                        }

                    case ListConvertMode.ZipToDict:
                        {
                            if (list == null) throw new ArgumentNullException("Prop_List");
                            if (listB == null) throw new ArgumentNullException("Prop_ListB", "Список значений обязателен для ZipToDict");
                            if (list.Count != listB.Count)
                                throw new ArgumentException($"Списки разной длины: {list.Count} vs {listB.Count}");
                            var dict = list
                                .Zip(listB, (k, v) => new { k = k ?? string.Empty, v = v ?? string.Empty })
                                .GroupBy(x => x.k)
                                .ToDictionary(g => g.Key, g => g.Last().v);
                            SetVariableValue(this.Prop_ResultDict, dict, sd);
                            SetVariableValue(this.Prop_Count, dict.Count, sd);
                            break;
                        }

                    case ListConvertMode.Flatten:
                        {
                            if (list == null) throw new ArgumentNullException("Prop_List");
                            // Разбиваем каждый элемент по разделителю и собираем в один плоский список
                            var flat = list
                                .Where(s => s != null)
                                .SelectMany(s => s.Split(new[] { sep }, StringSplitOptions.RemoveEmptyEntries))
                                .Select(s => s.Trim())
                                .ToList();
                            SetVariableValue(this.Prop_ResultList, flat, sd);
                            SetVariableValue(this.Prop_Count, flat.Count, sd);
                            break;
                        }

                    case ListConvertMode.Chunk:
                        {
                            if (list == null) throw new ArgumentNullException("Prop_List");
                            if (chunkSize < 1) throw new ArgumentException("Размер батча должен быть ≥ 1");
                            // Разбиваем на батчи через Range + Skip/Take
                            var chunks = Enumerable
                                .Range(0, (int)Math.Ceiling((double)list.Count / chunkSize))
                                .Select(i => list.Skip(i * chunkSize).Take(chunkSize).ToList())
                                .ToList();
                            // Chunk возвращает List<List<string>> — сохраняем кол-во батчей
                            SetVariableValue(this.Prop_Count, chunks.Count, sd);
                            // Дополнительно сохраняем первый батч в ResultList для удобства
                            SetVariableValue(this.Prop_ResultList, chunks.FirstOrDefault() ?? new List<string>(), sd);
                            break;
                        }

                    default:
                        throw new InvalidOperationException($"Неизвестный режим: {this.Mode}");
                }

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Конвертация ({this.Mode}) выполнена" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка конвертации: {ex.Message}" };
            }
        }

        /// <summary>
        /// Простой CSV-парсер поддерживающий кавычки и экранирование удвоением.
        /// "value 1","value ""quoted""",plain → ["value 1", "value \"quoted\"", "plain"]
        /// </summary>
        private static List<string> ParseCsvRow(string row)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(row)) return result;

            int i = 0;
            while (i < row.Length)
            {
                if (row[i] == '"')
                {
                    // Quoted field
                    var sb = new StringBuilder();
                    i++; // пропускаем открывающую кавычку
                    while (i < row.Length)
                    {
                        if (row[i] == '"' && i + 1 < row.Length && row[i + 1] == '"')
                        {
                            sb.Append('"'); i += 2; // двойная кавычка → одиночная
                        }
                        else if (row[i] == '"')
                        {
                            i++; break; // закрывающая кавычка
                        }
                        else
                        {
                            sb.Append(row[i++]);
                        }
                    }
                    result.Add(sb.ToString());
                    if (i < row.Length && row[i] == ',') i++; // пропускаем запятую
                }
                else
                {
                    // Unquoted field
                    int start = i;
                    while (i < row.Length && row[i] != ',') i++;
                    result.Add(row.Substring(start, i - start));
                    if (i < row.Length) i++; // пропускаем запятую
                }
            }

            return result;
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            if (this.Mode != ListConvertMode.FromCSVRow)
                ret.ValidateRequired(this.Prop_List, ActivityStrings.Field_List, ActivityStrings.Error_ListRequired);
            if (this.Mode == ListConvertMode.FromCSVRow && string.IsNullOrWhiteSpace(this.Prop_CsvInput))
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "CSV строка", Error = "CSV строка обязательна для FromCSVRow" });
            return ret;
        }
    }
}
