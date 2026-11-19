// =============================================================================
// LoopDebugPause.cs — модальное окно отладочной паузы контейнеров циклов MIA.
//
// Кнопки «Продолжить» / «Прервать цикл» (тексты — ActivityStrings). Крестик
// окна и Enter означают «Продолжить»; прервать можно ТОЛЬКО явной кнопкой.
// Активности исполняются на MTA-потоках — WPF-окно требует STA: при MTA
// диалог открывается на временном STA-потоке (основной поток блокируется
// до закрытия окна — цикл стоит на паузе).
// =============================================================================

using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Primo.MIA.Common
{
    /// <summary>Модальное окно отладочной паузы «Продолжить / Прервать цикл».</summary>
    internal static class LoopDebugPause
    {
        /// <summary>Показ окна. Возврат true — пользователь выбрал «Прервать цикл».</summary>
        public static bool Show(string text, string caption)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                return ShowDialog(text, caption);

            bool breakLoop = false;
            var t = new Thread(() => breakLoop = ShowDialog(text, caption));
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            return breakLoop;
        }

        private static bool ShowDialog(string text, string caption)
        {
            bool breakLoop = false;

            var btnContinue = new Button
            {
                Content = ActivityStrings.LoopPauseContinue,
                IsDefault = true, // Enter — продолжить
                MinWidth = 110,
                Height = 26,
                Margin = new Thickness(0, 0, 8, 0)
            };
            var btnBreak = new Button
            {
                Content = ActivityStrings.LoopPauseBreak,
                MinWidth = 110,
                Height = 26
            };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 14, 0, 0)
            };
            buttons.Children.Add(btnContinue);
            buttons.Children.Add(btnBreak);

            var msg = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 560
            };

            var root = new StackPanel { Margin = new Thickness(16) };
            root.Children.Add(msg);
            root.Children.Add(buttons);

            var win = new Window
            {
                Title = caption,
                Content = root,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Topmost = true // поверх Studio — пауза не должна теряться за окном
            };

            btnContinue.Click += (s, e) => win.Close();
            btnBreak.Click += (s, e) => { breakLoop = true; win.Close(); };
            // крестик / Alt+F4 — закрытие без флага = «Продолжить»

            win.ShowDialog();
            return breakLoop;
        }
    }
}
