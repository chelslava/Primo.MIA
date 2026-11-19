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

            // Тело цикла захватывается ОДИН РАЗ в конструкторе: в robot-режиме
            // Studio отсоединяет визуалы (element.Container обнуляется) —
            // доступ через UIControl в момент Execute даёт NRE.
            try
            {
                _body = ((ForEachExcelRow)this.UIControl).Body;
            }
            catch (Exception ex)
            {
                _bodyCaptureError = $"{ex.GetType().Name}: {ex.Message}";
                Primo.MIA.Common.ActivityFileLogger.Warning(
                    sdkComponentName, $"Тело цикла не захвачено при создании (будет взято при исполнении): {_bodyCaptureError}");
            }

            this.Prop_FilePath = "\"\"";
            this.Prop_Sheet = "\"\"";
            this.Prop_SkipRows = "0";
            this.Prop_MaxRows = "0";
            this.Prop_RowValues = "\"\"";
            this.Prop_Columns = "\"\"";
        }

        /// <summary>Тело цикла (WFContainerBase из вида), захваченное в конструкторе.</summary>
        private WFContainerBase _body;

        /// <summary>Причина неудачи захвата тела в конструкторе (диагностика).</summary>
        private string _bodyCaptureError;

        /// <summary>Движком не вызывается: Execute перекрывает стандартную цепочку.</summary>
        public override ExecutionResult SimpleAction(ScriptingData sd)
        {
            return new ExecutionResult { IsSuccess = true, SuccessMessage = "Ленивый цикл по строкам Excel" };
        }

        /// <summary>Точка входа: ленивый перебор строк файла; тело — через
        /// body.Execute (движковая цепочка: пошаговая отладка Studio работает
        /// внутри цикла).</summary>
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

                var body = _body ?? ForEachExcelRow.LastInstance?.Body;
                if (body == null)
                    throw new InvalidOperationException(
                        $"Тело цикла не найдено (WFContainerBase). Причина захвата при создании: {_bodyCaptureError ?? "Body вернул null"}");

                int index = 0;
                var frame = new LoopFrame();
                using (LoopControl.Push(frame))
                using (var reader = new ExcelLazyReader(filePath, sheet, this.Prop_HasHeader, skip, max))
                {
                    foreach (var row in reader)
                    {
                        SetVariableValue(this.Prop_RowValues, new List<string>(row), sd);
                        SetVariableValue(this.Prop_RowIndex, index, sd);
                        SetVariableValue(this.Prop_ColumnCount, row.Count, sd);
                        if (reader.Columns != null)
                            SetVariableValue(this.Prop_Columns, new List<string>(reader.Columns), sd);

                        var status = RunIteration(body, context, frame, $"строка Excel {index}");
                        if (status == IterationStatus.Fatal)
                            return; // EndProcess уже вызван с ошибкой
                        if (status == IterationStatus.Break)
                            break;
                        // Continue → следующая итерация
                        index++;
                    }
                }

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
        /// Одна итерация тела: body.Execute (движковая цепочка детей) +
        /// разбор результата — как в ForEachRowBack.
        /// </summary>
        private IterationStatus RunIteration(WFContainerBase body, ProcessContext context, LoopFrame frame, string iterationLabel)
        {
            ElementExecutionResults result = null;
            ElementExecutionFinishedDelegate handler = (s, r) => result = r;
            body.ElementExecutionFinished += handler;
            try
            {
                body.Execute(context);
            }
            finally
            {
                body.ElementExecutionFinished -= handler;
            }

            if (LoopControl.Current?.BreakRequested == true)
            {
                frame.BreakRequested = true;
                return IterationStatus.Break;
            }
            if (LoopControl.Current?.ContinueRequested == true)
                return IterationStatus.Continue;

            if (result != null && !result.IsSuccess)
            {
                if (!this.ContinueOnException)
                {
                    EndProcess(new ElementExecutionResults(
                        new ExecutionException(
                            $"[{sdkComponentName}] ошибка тела ({iterationLabel}): {result.ExceptionData?.Message ?? "неизвестная ошибка"}",
                            result.ExceptionData),
                        context));
                    return IterationStatus.Fatal;
                }
                Common.ActivityFileLogger.Warning(
                    sdkComponentName,
                    $"Ошибка вложенной активности ({iterationLabel}, Продолжить при ошибке): {result.ExceptionData?.Message}");
            }
            return IterationStatus.Success;
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
