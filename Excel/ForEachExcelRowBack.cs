// =============================================================================
// ForEachExcelRowBack.cs — контейнер «Excel: Для каждой строки» с ленивой
// загрузкой строк (OpenXML SAX, ExcelLazyReader).
//
// Файл открывается поточно: строки читаются из XML по одной — без загрузки
// книги в память целиком и без DataTable (в отличие от ReadRange + цикл).
// Каждая итерация: MoveNext ридера → значения строки в List<string> →
// исполнение тела (детей) через IWFComponent.Execute. Контракт движка —
// как у ForEachRowBack (декомпиляция LTools.SDK, см. базу знаний).
//
// Значения: строки — как есть (SharedStrings), числа — Invariant-строка,
// даты — Excel-серийное число (ограничение ленивого режима; полная
// типизация — ExcelReadRange).
// =============================================================================

using LTools.Common.Helpers;
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.Common.WFItems;
using LTools.SDK;
using LTools.Common.Exceptions;
using Primo.MIA.Common;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Контейнер перебора строк Excel с ленивым чтением.</summary>
    public class ForEachExcelRowBack : PrimoContainerCustom<ForEachExcelRow>
    {
        public override string GroupName { get => ActivityCategories.Excel; protected set { } }

        private string _propFilePath;
        /// <summary>Путь к файлу .xlsx.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_LoopFilePath)]
        public string Prop_FilePath
        {
            get => _propFilePath;
            set { _propFilePath = value; InvokePropertyChanged(this, nameof(Prop_FilePath)); }
        }

        private string _propSheet;
        /// <summary>Имя листа (пусто — первый).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_LoopSheet)]
        public string Prop_Sheet
        {
            get => _propSheet;
            set { _propSheet = value; InvokePropertyChanged(this, nameof(Prop_Sheet)); }
        }

        private bool _propHasHeader = true;
        /// <summary>Первая строка — заголовки (не попадает в тело).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_LoopHasHeader)]
        public bool Prop_HasHeader
        {
            get => _propHasHeader;
            set { _propHasHeader = value; InvokePropertyChanged(this, nameof(Prop_HasHeader)); }
        }

        private string _propSkipRows;
        /// <summary>Пропустить первые N строк данных.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_LoopSkipRows)]
        public string Prop_SkipRows
        {
            get => _propSkipRows;
            set { _propSkipRows = value; InvokePropertyChanged(this, nameof(Prop_SkipRows)); }
        }

        private string _propMaxRows;
        /// <summary>Максимум итераций (0 — все).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_LoopMaxRows)]
        public string Prop_MaxRows
        {
            get => _propMaxRows;
            set { _propMaxRows = value; InvokePropertyChanged(this, nameof(Prop_MaxRows)); }
        }

        private bool _propDebugPause;
        /// <summary>Отладочная пауза: диалог перед каждой итерацией (Продолжить/Прервать).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_LoopDebugPause)]
        public bool Prop_DebugPause
        {
            get => _propDebugPause;
            set { _propDebugPause = value; InvokePropertyChanged(this, nameof(Prop_DebugPause)); }
        }

        // ── Выходные ────────────────────────────────────────────────────

        private string _propRowValues;
        /// <summary>Значения ячеек текущей строки (List&lt;string&gt;).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_LoopRowValues)]
        public string Prop_RowValues
        {
            get => _propRowValues;
            set { _propRowValues = value; InvokePropertyChanged(this, nameof(Prop_RowValues)); }
        }

        private string _propRowIndex;
        /// <summary>Индекс строки данных (0-based).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_LoopRowIndex)]
        public string Prop_RowIndex
        {
            get => _propRowIndex;
            set { _propRowIndex = value; InvokePropertyChanged(this, nameof(Prop_RowIndex)); }
        }

        private string _propColumnCount;
        /// <summary>Ширина листа (число колонок).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_LoopCellCount)]
        public string Prop_ColumnCount
        {
            get => _propColumnCount;
            set { _propColumnCount = value; InvokePropertyChanged(this, nameof(Prop_ColumnCount)); }
        }

        private string _propColumns;
        /// <summary>Имена колонок из заголовков (List&lt;string&gt;).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_LoopColumns)]
        public string Prop_Columns
        {
            get => _propColumns;
            set { _propColumns = value; InvokePropertyChanged(this, nameof(Prop_Columns)); }
        }

        public ForEachExcelRowBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_ForEachExcelRow;
            sdkComponentHelp = @"Компонент ""Excel: Для каждой строки""
Ленивый цикл по строкам .xlsx: файл читается поточно (OpenXML SAX), строки отдаются по одной без загрузки книги целиком — старт мгновенный даже для сотен тысяч строк, память O(одной строки).

Основные:
Файл Excel*: [String] Путь к .xlsx.
Лист: [String] Имя листа; пусто — первый.
Первая строка — заголовки: [Bool] Не попадает в тело; уходит в выход «Колонки».

Настройки:
Пропустить строк: [Int32] Начать с индекса N (0 — с первой строки данных).
Максимум строк: [Int32] Ограничение итераций (0 — все).
Отладочная пауза: [Bool] Окно «Продолжить / Прервать цикл» перед каждой итерацией — замена брейкпоинта внутри тела (студийная отладка внутрь тела цикла не заходит).

Выходные данные:
Значения строки: [List<String>] Ячейки текущей строки.
Индекс строки: [Int32] 0-based индекс строки данных.
Колонок: [Int32] Ширина листа.
Колонки: [List<String>] Имена из строки заголовков (если включена).

Тело цикла:
Внутри доступны «Цикл: Прервать» и «Цикл: Продолжить».
Значения: текст — как есть, числа — Invariant-строка, даты — Excel-серийное число (полная типизация — активностью «Excel: Прочитать диапазон»).
Ошибка вложенной активности останавливает цикл (флаг «Продолжить при ошибке» элемента — продолжить).";
            sdkComponentIcon = ActivityIcons.Loop;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_FilePath", "Путь к файлу .xlsx"),
                PropertyBuilder.String("Prop_Sheet", "Лист (пусто — первый)"),
                PropertyBuilder.BooleanObject("Prop_HasHeader", "Первая строка — заголовки"),
                PropertyBuilder.Int("Prop_SkipRows", "Пропустить первые N строк данных"),
                PropertyBuilder.Int("Prop_MaxRows", "Максимум итераций (0 — все)"),
                PropertyBuilder.Variable<List<string>>("Prop_RowValues", "Значения строки (List<String>)"),
                PropertyBuilder.Variable<int>("Prop_RowIndex", "Индекс строки (Int32)"),
                PropertyBuilder.Variable<int>("Prop_ColumnCount", "Колонок в строке (Int32)"),
                PropertyBuilder.Variable<List<string>>("Prop_Columns", "Колонки из заголовков (List<String>)")
            };
            InitClass(container);

            // Тело цикла = модельный контейнер (приватное поле cont базового
            // класса — движок грузит сохранённых детей именно туда). Захват
            // в конструкторе: в robot-режиме element.Container обнуляется.
            _body = ResolveBody();

            this.Prop_FilePath = "\"\"";
            this.Prop_Sheet = "\"\"";
            this.Prop_SkipRows = "0";
            this.Prop_MaxRows = "0";
            this.Prop_RowValues = "\"\"";
            this.Prop_Columns = "\"\"";
        }

        /// <summary>
        /// Модельный контейнер тела: 1) приватное поле cont базового класса
        /// (рефлексия); 2) тело вида; 3) тело последнего экземпляра вида.
        /// </summary>
        private WFContainerBase ResolveBody()
        {
            try
            {
                var field = typeof(PrimoContainerCustom<ForEachExcelRow>).GetField(
                    "cont", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var viaReflection = field?.GetValue(this) as WFContainerBase;
                if (viaReflection != null)
                    return viaReflection;
            }
            catch (Exception ex)
            {
                Primo.MIA.Common.ActivityFileLogger.Warning(sdkComponentName, $"cont через рефлексию: {ex.Message}");
            }

            try
            {
                var viaView = ((ForEachExcelRow)this.UIControl).Body;
                if (viaView != null)
                    return viaView;
            }
            catch (Exception ex)
            {
                Primo.MIA.Common.ActivityFileLogger.Warning(sdkComponentName, $"cont через вид: {ex.Message}");
            }

            var last = ForEachExcelRow.LastInstance?.Body;
            if (last != null)
                return last;

            _bodyCaptureError = "не найден ни одним из способов (рефлексия/вид/LastInstance)";
            return null;
        }

        /// <summary>Тело цикла (WFContainerBase), захваченное в конструкторе.</summary>
        private WFContainerBase _body;

        /// <summary>Причина неудачи захвата тела в конструкторе (диагностика).</summary>
        private string _bodyCaptureError;

        /// <summary>Движком не вызывается: Execute перекрывает стандартную цепочку.</summary>
        public override ExecutionResult SimpleAction(ScriptingData sd)
        {
            return new ExecutionResult { IsSuccess = true, SuccessMessage = "Ленивый цикл по строкам Excel" };
        }

        /// <summary>Точка входа: ленивый перебор строк файла. Дети — из
        /// модельного контейнера (Components) или из тела вида (fallback).</summary>
        public override void Execute(ProcessContext context)
        {
            try
            {
                var sd = ScriptingHelper.PrepareScripting(this, context);

                var filePath = GetPropertyValue<string>(this.Prop_FilePath, nameof(Prop_FilePath), sd);
                if (string.IsNullOrWhiteSpace(filePath))
                    throw new InvalidOperationException(ActivityStrings.Error_LoopFilePathRequired);

                var sheet = GetPropertyValue<string>(this.Prop_Sheet, nameof(Prop_Sheet), sd);
                int skip = GetInt(sd, this.Prop_SkipRows, nameof(Prop_SkipRows), 0);
                int max = GetInt(sd, this.Prop_MaxRows, nameof(Prop_MaxRows), 0);

                // Тело: дети из this.Components (модель контейнера) исполняются
                // напрямую — подтверждено рабочим прогоном ForEachRow.
                var children = this.Components;
                int childCount = children?.Count ?? 0;
                if (childCount == 0)
                    throw new InvalidOperationException(
                        "Тело цикла пусто: помещайте вложенные активности ВНУТРЬ контейнера");

                int index = 0;
                var frame = new LoopFrame();
                using (LoopControl.Push(frame))
                using (var reader = new ExcelLazyReader(filePath, sheet, this.Prop_HasHeader, skip, max))
                {
                    ActivityFileLogger.Debug(sdkComponentName,
                        $"открытие \"{filePath}\", лист \"{sheet ?? "(первый)"}\", заголовки {this.Prop_HasHeader}, skip {skip}, max {max}");
                    foreach (var row in reader)
                    {
                        SetVariableValue(this.Prop_RowValues, new List<string>(row), sd);
                        SetVariableValue(this.Prop_RowIndex, index, sd);
                        SetVariableValue(this.Prop_ColumnCount, row.Count, sd);
                        if (reader.Columns != null)
                            SetVariableValue(this.Prop_Columns, new List<string>(reader.Columns), sd);

                        FireExecutionInfo(context, $"итерация {index}");

                        // отладочная пауза: диалог «Продолжить/Прервать»
                        if (this.Prop_DebugPause && DebugPause(row, index, reader.Columns))
                            break;

                        var status = RunBodyDirect(children, context, frame, $"строка Excel {index}")
                            ? IterationStatus.Success
                            : IterationStatus.Fatal;

                        index++;
                        if (status == IterationStatus.Fatal)
                            return; // EndProcess уже вызван с ошибкой
                        if (status == IterationStatus.Break)
                            break;
                        // Continue → следующая итерация
                    }
                }

                ActivityFileLogger.Info(sdkComponentName, $"цикл завершён: итераций {index}");
                EndProcess(new ElementExecutionResults(context));
            }
            catch (Exception ex)
            {
                Common.ActivityFileLogger.Error(sdkComponentName, ex.GetType().Name + ": " + ex.Message + Environment.NewLine + ex.StackTrace);
                // стек в сообщении — диагностика циклов MIA (Studio показывает первую
                // строку в колонке «Сообщение», полный текст — в деталях строки)
                var stack = string.Join(" | ", ex.StackTrace.Split('\n').Take(6));
                EndProcess(new ElementExecutionResults(
                    new ExecutionException($"{ex.GetType().Name}: {ex.Message} [{stack}]", ex),
                    context));
            }
        }

        /// <summary>
        /// Отладочная пауза: окно с индексом итерации и значениями ячеек
        /// строки (с именами колонок из заголовков, если есть).
        /// Возврат true — пользователь выбрал «Прервать цикл».
        /// </summary>
        private static bool DebugPause(IReadOnlyList<string> row, int rowIndex, IReadOnlyList<string> columns)
        {
            string values;
            if (columns != null && columns.Count > 0)
                values = string.Join(";  ", row.Select((v, i) =>
                    $"{(i < columns.Count ? columns[i] : $"[{i}]")} = {v}"));
            else
                values = string.Join(";  ", row.Select((v, i) => $"[{i}] = {v}"));
            if (values.Length > 1200)
                values = values.Substring(0, 1200) + " …";
            return LoopDebugPause.Show(
                $"Итерация {rowIndex}{Environment.NewLine}{Environment.NewLine}{values}",
                ActivityStrings.LoopPauseCaption);
        }

        /// <summary>
        /// Прямое исполнение тела: дети из модельного контейнера — как в
        /// ForEachRowBack (ошибка ребёнка: ContinueOnException → продолжить,
        /// иначе остановить цикл).
        /// </summary>
        private bool RunBodyDirect(IList<IWFComponent> children, ProcessContext context, LoopFrame frame, string iterationLabel)
        {
            foreach (var child in children)
            {
                ElementExecutionResults result = null;
                ElementExecutionFinishedDelegate handler = (s, r) => result = r;
                child.ElementExecutionFinished += handler;
                try
                {
                    child.Execute(context);
                }
                finally
                {
                    child.ElementExecutionFinished -= handler;
                }

                if (result != null && !result.IsSuccess)
                {
                    if (!this.ContinueOnException)
                    {
                        EndProcess(new ElementExecutionResults(
                            new ExecutionException(
                                $"[{sdkComponentName}] ошибка тела ({iterationLabel}): {result.ExceptionData?.Message ?? "неизвестная ошибка"}",
                                result.ExceptionData),
                            context));
                        return false;
                    }
                    Common.ActivityFileLogger.Warning(
                        sdkComponentName,
                        $"Ошибка вложенной активности ({iterationLabel}, Продолжить при ошибке): {result.ExceptionData?.Message}");
                }

                if (LoopControl.Current?.BreakRequested == true || LoopControl.Current?.ContinueRequested == true)
                    return true;
            }
            return true;
        }

        /// <summary>
        /// Прогресс итерации в отладочный лог Studio: поднимает событие
        /// ElementExecutionInfo базового класса через рефлексию (событие
        /// объявлено в базовом классе — подкласс не может бросить напрямую).
        /// </summary>
        private void FireExecutionInfo(ProcessContext context, string text)
        {
            try
            {
                var field = typeof(PrimoContainerCustom<ForEachExcelRow>).GetField(
                    "ElementExecutionInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var del = field?.GetValue(this) as MulticastDelegate;
                if (del == null)
                    return;
                var args = new LTools.Common.Model.ElementExecutionInfoArgs
                {
                    MessageType = LTools.Enums.LogMessageType.Debug,
                    Text = text,
                    Context = context
                };
                foreach (var d in del.GetInvocationList())
                    d.Method.Invoke(d.Target, new object[] { this, args });
            }
            catch
            {
                // прогресс-событие не критично
            }
        }

        /// <summary>Итог итерации тела.</summary>
        private enum IterationStatus
        {
            Success,
            Break,
            Continue,
            Fatal
        }

        private int GetInt(ScriptingData sd, string prop, string name, int fallback)
        {
            var raw = GetPropertyValue<string>(prop, name, sd);
            return int.TryParse(raw, out var v) && v > 0 ? v : fallback;
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_FilePath, ActivityStrings.Field_LoopFilePath, ActivityStrings.Error_LoopFilePathRequired);
            return ret;
        }
    }
}
