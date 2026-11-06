using System;
using FluentAssertions;
using Moq;
using OpenQA.Selenium;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.Browser
{
    /// <summary>
    /// Тесты ElementInteractionLogic: валидация входных данных DragDrop
    /// (без живого браузера — проверяем контракты и обработку ошибок).
    /// </summary>
    [Trait("Category", "Fast")]
    public class ElementInteractionLogicTests
    {
        private readonly ElementInteractionLogic _logic;

        public ElementInteractionLogicTests()
        {
            _logic = new ElementInteractionLogic();
        }

        [Fact]
        public void DragDrop_NullDriver_ThrowsArgumentNull()
        {
            // Валидация до try-блока: контракт — исключение, а не результат с ошибкой
            Action act = () => _logic.DragDrop("#source", "#target", 5000, null);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void DragDrop_EmptySourceSelector_ThrowsArgumentException()
        {
            var driver = new Mock<IWebDriver>().Object;
            Action act = () => _logic.DragDrop("", "#target", 5000, driver);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void DragDrop_EmptyTargetSelector_ReturnsFailureResult()
        {
            // Пустая цель проверяется ПОСЛЕ поиска источника: на мок-драйвере
            // поиск источника завершится таймаутом — контракт тот же:
            // исключение не пробрасывается, возвращается результат с ошибкой
            var driver = new Mock<IWebDriver>().Object;
            var result = _logic.DragDrop("#source", "", 100, driver);
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void DragDrop_NonExistentElement_TimesOutWithFailure()
        {
            var driver = new Mock<IWebDriver>().Object;
            // Элемент не найден — SeleniumHelper.WaitForElement бросит по таймауту,
            // логика возвращает результат с ошибкой вместо исключения
            var result = _logic.DragDrop("#nonexistent", "#target", 1, driver);
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }
    }
}
