// =============================================================================
// UtilitiesClearRunScopeBack.cs — активность «Утилиты: Очистка состояния
// прогона» (issue #27).
//
// Делегирует в RepoDict.ClearRunScope(): закрытие браузерных сессий
// (Quit + Dispose + снятие с учёта) и очистка пользовательских данных
// всех словарей RepoDict. Primo Studio исполняет роботов в разделяемом
// процессе — без явной очистки в конце прогона данные и WebDriver
// утекают между запусками разных роботов.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность очистки состояния прогона робота.</summary>
    public class UtilitiesClearRunScopeBack : PrimoComponentTO<UtilitiesClearRunScope>
    {
        public override string GroupName { get => ActivityCategories.Utilities; protected set { } }

        protected override int sdkTimeOut { get => 60000; set { } }

        private string _propClosedSessions;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ClosedSessions)]
        /// <summary>Количество закрытых браузерных сессий.</summary>
        public string Prop_ClosedSessions
        {
            get => _propClosedSessions;
            set { _propClosedSessions = value; InvokePropertyChanged(this, nameof(Prop_ClosedSessions)); }
        }

        public UtilitiesClearRunScopeBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_ClearRunScope;
            sdkComponentHelp = @"Компонент ""Утилиты: Очистка состояния прогона""
Очищает состояние прогона робота — вызывайте в конце сценария.

Что делает:
1. Закрывает все браузерные сессии (Quit + Dispose драйверов, снятие с учёта).
2. Очищает пользовательские данные RepoDict: таблицы, объекты, строки,
   числа, списки, JSON, SecureString-пароли, стек контекстов браузера.

Зачем: Primo Studio исполняет роботов в разделяемом процессе —
без очистки данные и браузеры утекают между запусками разных роботов.

Не затрагивается: системные словари (пути, pre-compiled regex).

Выходные данные:
Закрыто сессий: [Int32] Количество закрытых браузерных сессий.";
            sdkComponentIcon = ActivityIcons.Clean;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<int>("Prop_ClosedSessions", "Количество закрытых браузерных сессий")
            };

            InitClass(container);

            this.Prop_ClosedSessions = "\"\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var closed = RepoDict.ClearRunScope();
                if (!string.IsNullOrWhiteSpace(this.Prop_ClosedSessions))
                    SetVariableValue(this.Prop_ClosedSessions, closed, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[Утилиты] Состояние прогона очищено; закрыто сессий браузера: {closed}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка очистки состояния прогона: {ex.Message}"
                };
            }
        }
    }
}
