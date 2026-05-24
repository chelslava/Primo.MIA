using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using Primo.MIA.Models;

namespace Primo.MIA
{
    internal static class BrowserWindowHelper
    {
        internal static List<WindowInfo> GetAllWindows(
            IWebDriver driver,
            Action<string> logInfo,
            Action<string> logWarning)
        {
            var windows = new List<WindowInfo>();
            string currentHandle = driver.CurrentWindowHandle;

            foreach (string handle in driver.WindowHandles)
            {
                try
                {
                    driver.SwitchTo().Window(handle);
                    windows.Add(new WindowInfo
                    {
                        Handle = handle,
                        Title = driver.Title,
                        Url = driver.Url,
                        OpenedAt = DateTime.Now,
                        IsActive = handle == currentHandle
                    });
                }
                catch (Exception ex)
                {
                    logWarning($"Не удалось получить информацию об окне {handle}: {ex.Message}");
                }
            }

            driver.SwitchTo().Window(currentHandle);
            logInfo($"Найдено {windows.Count} окон");
            return windows;
        }

        internal static bool SwitchToWindowByTitle(
            IWebDriver driver,
            string title,
            Action<string> logInfo,
            Action<string> logWarning)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Заголовок не может быть пустым", nameof(title));

            string currentHandle = driver.CurrentWindowHandle;

            foreach (string handle in driver.WindowHandles)
            {
                try
                {
                    driver.SwitchTo().Window(handle);
                    if (driver.Title.Contains(title))
                    {
                        logInfo($"Переключено на окно с заголовком: {driver.Title}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    logWarning($"Ошибка при проверке окна {handle}: {ex.Message}");
                }
            }

            driver.SwitchTo().Window(currentHandle);
            logWarning($"Окно с заголовком '{title}' не найдено");
            return false;
        }

        internal static bool SwitchToWindowByUrl(
            IWebDriver driver,
            string url,
            Action<string> logInfo,
            Action<string> logWarning)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL не может быть пустым", nameof(url));

            string currentHandle = driver.CurrentWindowHandle;

            foreach (string handle in driver.WindowHandles)
            {
                try
                {
                    driver.SwitchTo().Window(handle);
                    if (driver.Url.Contains(url))
                    {
                        logInfo($"Переключено на окно с URL: {driver.Url}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    logWarning($"Ошибка при проверке окна {handle}: {ex.Message}");
                }
            }

            driver.SwitchTo().Window(currentHandle);
            logWarning($"Окно с URL '{url}' не найдено");
            return false;
        }

        internal static int CloseAllExceptMain(
            IWebDriver driver,
            string mainWindowHandle,
            Action<string> logInfo,
            Action<string> logWarning)
        {
            var handles = driver.WindowHandles;
            if (handles.Count <= 1)
            {
                logInfo("Открыто только одно окно, нечего закрывать");
                return 0;
            }

            if (string.IsNullOrEmpty(mainWindowHandle))
                mainWindowHandle = handles[0];

            int closedCount = 0;
            foreach (string handle in handles)
            {
                if (handle != mainWindowHandle)
                {
                    try
                    {
                        driver.SwitchTo().Window(handle);
                        driver.Close();
                        closedCount++;
                        logInfo($"Закрыто окно: {handle}");
                    }
                    catch (Exception ex)
                    {
                        logWarning($"Не удалось закрыть окно {handle}: {ex.Message}");
                    }
                }
            }

            driver.SwitchTo().Window(mainWindowHandle);
            logInfo($"Закрыто {closedCount} окон, осталось основное");
            return closedCount;
        }
    }
}
