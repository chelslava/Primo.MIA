using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;

namespace Primo.MIA
{
    public class WaitResult
    {
        public bool Success { get; set; }
        public string ActualState { get; set; }
        public TimeSpan Elapsed { get; set; }
    }

    public class BrowserWaitLogic
    {
        public WaitResult WaitFor(
            WaitConditionType condition,
            int timeoutMs,
            int pollingIntervalMs,
            IWebDriver driver,
            By locator = null,
            string text = null)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            var startTime = DateTime.Now;

            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(timeoutMs))
                {
                    PollingInterval = TimeSpan.FromMilliseconds(pollingIntervalMs > 0 ? pollingIntervalMs : 500)
                };

                bool conditionMet = EvaluateCondition(wait, driver, condition, locator, text);
                var elapsed = DateTime.Now - startTime;

                return new WaitResult
                {
                    Success = conditionMet,
                    ActualState = condition.ToString(),
                    Elapsed = elapsed
                };
            }
            catch (WebDriverTimeoutException)
            {
                return new WaitResult
                {
                    Success = false,
                    ActualState = $"Таймаут: условие {condition} не выполнено за {timeoutMs}мс",
                    Elapsed = DateTime.Now - startTime
                };
            }
        }

        private bool EvaluateCondition(
            WebDriverWait wait,
            IWebDriver driver,
            WaitConditionType condition,
            By locator,
            string text)
        {
            switch (condition)
            {
                case WaitConditionType.ElementExists:
                    wait.Until(drv => drv.FindElement(locator));
                    return true;

                case WaitConditionType.ElementVisible:
                    wait.Until(drv =>
                    {
                        try { var el = drv.FindElement(locator); return el.Displayed; }
                        catch { return false; }
                    });
                    return true;

                case WaitConditionType.ElementClickable:
                    wait.Until(drv =>
                    {
                        try { var el = drv.FindElement(locator); return el.Displayed && el.Enabled; }
                        catch { return false; }
                    });
                    return true;

                case WaitConditionType.ElementInvisible:
                    wait.Until(drv =>
                    {
                        try { var el = drv.FindElement(locator); return !el.Displayed; }
                        catch (NoSuchElementException) { return true; }
                    });
                    return true;

                case WaitConditionType.TextPresent:
                    wait.Until(drv =>
                    {
                        try { var el = drv.FindElement(locator); return el.Text.Contains(text ?? ""); }
                        catch { return false; }
                    });
                    return true;

                case WaitConditionType.TitleContains:
                    wait.Until(drv => drv.Title.Contains(text ?? ""));
                    return true;

                case WaitConditionType.UrlContains:
                    wait.Until(drv => drv.Url.Contains(text ?? ""));
                    return true;

                case WaitConditionType.AlertPresent:
                    wait.Until(drv =>
                    {
                        try { drv.SwitchTo().Alert(); return true; }
                        catch (NoAlertPresentException) { return false; }
                    });
                    return true;

                default:
                    throw new NotSupportedException($"Условие {condition} не поддерживается");
            }
        }
    }
}
