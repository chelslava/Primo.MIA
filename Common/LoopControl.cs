// =============================================================================
// LoopControl.cs — управление циклами MIA: «Прервать» / «Продолжить»
// (контейнеры ForEachRow / ForEachExcelRow).
//
// Цикл кладёт кадр в ThreadStatic-стек на время итерации; активности
// LoopBreak / LoopContinue внутри тела выставляют флаги верхнего кадра.
// Синхронное исполнение (движок SDK гоняет детей в том же потоке —
// ExecuteTask.RunSynchronously) делает ThreadStatic-стек достаточным.
// Вложенные циклы: Break/Continue адресуют ближайший (верхний) кадр.
// =============================================================================

using System;
using System.Collections.Generic;

namespace Primo.MIA.Common
{
    /// <summary>Кадр исполнения цикла: флаги управления.</summary>
    public class LoopFrame
    {
        /// <summary>Прервать цикл целиком (выход из всех итераций).</summary>
        public bool BreakRequested { get; set; }

        /// <summary>Перейти к следующей итерации (пропустить остаток тела).</summary>
        public bool ContinueRequested { get; set; }
    }

    /// <summary>
    /// Стек активных циклов текущего потока + статические входы для
    /// активностей Break/Continue.
    /// </summary>
    public static class LoopControl
    {
        [ThreadStatic]
        private static Stack<LoopFrame> _stack;

        private static Stack<LoopFrame> Stack => _stack ?? (_stack = new Stack<LoopFrame>());

        /// <summary>Число активных кадров (диагностика, тесты).</summary>
        public static int Depth => Stack.Count;

        /// <summary>Полная очистка кадров текущего потока (тесты).</summary>
        public static void Clear() => Stack.Clear();

        /// <summary>Цикл открывает кадр; верните IDisposable для гарантированного снятия.</summary>
        public static IDisposable Push(LoopFrame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));
            Stack.Push(frame);
            return new FrameScope(frame);
        }

        /// <summary>Верхний кадр или null (вне цикла).</summary>
        public static LoopFrame Current => Stack.Count > 0 ? Stack.Peek() : null;

        /// <summary>Прервать ближайший цикл (активность «Цикл: Прервать»).</summary>
        public static void Break()
        {
            var frame = Current
                ?? throw new InvalidOperationException(
                    "Цикл: Прервать — вне тела цикла. Разместите активность внутри ForEach-контейнера MIA.");
            frame.BreakRequested = true;
        }

        /// <summary>Перейти к следующей итерации (активность «Цикл: Продолжить»).</summary>
        public static void Continue()
        {
            var frame = Current
                ?? throw new InvalidOperationException(
                    "Цикл: Продолжить — вне тела цикла. Разместите активность внутри ForEach-контейнера MIA.");
            frame.ContinueRequested = true;
        }

        /// <summary>Снимает кадр, открытый соответствующим Push.</summary>
        private sealed class FrameScope : IDisposable
        {
            private readonly LoopFrame _frame;
            private bool _disposed;

            public FrameScope(LoopFrame frame) => _frame = frame;

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                // снимаем ровно до нашего кадра (защита от рассинхрона)
                while (Stack.Count > 0 && !ReferenceEquals(Stack.Peek(), _frame))
                    Stack.Pop();
                if (Stack.Count > 0 && ReferenceEquals(Stack.Peek(), _frame))
                    Stack.Pop();
            }
        }
    }
}
