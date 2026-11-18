// =============================================================================
// LoopContinueBack.cs — активность «Цикл: Продолжить».
// Ставит флаг Continue в кадре ближайшего цикла MIA; остаток тела текущей
// итерации пропускается, цикл переходит к следующей строке.
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
    public class LoopContinueBack : PrimoComponentTO<LoopContinue>
    {
        public override string GroupName { get => ActivityCategories.Utilities; protected set { } }

        protected override int sdkTimeOut { get => 10000; set { } }

        public LoopContinueBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_LoopContinue;
            sdkComponentHelp = @"Компонент ""Цикл: Продолжить""
Пропускает остаток тела текущей итерации ближайшего цикла MIA и переходит к следующей строке.

Размещение: внутри тела ForEach-контейнера MIA. Вложенные циклы: адресует ближайший (внутренний).

Примечание: работает только с циклами MIA; студийные циклы не обслуживает.";
            sdkComponentIcon = ActivityIcons.Loop;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>();
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            LoopControl.Continue();
            return new ExecutionResult { IsSuccess = true, SuccessMessage = "Переход к следующей итерации" };
        }

        public override ValidationResult Validate()
        {
            return new ValidationResult();
        }
    }
}
