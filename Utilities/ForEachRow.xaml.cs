using LTools.Common.Helpers;
using LTools.Common.WFItems;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Представление контейнера «Таблица: Для каждой строки».
    /// Паттерн SDK custom-container (docs.primo-rpa.ru): при рендере в Studio
    /// XAML создаёт WFContainerBase; в робо-режиме (DoRender = false) контейнер
    /// создаётся кодом — иначе InitClass не найдёт тело (throws
    /// «Container of type WFContainerBase not found»).
    /// </summary>
    public partial class ForEachRow : UserControl
    {
        private WFContainerBase _cntBody;

        /// <summary>
        /// Последнее созданное представление этого типа. В robot-режиме это
        /// экземпляр текущего прогона — Back использует его как fallback
        /// для получения тела цикла.
        /// </summary>
        public static ForEachRow LastInstance;

        /// <summary>Тело цикла: поле из XAML (Studio) либо созданное кодом (робот).</summary>
        public WFContainerBase Body => cntBody ?? _cntBody;

        public ForEachRow()
        {
            LastInstance = this;
            if (WFHelper.DoRender)
                InitializeComponent();
            else
            {
                _cntBody = new WFContainerBase();
                Content = _cntBody;
            }
        }
    }
}
