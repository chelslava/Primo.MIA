// =============================================================================
// HttpOAuth2RefreshBack.cs — активность «HTTP: OAuth2 обновление токена».
//
// Обновляет OAuth2 токен доступа с использованием refresh token.
//
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для обновления OAuth2 токена.
    /// </summary>
    public class HttpOAuth2RefreshBack : HttpActivityBase<HttpOAuth2Refresh>
    {
        // ── Входные параметры ─────────────────────────────────────────────

        #region Prop_TokenUrl

        private string _propTokenUrl;

        /// <summary>
        /// URL endpoint для обновления токена.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Token URL")]
        public string Prop_TokenUrl
        {
            get => _propTokenUrl;
            set { _propTokenUrl = value; InvokePropertyChanged(this, nameof(Prop_TokenUrl)); }
        }

        #endregion

        #region Prop_ClientId

        private string _propClientId;

        /// <summary>
        /// Идентификатор клиента (Client ID).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Client ID")]
        public string Prop_ClientId
        {
            get => _propClientId;
            set { _propClientId = value; InvokePropertyChanged(this, nameof(Prop_ClientId)); }
        }

        #endregion

        #region Prop_ClientSecret

        private string _propClientSecret;

        /// <summary>
        /// Секрет клиента (Client Secret).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Client Secret")]
        public string Prop_ClientSecret
        {
            get => _propClientSecret;
            set { _propClientSecret = value; InvokePropertyChanged(this, nameof(Prop_ClientSecret)); }
        }

        #endregion

        #region Prop_RefreshToken

        private string _propRefreshToken;

        /// <summary>
        /// Токен обновления (Refresh Token).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Refresh Token")]
        public string Prop_RefreshToken
        {
            get => _propRefreshToken;
            set { _propRefreshToken = value; InvokePropertyChanged(this, nameof(Prop_RefreshToken)); }
        }

        #endregion

        // ── Выходные параметры ────────────────────────────────────────────

        #region Prop_AccessToken

        private string _propAccessToken;

        /// <summary>
        /// Новый токен доступа (Access Token).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Access Token")]
        public string Prop_AccessToken
        {
            get => _propAccessToken;
            set { _propAccessToken = value; InvokePropertyChanged(this, nameof(Prop_AccessToken)); }
        }

        #endregion

        #region Prop_NewRefreshToken

        private string _propNewRefreshToken;

        /// <summary>
        /// Новый токен обновления (Refresh Token).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("New Refresh Token")]
        public string Prop_NewRefreshToken
        {
            get => _propNewRefreshToken;
            set { _propNewRefreshToken = value; InvokePropertyChanged(this, nameof(Prop_NewRefreshToken)); }
        }

        #endregion

        #region Prop_ExpiresIn

        private string _propExpiresIn;

        /// <summary>
        /// Время жизни токена в секундах.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Expires In")]
        public string Prop_ExpiresIn
        {
            get => _propExpiresIn;
            set { _propExpiresIn = value; InvokePropertyChanged(this, nameof(Prop_ExpiresIn)); }
        }

        #endregion

        // ── Конструктор ───────────────────────────────────────────────────

        /// <summary>
        /// Конструктор активности «HTTP: OAuth2 обновление токена».
        /// </summary>
        public HttpOAuth2RefreshBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "HTTP: OAuth2 обновление токена";
            sdkComponentHelp =
                "Обновляет OAuth2 токен доступа с использованием refresh token.\n" +
                "\n" +
                "── Входные параметры ──────────────────────────\n" +
                "Token URL     — endpoint для обновления токена\n" +
                "Client ID     — идентификатор клиента\n" +
                "Client Secret — секрет клиента\n" +
                "Refresh Token — токен обновления\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Access Token      — новый токен доступа\n" +
                "New Refresh Token — новый токен обновления\n" +
                "Expires In        — время жизни (сек)";

            sdkComponentIcon = ActivityIcons.Http;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<string>("Prop_TokenUrl", "URL endpoint"),
                PropertyBuilder.Script<string>("Prop_ClientId", "Client ID"),
                PropertyBuilder.Script<string>("Prop_ClientSecret", "Client Secret"),
                PropertyBuilder.Script<string>("Prop_RefreshToken", "Refresh Token"),
                PropertyBuilder.BooleanObject("Prop_IgnoreSslErrors", "Игнорировать ошибки SSL"),
                PropertyBuilder.Variable<string>("Prop_AccessToken", "Новый Access Token"),
                PropertyBuilder.Variable<string>("Prop_NewRefreshToken", "Новый Refresh Token"),
                PropertyBuilder.Variable<int>("Prop_ExpiresIn", "Время жизни (сек)")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_TokenUrl = "\"https://oauth.example.com/token\"";
            this.Prop_ClientId = "\"client_id\"";
            this.Prop_ClientSecret = "\"client_secret\"";
            this.Prop_RefreshToken = "\"refresh_token\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────────

        /// <summary>
        /// Основной метод обновления токена.
        /// </summary>
        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // ── Чтение входных параметров ──────────────────────────────
                string tokenUrl = GetPropertyValue<string>(this.Prop_TokenUrl, nameof(Prop_TokenUrl), sd);
                Guard.NotNullOrWhiteSpace(tokenUrl, nameof(Prop_TokenUrl));

                string clientId = GetPropertyValue<string>(this.Prop_ClientId, nameof(Prop_ClientId), sd);
                string clientSecret = GetPropertyValue<string>(this.Prop_ClientSecret, nameof(Prop_ClientSecret), sd);
                string refreshToken = GetPropertyValue<string>(this.Prop_RefreshToken, nameof(Prop_RefreshToken), sd);
                Guard.NotNullOrWhiteSpace(refreshToken, nameof(Prop_RefreshToken));

                // ── Создание HttpClient ───────────────────────────────────
                using (var client = CreateHttpClient(sd, 60))
                {
                    // ── Формирование параметров запроса ────────────────────
                    var parameters = new Dictionary<string, string>
                    {
                        { "grant_type", "refresh_token" },
                        { "refresh_token", refreshToken }
                    };

                    if (!string.IsNullOrWhiteSpace(clientId))
                        parameters["client_id"] = clientId;
                    if (!string.IsNullOrWhiteSpace(clientSecret))
                        parameters["client_secret"] = clientSecret;

                    // ── Выполнение запроса ─────────────────────────────────
                    var content = new FormUrlEncodedContent(parameters);
                    var response = client.PostAsync(tokenUrl, content).Result;

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorContent = response.Content.ReadAsStringAsync().Result;
                        return new ExecutionResult
                        {
                            IsSuccess = false,
                            ErrorMessage = $"Ошибка обновления токена: HTTP {(int)response.StatusCode} — {response.ReasonPhrase}. {errorContent}"
                        };
                    }

                    // ── Парсинг ответа ────────────────────────────────────
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var tokenResponse = ParseTokenResponse(responseContent);

                    // ── Запись выходных параметров ────────────────────────
                    if (!string.IsNullOrWhiteSpace(this.Prop_AccessToken))
                        SetVariableValue(this.Prop_AccessToken, tokenResponse.AccessToken, sd);

                    if (!string.IsNullOrWhiteSpace(this.Prop_NewRefreshToken))
                        SetVariableValue(this.Prop_NewRefreshToken, tokenResponse.RefreshToken ?? refreshToken, sd);

                    if (!string.IsNullOrWhiteSpace(this.Prop_ExpiresIn))
                        SetVariableValue(this.Prop_ExpiresIn, tokenResponse.ExpiresIn, sd);

                    return new ExecutionResult
                    {
                        IsSuccess = true,
                        SuccessMessage = $"[HTTP: OAuth2 обновление] Токен обновлён, expires_in: {tokenResponse.ExpiresIn} сек"
                    };
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка HTTP: {ex.Message}"
                };
            }
            catch (TimeoutException ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Превышено время ожидания: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка [HTTP: OAuth2 обновление]: {ex.Message}"
                };
            }
        }

        // ── Приватные методы ──────────────────────────────────────────────

        /// <summary>
        /// Парсит ответ OAuth2 сервера.
        /// </summary>
        private OAuth2TokenResponse ParseTokenResponse(string json)
        {
            var response = new OAuth2TokenResponse();

            try
            {
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                response.AccessToken = obj["access_token"]?.ToString();
                response.RefreshToken = obj["refresh_token"]?.ToString();

                if (int.TryParse(obj["expires_in"]?.ToString(), out int expiresIn))
                    response.ExpiresIn = expiresIn;
            }
            catch
            {
                // Если парсинг не удался
            }

            return response;
        }

        /// <summary>
        /// Модель ответа OAuth2.
        /// </summary>
        private class OAuth2TokenResponse
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
            public int ExpiresIn { get; set; }
        }

        // ── Валидация ─────────────────────────────────────────────────────

        /// <summary>
        /// Валидация параметров активности.
        /// </summary>
        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            if (string.IsNullOrWhiteSpace(this.Prop_TokenUrl))
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = "Prop_TokenUrl",
                    Error = "Token URL не может быть пустым"
                });
            }

            if (string.IsNullOrWhiteSpace(this.Prop_RefreshToken))
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = "Prop_RefreshToken",
                    Error = "Refresh Token не может быть пустым"
                });
            }

            return ret;
        }
    }
}
