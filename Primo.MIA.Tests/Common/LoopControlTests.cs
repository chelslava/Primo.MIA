using System;
using FluentAssertions;
using Primo.MIA.Common;
using Xunit;

namespace Primo.MIA.Tests.Common
{
    /// <summary>
    /// Тесты управления циклами (LoopControl): стек кадров, Break/Continue,
    /// вложенность, гарантированное снятие кадра, ошибка вне цикла.
    /// </summary>
    [Trait("Category", "Fast")]
    public class LoopControlTests
    {
        private static void CleanStack() => LoopControl.Clear();

        [Fact(DisplayName = "Вне цикла: Current = null, Break/Continue бросают понятную ошибку")]
        public void OutsideLoop_NullAndThrows()
        {
            CleanStack();
            LoopControl.Current.Should().BeNull();

            Action brk = () => LoopControl.Break();
            Action cont = () => LoopControl.Continue();
            brk.Should().Throw<InvalidOperationException>().WithMessage("*вне тела цикла*");
            cont.Should().Throw<InvalidOperationException>().WithMessage("*вне тела цикла*");
            LoopControl.Depth.Should().Be(0);
        }

        [Fact(DisplayName = "Push/Dispose: кадр снимается, флаги изолированы")]
        public void PushDispose_FrameLifecycle()
        {
            CleanStack();
            var frame = new LoopFrame();
            using (LoopControl.Push(frame))
            {
                LoopControl.Current.Should().BeSameAs(frame);
                LoopControl.Continue();
                frame.ContinueRequested.Should().BeTrue();
                frame.ContinueRequested = false;
                LoopControl.Break();
                frame.BreakRequested.Should().BeTrue();
            }
            LoopControl.Current.Should().BeNull();
        }

        [Fact(DisplayName = "Вложенные циклы: Break/Continue адресуют верхний кадр")]
        public void Nested_InnerTargeted()
        {
            CleanStack();
            var outer = new LoopFrame();
            var inner = new LoopFrame();
            using (LoopControl.Push(outer))
            {
                using (LoopControl.Push(inner))
                {
                    LoopControl.Break();
                    inner.BreakRequested.Should().BeTrue("прерывание адресует внутренний цикл");
                    outer.BreakRequested.Should().BeFalse();
                }
                LoopControl.Current.Should().BeSameAs(outer);
                LoopControl.Continue();
                outer.ContinueRequested.Should().BeTrue();
            }
        }

        [Fact(DisplayName = "Dispose: снимает свой кадр; кадры внешних областей не трогает")]
        public void Dispose_IsResilient()
        {
            CleanStack();
            var stray = new LoopFrame();
            var mine = new LoopFrame();
            LoopControl.Push(stray); // кадр внешней области (ниже моего)
            var scope = LoopControl.Push(mine);
            scope.Dispose(); // снимает только мой кадр и всё выше него

            LoopControl.Current.Should().BeSameAs(stray, "кадр внешней области остаётся живым");
            CleanStack();
            LoopControl.Depth.Should().Be(0);
        }

        [Fact(DisplayName = "Эмуляция цикла: Break завершает, Continue пропускает остаток тела")]
        public void SimulatedLoop_Semantics()
        {
            CleanStack();
            // внешний цикл по 3 строкам, тело из 2 «активностей»;
            // на второй строке — Продолжить, на третьей — Прервать
            var executed = new System.Collections.Generic.List<string>();

            for (int row = 0; row < 3; row++)
            {
                var frame = new LoopFrame();
                using (LoopControl.Push(frame))
                {
                    foreach (var step in new[] { "A", "B" })
                    {
                        if (row == 1 && step == "B")
                        {
                            LoopControl.Continue();
                            break; // флаг проверяется после шага — не добавляем в выполненные
                        }
                        if (row == 2)
                            LoopControl.Break();
                        executed.Add($"{row}:{step}");
                        if (frame.BreakRequested || frame.ContinueRequested)
                            break;
                    }
                    if (frame.BreakRequested)
                        break;
                }
            }

            executed.Should().ContainInOrder("0:A", "0:B", "1:A", "2:A");
            executed.Should().HaveCount(4);
        }
    }
}
