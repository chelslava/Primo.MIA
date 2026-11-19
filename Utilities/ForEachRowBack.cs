// =============================================================================
// ForEachRowBack.cs — контейнер «Таблица: Для каждой строки».
//
// Быстрый аналог студийного цикла по DataTable: строки отдаются по одной,
// тело цикла исполняется движком SDK на каждой итерации.
//
// Контракт движка (установлен декомпиляцией LTools.SDK 2026-11, см. базу
// знаний): PrimoContainerCustom.Execute(ProcessContext) переопределяется —
// дети (Components) вызываются напрямую child.Execute(context); каждый
// ребёнок самодостаточен (PrepareScripting внутри). ScriptingData для
// записи переменных — ScriptingHelper.PrepareScripting(this, context).
// Завершение — EndProcess (protected из WFElementBase): успех без ошибки
// / с ExecutionException. SimpleAction у движка не используется (Execute
// перекрывает цепочку) — оставлен тривиальным.
//
// Тело: дети по очереди; после каждого — проверка флагов LoopControl
// (Цикл: Прервать / Цикл: Продолжить). Ошибка ребёнка: ContinueOnException
// контейнера → продолжить, иначе завершить с ошибкой.
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
using System.Data;
using System.Reflection;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Контейнер перебора строк DataTable.</summary>
    public class ForEachRowBack : PrimoContainerCustom<ForEachRow>
    {
        public override string GroupName { get => ActivityCategories.Utilities; protected set { } }

        // ── Входные ─────────────────────────────────────────────────────

        private string _propTable;
        /// <summary>Переменная DataTable для перебора.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_LoopTable)]
        public string Prop_Table
        {
            get => _propTable;
            set { _propTable = value; InvokePropertyChanged(this, nameof(Prop_Table)); }
        }

        private string _propSkipRows;
        /// <summary>Пропустить первые N строк.</summary>
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

        private string _propCurrentRow;
        /// <summary>Текущая строка (DataRow).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataRow))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_LoopCurrentRow)]
        public string Prop_CurrentRow
        {
            get => _propCurrentRow;
            set { _propCurrentRow = value; InvokePropertyChanged(this, nameof(Prop_CurrentRow)); }
        }

        private string _propRowIndex;
        /// <summary>Индекс строки (0-based).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_LoopRowIndex)]
        public string Prop_RowIndex
        {
            get => _propRowIndex;
            set { _propRowIndex = value; InvokePropertyChanged(this, nameof(Prop_RowIndex)); }
        }

        private string _propColumnCount;
        /// <summary>Число колонок таблицы.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_LoopCellCount)]
        public string Prop_ColumnCount
        {
            get => _propColumnCount;
            set { _propColumnCount = value; InvokePropertyChanged(this, nameof(Prop_ColumnCount)); }
        }

        public ForEachRowBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_ForEachRow;
            sdkComponentHelp = @"Компонент ""Таблица: Для каждой строки""
Быстрый цикл по строкам DataTable: вложенные активности исполняются один раз на каждую строку. Аналог студийного «Для каждой» — без промежуточных списков и с прямым доступом к строке.

Основные:
Таблица*: [DataTable] Переменная для перебора.

Настройки:
Пропустить строк: [Int32] Начать с индекса N (0 — с первой).
Максимум строк: [Int32] Ограничение итераций (0 — все).
Отладочная пауза: [Bool] Окно «Продолжить / Прервать цикл» перед каждой итерацией — замена брейкпоинта внутри тела (студийная отладка внутрь тела цикла не заходит).

Выходные данные:
Текущая строка: [DataRow] Строка текущей итерации (обращение к колонкам: ТекущаяСтрока(""Колонка"")).
Индекс строки: [Int32] 0-based индекс итерации.
Колонок: [Int32] Число колонок таблицы.

Тело цикла:
Внутрь помещаются любые активности — они выполняются на каждую строку.
Внутри доступны активности «Цикл: Прервать» и «Цикл: Продолжить».
Ошибка вложенной активности останавливает цикл (флаг «Продолжить при ошибке» элемента — продолжить со следующей строки).";
            sdkComponentIcon = ActivityIcons.Loop;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<DataTable>("Prop_Table", "Таблица для перебора"),
                PropertyBuilder.Int("Prop_SkipRows", "Пропустить первые N строк"),
                PropertyBuilder.Int("Prop_MaxRows", "Максимум итераций (0 — все)"),
                PropertyBuilder.Variable<DataRow>("Prop_CurrentRow", "Текущая строка (DataRow)"),
                PropertyBuilder.Variable<int>("Prop_RowIndex", "Индекс строки (Int32)"),
                PropertyBuilder.Variable<int>("Prop_ColumnCount", "Число колонок (Int32)")
            };
            InitClass(container);

            // Тело цикла = модельный контейнер (приватное поле cont базового
            // класса — именно в него движок загружает сохранённых детей).
            // Захват в конструкторе: в robot-режиме element.Container
            // обнуляется, UIControl в Execute даёт NRE. Цепочка источников:
            // рефлексия cont → вид → LastInstance.
            _body = ResolveBody();
            // значения по умолчанию: строковые переменные — "" (в кавычках),
            // числовые — БЕЗ кавычек (тип должен совпадать с ValidateReturnScript)
            this.Prop_Table = "\"\"";
            this.Prop_CurrentRow = "\"\"";
            this.Prop_SkipRows = "0";
            this.Prop_MaxRows = "0";
        }

        /// <summary>
        /// Модельный контейнер тела: 1) приватное поле cont базового класса
        /// (рефлексия — ровно тот экземпляр, в который движок грузит детей);
        /// 2) тело вида; 3) тело последнего экземпляра вида.
        /// </summary>
        private WFContainerBase ResolveBody()
        {
            try
            {
                var field = typeof(PrimoContainerCustom<ForEachRow>).GetField(
                    "cont", BindingFlags.NonPublic | BindingFlags.Instance);
                var viaReflection = field?.GetValue(this) as WFContainerBase;
                if (viaReflection != null)
                    return viaReflection;
            }
            catch (Exception ex)
            {
                Common.ActivityFileLogger.Warning(sdkComponentName, $"cont через рефлексию: {ex.Message}");
            }

            try
            {
                var viaView = ((ForEachRow)this.UIControl).Body;
                if (viaView != null)
                    return viaView;
            }
            catch (Exception ex)
            {
                Common.ActivityFileLogger.Warning(sdkComponentName, $"cont через вид: {ex.Message}");
            }

            var last = ForEachRow.LastInstance?.Body;
            if (last != null)
                return last;

            _bodyCaptureError = "не найден ни одним из способов (reflexion/вид/LastInstance)";
            return null;
        }

        /// <summary>Тело цикла (WFContainerBase из вида), захваченное в конструкторе.</summary>
        private WFContainerBase _body;

        /// <summary>Причина неудачи захвата тела в конструкторе (диагностика).</summary>
        private string _bodyCaptureError;

        /// <summary>Движком не вызывается: Execute перекрывает стандартную цепочку. Тривиально для контракта.</summary>
        public override ExecutionResult SimpleAction(ScriptingData sd)
        {
            return new ExecutionResult { IsSuccess = true, SuccessMessage = "Цикл по строкам таблицы" };
        }

        /// <summary>
        /// Точка входа: перебор строк; на каждой итерации — запись переменных
        /// и исполнение ТЕЛА через body.Execute(context) — полная движковая
        /// цепочка: пошаговая отладка Studio, брейкпоинты и ContinueOnException
        /// детей работают как в стандартных активностях.
        /// </summary>
        public override void Execute(ProcessContext context)
        {
            try
            {
                var sd = ScriptingHelper.PrepareScripting(this, context);
                // контекст для дублирования сообщений (в т.ч. детей цикла) в консоль Studio
                ActivityFileLogger.BeginConsole(sd);

                var tableObj = GetPropertyValue<object>(this.Prop_Table, nameof(Prop_Table), sd);
                DataTable table;
                if (tableObj is DataTable dt)
                {
                    table = dt;
                }
                else if (tableObj == null || (tableObj is string st && string.IsNullOrWhiteSpace(st)))
                {
                    throw new InvalidOperationException(
                        ActivityStrings.Error_LoopTableRequired + " (переменная не привязана или пуста)");
                }
                else
                {
                    throw new InvalidOperationException(
                        $"{ActivityStrings.Error_LoopTableRequired}: получен тип {tableObj.GetType().Name}, ожидался DataTable");
                }

                int skip = GetInt(sd, this.Prop_SkipRows, nameof(Prop_SkipRows), 0);
                int max = GetInt(sd, this.Prop_MaxRows, nameof(Prop_MaxRows), 0);

                int rowCount = table.Rows.Count;
                int started = Math.Min(skip, rowCount);
                int remaining = rowCount - started;
                int limit = max > 0 ? Math.Min(max, remaining) : remaining;

                SetVariableValue(this.Prop_ColumnCount, table.Columns.Count, sd);

                ActivityFileLogger.Debug(sdkComponentName,
                    $"цикл по DataTable: строк {rowCount}, skip {started}, максимум {limit}, колонок {table.Columns.Count}");

                // Тело: дети из this.Components (модель контейнера) исполняются
                // напрямую — подтверждено рабочим прогоном. Движковая цепочка
                // cont.ExecuteNext в robot-режиме не содержит детей.
                var children = this.Components;
                int childCount = children?.Count ?? 0;
                if (childCount == 0)
                    throw new InvalidOperationException(
                        "Тело цикла пусто: помещайте вложенные активности ВНУТРЬ контейнера");

                var frame = new LoopFrame();
                int iterations = 0;
                using (LoopControl.Push(frame))
                {
                    for (int i = 0; i < limit; i++)
                    {
                        var row = table.Rows[started + i];
                        SetVariableValue(this.Prop_CurrentRow, row, sd);
                        SetVariableValue(this.Prop_RowIndex, started + i, sd);

                        // прогресс итерации в отладочный лог Studio (событие
                        // ElementExecutionInfo поднимается рефлексией — базовый
                        // класс не даёт подклассам бросать свои события)
                        FireExecutionInfo(context, $"итерация {started + i} из {limit}");

                        // отладочная пауза: диалог «Продолжить/Прервать»
                        if (this.Prop_DebugPause && DebugPause(row, started + i, table))
                            break;

                        var status = RunBodyDirect(children, context, frame, $"строка {started + i}")
                            ? IterationStatus.Success
                            : IterationStatus.Fatal;

                        iterations++;
                        if (status == IterationStatus.Fatal)
                            return; // EndProcess уже вызван с ошибкой
                        if (status == IterationStatus.Break)
                            break;
                        // Continue → следующая итерация
                    }
                }

                EndProcess(new ElementExecutionResults(context));
                Common.ActivityFileLogger.Info(sdkComponentName,
                    $"цикл завершён: итераций {iterations}, активностей в теле {childCount}");
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
        /// Отладочная пауза: окно с индексом итерации и значениями ячеек строки.
        /// Возврат true — пользователь выбрал «Прервать цикл».
        /// </summary>
        private static bool DebugPause(DataRow row, int rowIndex, DataTable table)
        {
            var values = string.Join(";  ",
                table.Columns.Cast<DataColumn>().Select(c => $"{c.ColumnName} = {row[c]}"));
            if (values.Length > 1200)
                values = values.Substring(0, 1200) + " …";
            return LoopDebugPause.Show(
                $"Итерация {rowIndex}{Environment.NewLine}{Environment.NewLine}{values}",
                ActivityStrings.LoopPauseCaption);
        }

        /// <summary>
        /// Прямое исполнение тела: дети из модельного контейнера
        /// (this.Components) вызываются напрямую. Ошибка ребёнка: при
        /// ContinueOnException — продолжить, иначе — остановить цикл.
        /// Возврат false — тело прервано ошибкой (цикл завершается).
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
                        // пробрасываем ошибку вверх — цикл останавливается
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
                    return true; // флаги обрабатывает внешний цикл
            }
            return true;
        }

        /// <summary>
        /// Одна итерация тела: body.Execute (движковая цепочка детей) +
        /// разбор результата: управляемые Break/Continue (флаги кадра,
        /// выставленные активностями до маркер-исключения) / ошибка ребёнка
        /// (ContinueOnException контейнера) / успех.
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

            // управляемые флаги имеют приоритет над ошибкой-маркером
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
                    // пробрасываем ошибку вверх — цикл останавливается
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

        /// <summary>
        /// Прогресс итерации в отладочный лог Studio: поднимает событие
        /// ElementExecutionInfo базового класса через рефлексию (событие
        /// объявлено в базовом классе — подкласс не может бросить напрямую).
        /// </summary>
        private void FireExecutionInfo(ProcessContext context, string text)
        {
            try
            {
                var field = typeof(PrimoContainerCustom<ForEachRow>).GetField(
                    "ElementExecutionInfo", BindingFlags.NonPublic | BindingFlags.Instance);
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
            ret.ValidateRequired(this.Prop_Table, ActivityStrings.Field_LoopTable, ActivityStrings.Error_LoopTableRequired);
            return ret;
        }
    }
}
