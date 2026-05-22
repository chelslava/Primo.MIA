// =============================================================================
// HttpActivityBase.cs — базовый класс для HTTP-активностей.
//
// Предоставляет общую функциональность для всех HTTP-активностей:
// - Создание и настройка HttpClient
// - Обработка сертификатов и SSL
// - Парсинг заголовков
// - Обработка ошибок
//
// Наследники: HttpBack, HttpDownloadBack, HttpUploadBack, HttpRetryBack и др.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Primo.MIA
{
    /// <summary>
    /// Базовый класс для всех HTTP-активностей.
    /// Предоставляет общую инфраструктуру для работы с HTTP-запросами.
    /// </summary>
    /// <typeparam name="TView">Тип View-класса (XAML UserControl).</typeparam>
    public abstract class HttpActivityBase<TView> : PrimoComponentTO<TView> where TView : System.Windows.Controls.UserControl, new()
    {
        // ── Константы ─────────────────────────────────────────────────────

        /// <summary>Таймаут по умолчанию в секундах.</summary>
        protected const int DefaultTimeoutSeconds = 100;

        /// <summary>Размер буфера по умолчанию для скачивания файлов.</summary>
        protected const int DefaultBufferSize = 8192;

        // ── Свойства SDK ──────────────────────────────────────────────────

        /// <summary>
        /// Категория активности в дизайнере Primo.
        /// </summary>
        public override string GroupName
        {
            get => ActivityCategories.HttpWeb;
            protected set { }
        }

        /// <summary>
        /// Максимальное время выполнения активности (мс).
        /// Переопределяется в наследниках при необходимости.
        /// </summary>
        protected override int sdkTimeOut
        {
            get => DefaultTimeoutSeconds * 1000;
            set { }
        }

        // ── Общие свойства: SSL и сертификаты ─────────────────────────────

        #region Prop_IgnoreSslErrors

        private bool _propIgnoreSslErrors;

        /// <summary>
        /// Игнорировать ошибки проверки SSL-сертификата сервера.
        /// ВНИМАНИЕ: Используйте только в тестовых средах!
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Sertificates_SSL),
         System.ComponentModel.DisplayName("Игнорировать ошибки SSL")]
        public bool Prop_IgnoreSslErrors
        {
            get => _propIgnoreSslErrors;
            set { _propIgnoreSslErrors = value; InvokePropertyChanged(this, nameof(Prop_IgnoreSslErrors)); }
        }

        #endregion

        #region Prop_UseCertificate

        private bool _propUseCertificate;

        /// <summary>
        /// Использовать клиентский сертификат для аутентификации.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Sertificates_SSL),
         System.ComponentModel.DisplayName("Использовать сертификат")]
        public bool Prop_UseCertificate
        {
            get => _propUseCertificate;
            set { _propUseCertificate = value; InvokePropertyChanged(this, nameof(Prop_UseCertificate)); }
        }

        #endregion

        #region Prop_CertPath

        private string _propCertPath;

        /// <summary>
        /// Путь к файлу клиентского сертификата (.pfx).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Sertificates_SSL),
         System.ComponentModel.DisplayName("Путь к сертификату")]
        public string Prop_CertPath
        {
            get => _propCertPath;
            set { _propCertPath = value; InvokePropertyChanged(this, nameof(Prop_CertPath)); }
        }

        #endregion

        #region Prop_CertPassword

        private string _propCertPassword;

        /// <summary>
        /// Пароль к файлу сертификата.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Sertificates_SSL),
         System.ComponentModel.DisplayName("Пароль сертификата")]
        public string Prop_CertPassword
        {
            get => _propCertPassword;
            set { _propCertPassword = value; InvokePropertyChanged(this, nameof(Prop_CertPassword)); }
        }

        #endregion

        // ── Конструктор ───────────────────────────────────────────────────

        /// <summary>
        /// Конструктор базового класса HTTP-активности.
        /// </summary>
        /// <param name="container">Контейнер workflow.</param>
        protected HttpActivityBase(IWFContainer container) : base(container)
        {
            // Значения по умолчанию для сертификатов
            this.Prop_IgnoreSslErrors = false;
            this.Prop_UseCertificate = false;
            this.Prop_CertPath = "\"\"";
            this.Prop_CertPassword = "\"\"";
        }

        // ── Методы создания HttpClient ────────────────────────────────────

        /// <summary>
        /// Создаёт настроенный HttpClient с поддержкой сертификатов и SSL.
        /// </summary>
        /// <param name="sd">Данные скрипта для получения значений свойств.</param>
        /// <param name="timeoutSeconds">Таймаут запроса в секундах.</param>
        /// <returns>Настроенный HttpClient.</returns>
        protected HttpClient CreateHttpClient(ScriptingData sd, int timeoutSeconds)
        {
            var handler = new HttpClientHandler();

            // Настройка клиентского сертификата
            if (this.Prop_UseCertificate)
            {
                string certPath = GetPropertyValue<string>(this.Prop_CertPath, nameof(Prop_CertPath), sd);
                string certPassword = GetPropertyValue<string>(this.Prop_CertPassword, nameof(Prop_CertPassword), sd) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(certPath))
                    throw new ArgumentException("Путь к сертификату не указан");

                if (!File.Exists(certPath))
                    throw new FileNotFoundException($"Файл сертификата не найден: {certPath}");

                var certificate = new X509Certificate2(certPath, certPassword);
                handler.ClientCertificates.Add(certificate);
            }

            // Игнорирование SSL-ошибок (только для тестовых сред)
            if (this.Prop_IgnoreSslErrors)
            {
                handler.ServerCertificateCustomValidationCallback =
                    (sender, cert, chain, sslPolicyErrors) => true;
            }

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };

            return client;
        }

        /// <summary>
        /// Создаёт HttpClient с кастомным handler.
        /// Используется для специфичных сценариев (прокси, custom validation).
        /// </summary>
        /// <param name="handler">Кастомный HttpClientHandler.</param>
        /// <param name="timeoutSeconds">Таймаут запроса в секундах.</param>
        /// <returns>Настроенный HttpClient.</returns>
        protected HttpClient CreateHttpClient(HttpClientHandler handler, int timeoutSeconds)
        {
            // Применяем SSL-настройки из свойств
            if (this.Prop_IgnoreSslErrors)
            {
                handler.ServerCertificateCustomValidationCallback =
                    (sender, cert, chain, sslPolicyErrors) => true;
            }

            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };
        }

        // ── Вспомогательные методы ────────────────────────────────────────

        /// <summary>
        /// Создаёт успешный результат выполнения.
        /// </summary>
        /// <param name="message">Сообщение об успехе.</param>
        /// <returns>ExecutionResult с IsSuccess = true.</returns>
        protected ExecutionResult CreateSuccessResult(string message)
        {
            return new ExecutionResult
            {
                IsSuccess = true,
                SuccessMessage = message
            };
        }

        /// <summary>
        /// Создаёт результат с ошибкой.
        /// </summary>
        /// <param name="error">Сообщение об ошибке.</param>
        /// <returns>ExecutionResult с IsSuccess = false.</returns>
        protected ExecutionResult CreateErrorResult(string error)
        {
            return new ExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = error
            };
        }

        /// <summary>
        /// Формирует метку активности для логирования.
        /// </summary>
        /// <param name="activityName">Имя активности.</param>
        /// <returns>Форматированная метка.</returns>
        protected string ActivityLabel(string activityName)
        {
            return $"[{activityName}]";
        }
    }
}
