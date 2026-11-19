// =============================================================================
// LoopContinueBack.cs — активность «Цикл: Продолжить».
//
// Переход к следующей итерации ближайшего цикла MIA НЕМЕДЛЕННО: флаг
// Continue в кадре LoopControl + маркер-исключение — остаток тела текущей
// итерации не выполняется. База PrimoComponentSimple (без retry-петли
// sdkTimeOut — иначе маркер давился повторами).
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Перейти к следующей итерации ближайшего цикла MIA.</summary>
    public class LoopContinueBack : PrimoComponentSimple<LoopContinue>
    {
        public override string GroupName { get => ActivityCategories.Utilities; protected set { } }

        public LoopContinueBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_LoopContinue;
            sdkComponentHelp = @"Компонент ""Цикл: Продолжить""
Пропускает остаток тела текущей итерации ближайшего цикла MIA и немедленно переходит к следующей строке.

Размещение: внутри тела ForEach-контейнера MIA. Вложенные циклы: адресует ближайший (внутренний).

Примечания:
- Элемент фиксирует в логе ошибку-маркер перехода — это штатное поведение.
- Работает только с циклами MIA; студийные циклы не обслуживает.";
            sdkComponentIcon = ActivityIcons.Loop;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>();
            InitClass(container);
        }

        /// <summary>Флаг + маркер-исключение: остаток тела не выполняется.</summary>
        public override ExecutionResult SimpleAction(ScriptingData sd)
        {
            // контекст для дублирования сообщений в консоль Studio
            ActivityFileLogger.BeginConsole(sd);
            var frame = LoopControl.Current
                ?? throw new InvalidOperationException(
                    "Цикл: Продолжить — вне тела цикла. Разместите активность внутри ForEach-контейнера MIA.");
            frame.ContinueRequested = true;
            ActivityFileLogger.Debug(sdkComponentName, "маркер «Продолжить» установлен — переход к следующей итерации");
            throw new InvalidOperationException("Переход к следующей итерации: Цикл: Продолжить");
        }

        public override ValidationResult Validate()
        {
            return new ValidationResult();
        }
    }
}
