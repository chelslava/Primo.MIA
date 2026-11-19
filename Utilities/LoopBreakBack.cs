// =============================================================================
// LoopBreakBack.cs — активность «Цикл: Прервать».
//
// Прерывает ближайший цикл MIA НЕМЕДЛЕННО: выставляет флаг Break в кадре
// LoopControl и бросает маркер-исключение — движок останавливает остаток
// тела, ForEach перехватывает по флагу и завершает перебор штатно.
// База PrimoComponentSimple (не TO): SimpleAction выполняется ОДИН раз,
// исключение не ретраится (иначе.retry-петля sdkTimeOut давила бы маркер).
// Элемент завершается ошибкой-маркером в логе — штатное поведение.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Прервать ближайший цикл MIA (выход из всех итераций).</summary>
    public class LoopBreakBack : PrimoComponentSimple<LoopBreak>
    {
        public override string GroupName { get => ActivityCategories.Utilities; protected set { } }

        public LoopBreakBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_LoopBreak;
            sdkComponentHelp = @"Компонент ""Цикл: Прервать""
Немедленно прекращает выполнение ближайшего цикла MIA («Таблица: Для каждой строки», «Excel: Для каждой строки») — выход из всех итераций; остаток тела текущей итерации не выполняется.

Размещение: внутри тела ForEach-контейнера MIA. Вложенные циклы: прерывает ближайший (внутренний).

Примечания:
- Элемент фиксирует в логе ошибку-маркер прерывания — это штатное поведение.
- Работает только с циклами MIA; студийные циклы не прерывает.";
            sdkComponentIcon = ActivityIcons.Loop;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>();
            InitClass(container);
        }

        /// <summary>Флаг + маркер-исключение: движок останавливает остаток тела немедленно.</summary>
        public override ExecutionResult SimpleAction(ScriptingData sd)
        {
            // контекст для дублирования сообщений в консоль Studio
            ActivityFileLogger.BeginConsole(sd);
            var frame = LoopControl.Current
                ?? throw new InvalidOperationException(
                    "Цикл: Прервать — вне тела цикла. Разместите активность внутри ForEach-контейнера MIA.");
            frame.BreakRequested = true;
            ActivityFileLogger.Debug(sdkComponentName, "маркер «Прервать» установлен — остаток тела не выполняется");
            throw new InvalidOperationException("Прервано пользователем: Цикл: Прервать");
        }

        public override ValidationResult Validate()
        {
            return new ValidationResult();
        }
    }
}
