// =============================================================================
// LoopBreakBack.cs — активность «Цикл: Прервать».
// Ставит флаг Break в кадре ближайшего цикла MIA; ForEachRow/ForEachExcelRow
// проверяют флаг после каждого ребёнка и завершают перебор.
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
    public class LoopBreakBack : PrimoComponentTO<LoopBreak>
    {
        public override string GroupName { get => ActivityCategories.Utilities; protected set { } }

        protected override int sdkTimeOut { get => 10000; set { } }

        public LoopBreakBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_LoopBreak;
            sdkComponentHelp = @"Компонент ""Цикл: Прервать""
Немедленно прекращает выполнение ближайшего цикла MIA («Таблица: Для каждой строки», «Excel: Для каждой строки») — выход из всех итераций.

Размещение: внутри тела ForEach-контейнера MIA. Вложенные циклы: прерывает ближайший (внутренний).

Примечание: работает только с циклами MIA; студийные циклы не прерывает.";
            sdkComponentIcon = ActivityIcons.Loop;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>();
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            LoopControl.Break();
            return new ExecutionResult { IsSuccess = true, SuccessMessage = "Цикл прерван" };
        }

        public override ValidationResult Validate()
        {
            return new ValidationResult();
        }
    }
}
