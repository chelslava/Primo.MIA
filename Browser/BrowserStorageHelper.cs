using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using OpenQA.Selenium;
using Primo.MIA.Models;

namespace Primo.MIA
{
    internal static class BrowserStorageHelper
    {
        internal static string ExportCookiesToJson(
            IWebDriver driver,
            Action<string> logInfo,
            Action<string, Exception> logError)
        {
            try
            {
                var cookieCollection = new CookieCollection
                {
                    SourceUrl = driver.Url,
                    ExportedAt = DateTime.Now
                };

                foreach (var cookie in driver.Manage().Cookies.AllCookies)
                {
                    cookieCollection.Cookies.Add(new CookieData
                    {
                        Name = cookie.Name,
                        Value = cookie.Value,
                        Domain = cookie.Domain,
                        Path = cookie.Path,
                        Expiry = cookie.Expiry,
                        Secure = cookie.Secure,
                        HttpOnly = cookie.IsHttpOnly,
                        SameSite = cookie.SameSite
                    });
                }

                string json = JsonConvert.SerializeObject(cookieCollection, Formatting.Indented);
                logInfo($"Экспортировано {cookieCollection.Cookies.Count} cookies");
                return json;
            }
            catch (Exception ex)
            {
                logError("Ошибка экспорта cookies", ex);
                throw;
            }
        }

        internal static int ImportCookiesFromJson(
            IWebDriver driver,
            string json,
            Action<string> logInfo,
            Action<string> logWarning,
            Action<string, Exception> logError)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON не может быть пустым", nameof(json));

            try
            {
                var cookieCollection = JsonConvert.DeserializeObject<CookieCollection>(json);
                if (cookieCollection?.Cookies == null)
                    throw new ArgumentException("Некорректный формат JSON cookies");

                int importedCount = 0;
                foreach (var cookieData in cookieCollection.Cookies)
                {
                    try
                    {
                        var cookie = new Cookie(
                            cookieData.Name, cookieData.Value,
                            cookieData.Domain, cookieData.Path, cookieData.Expiry);
                        driver.Manage().Cookies.AddCookie(cookie);
                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        logWarning($"Не удалось импортировать cookie '{cookieData.Name}': {ex.Message}");
                    }
                }

                logInfo($"Импортировано {importedCount} из {cookieCollection.Cookies.Count} cookies");
                return importedCount;
            }
            catch (Exception ex)
            {
                logError("Ошибка импорта cookies", ex);
                throw;
            }
        }

        internal static List<Cookie> FilterCookiesByDomain(
            IWebDriver driver,
            string domain,
            Action<string> logInfo)
        {
            if (string.IsNullOrWhiteSpace(domain))
                throw new ArgumentException("Домен не может быть пустым", nameof(domain));

            var filtered = new List<Cookie>();
            foreach (var cookie in driver.Manage().Cookies.AllCookies)
            {
                if (cookie.Domain != null && cookie.Domain.Contains(domain))
                    filtered.Add(cookie);
            }

            logInfo($"Найдено {filtered.Count} cookies для домена '{domain}'");
            return filtered;
        }

        internal static List<Cookie> FilterCookiesByName(
            IWebDriver driver,
            string namePattern,
            Action<string> logInfo)
        {
            if (string.IsNullOrWhiteSpace(namePattern))
                throw new ArgumentException("Паттерн не может быть пустым", nameof(namePattern));

            var filtered = new List<Cookie>();
            foreach (var cookie in driver.Manage().Cookies.AllCookies)
            {
                if (cookie.Name != null && cookie.Name.Contains(namePattern))
                    filtered.Add(cookie);
            }

            logInfo($"Найдено {filtered.Count} cookies с именем содержащим '{namePattern}'");
            return filtered;
        }

        internal static string ManageLocalStorage(
            IWebDriver driver,
            string operation,
            string key,
            string value,
            Action<string> logInfo,
            Action<string, Exception> logError)
        {
            IJavaScriptExecutor js = driver as IJavaScriptExecutor;
            if (js == null)
                throw new InvalidOperationException("WebDriver не поддерживает JavaScript");

            try
            {
                switch (operation.ToLower())
                {
                    case "get":
                        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("key обязателен", nameof(key));
                        var getResult = js.ExecuteScript($"return localStorage.getItem('{key}');");
                        logInfo($"localStorage.getItem('{key}') = {getResult}");
                        return getResult?.ToString();

                    case "set":
                        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("key обязателен", nameof(key));
                        js.ExecuteScript($"localStorage.setItem('{key}', '{value}');");
                        logInfo($"localStorage.setItem('{key}', '{value}')");
                        return null;

                    case "remove":
                        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("key обязателен", nameof(key));
                        js.ExecuteScript($"localStorage.removeItem('{key}');");
                        logInfo($"localStorage.removeItem('{key}')");
                        return null;

                    case "clear":
                        js.ExecuteScript("localStorage.clear();");
                        logInfo("localStorage.clear()");
                        return null;

                    default:
                        throw new ArgumentException($"Неизвестная операция: {operation}");
                }
            }
            catch (Exception ex)
            {
                logError($"Ошибка работы с localStorage: {operation}", ex);
                throw;
            }
        }

        internal static string ManageSessionStorage(
            IWebDriver driver,
            string operation,
            string key,
            string value,
            Action<string> logInfo,
            Action<string, Exception> logError)
        {
            IJavaScriptExecutor js = driver as IJavaScriptExecutor;
            if (js == null)
                throw new InvalidOperationException("WebDriver не поддерживает JavaScript");

            try
            {
                switch (operation.ToLower())
                {
                    case "get":
                        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("key обязателен", nameof(key));
                        var getResult = js.ExecuteScript($"return sessionStorage.getItem('{key}');");
                        logInfo($"sessionStorage.getItem('{key}') = {getResult}");
                        return getResult?.ToString();

                    case "set":
                        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("key обязателен", nameof(key));
                        js.ExecuteScript($"sessionStorage.setItem('{key}', '{value}');");
                        logInfo($"sessionStorage.setItem('{key}', '{value}')");
                        return null;

                    case "remove":
                        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("key обязателен", nameof(key));
                        js.ExecuteScript($"sessionStorage.removeItem('{key}');");
                        logInfo($"sessionStorage.removeItem('{key}')");
                        return null;

                    case "clear":
                        js.ExecuteScript("sessionStorage.clear();");
                        logInfo("sessionStorage.clear()");
                        return null;

                    default:
                        throw new ArgumentException($"Неизвестная операция: {operation}");
                }
            }
            catch (Exception ex)
            {
                logError($"Ошибка работы с sessionStorage: {operation}", ex);
                throw;
            }
        }

        internal static void ClearAllCookiesAndStorage(
            IWebDriver driver,
            Action<string> logInfo,
            Action<string, Exception> logError)
        {
            try
            {
                driver.Manage().Cookies.DeleteAllCookies();
                logInfo("Все cookies удалены");

                IJavaScriptExecutor js = driver as IJavaScriptExecutor;
                if (js != null)
                {
                    js.ExecuteScript("localStorage.clear(); sessionStorage.clear();");
                    logInfo("localStorage и sessionStorage очищены");
                }
            }
            catch (Exception ex)
            {
                logError("Ошибка очистки cookies и storage", ex);
                throw;
            }
        }
    }
}
