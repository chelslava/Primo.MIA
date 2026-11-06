using System;
using System.Windows;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Присоединённые свойства для привязки <see cref="PasswordBox.Password"/>.
    /// PasswordBox не поддерживает прямую привязку к Password (свойство без DP),
    /// поэтому используется стандартный паттерн «attached property»:
    /// <code>
    /// &lt;PasswordBox common:PasswordBoxHelper.BindPassword="True"
    ///              common:PasswordBoxHelper.BoundPassword="{Binding Prop_Password, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
    ///              Style="{StaticResource FieldPasswordBox}"/&gt;
    /// </code>
    /// Привязка остаётся на прежнее свойство активити ([StoringProperty]), поэтому
    /// сериализация Primo Studio не меняется; символы пароля не отображаются в UI.
    /// </summary>
    public static class PasswordBoxHelper
    {
        /// <summary>Включает синхронизацию PasswordChanged → BoundPassword.</summary>
        public static readonly DependencyProperty BindPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BindPassword", typeof(bool), typeof(PasswordBoxHelper),
                new PropertyMetadata(false, OnBindPasswordChanged));

        /// <summary>Привязываемое значение пароля (двусторонняя синхронизация с PasswordBox.Password).</summary>
        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BoundPassword", typeof(string), typeof(PasswordBoxHelper),
                new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

        /// <summary>Защита от повторного входа при взаимном обновлении.</summary>
        private static readonly DependencyProperty UpdatingPasswordProperty =
            DependencyProperty.RegisterAttached(
                "UpdatingPassword", typeof(bool), typeof(PasswordBoxHelper),
                new PropertyMetadata(false));

        /// <summary>Возвращает значение BindPassword.</summary>
        public static bool GetBindPassword(DependencyObject obj)
            => (bool)obj.GetValue(BindPasswordProperty);

        /// <summary>Устанавливает значение BindPassword.</summary>
        public static void SetBindPassword(DependencyObject obj, bool value)
            => obj.SetValue(BindPasswordProperty, value);

        /// <summary>Возвращает значение BoundPassword.</summary>
        public static string GetBoundPassword(DependencyObject obj)
            => (string)obj.GetValue(BoundPasswordProperty);

        /// <summary>Устанавливает значение BoundPassword.</summary>
        public static void SetBoundPassword(DependencyObject obj, string value)
            => obj.SetValue(BoundPasswordProperty, value);

        private static void OnBindPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PasswordBox box)
                return;

            if ((bool)e.OldValue)
                box.PasswordChanged -= HandlePasswordChanged;
            if ((bool)e.NewValue)
                box.PasswordChanged += HandlePasswordChanged;
        }

        private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not PasswordBox box)
                return;
            if ((bool)box.GetValue(UpdatingPasswordProperty))
                return;

            box.SetValue(UpdatingPasswordProperty, true);
            try
            {
                SetBoundPassword(box, box.Password);
            }
            finally
            {
                box.SetValue(UpdatingPasswordProperty, false);
            }
        }

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PasswordBox box)
                return;

            var newValue = (string)e.NewValue ?? string.Empty;
            if (!string.Equals(box.Password, newValue, StringComparison.Ordinal))
            {
                box.SetValue(UpdatingPasswordProperty, true);
                try
                {
                    box.Password = newValue;
                }
                finally
                {
                    box.SetValue(UpdatingPasswordProperty, false);
                }
            }
        }
    }
}
