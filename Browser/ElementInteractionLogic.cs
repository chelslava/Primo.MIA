using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Primo.MIA.Common;
using System;

namespace Primo.MIA
{
    public class DragDropResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ElementInteractionLogic
    {
        public DragDropResult DragDrop(
            string sourceSelector,
            string targetSelector,
            int timeoutMs,
            IWebDriver driver)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));
            if (string.IsNullOrWhiteSpace(sourceSelector))
                throw new ArgumentException("Исходный селектор не может быть пустым", nameof(sourceSelector));

            try
            {
                var sourceElement = SeleniumHelper.WaitForElement(
                    driver, By.CssSelector(sourceSelector), timeoutMs / 1000);

                if (!string.IsNullOrWhiteSpace(targetSelector))
                {
                    var targetElement = SeleniumHelper.WaitForElement(
                        driver, By.CssSelector(targetSelector), timeoutMs / 1000);

                    SeleniumHelper.DragAndDrop(driver, sourceElement, targetElement);
                }
                else
                {
                    throw new ArgumentException("Целевой селектор не может быть пустым", nameof(targetSelector));
                }

                return new DragDropResult { Success = true };
            }
            catch (Exception ex)
            {
                return new DragDropResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
