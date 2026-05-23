// =============================================================================
// HttpOAuth2TokenBack.cs — активность «HTTP: OAuth2 токен».
//
// Получает OAuth2 токен доступа по различным Grant Type:
// - ClientCredentials — авторизация от имени приложения
// - Password — авторизация по паролю пользователя
// - AuthorizationCode — авторизация через код авторизации
//
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для получения OAuth2 токена доступа.
    /// </summary>
    public class HttpOAuth2TokenBack : HttpActivityBase<HttpOAuth2Token>
    {
        // ── Входные параметры: Основные ───────────────────────────────────

        #region Prop_TokenUrl

        private string _propTokenUrl;

        /// <summary>
        /// URL endpoint для получения токена.
        /// Пример: "https://oauth.example.com/token"
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

        #region Prop_GrantType

        private OAuth2GrantType _propGrantType;

        /// <summary>
        /// Тип OAuth2 Grant Type.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Grant Type")]
        public OAuth2GrantType Prop_GrantType
        {
            get => _propGrantType;
            set { _propGrantType = value; InvokePropertyChanged(this, nameof(Prop_GrantType)); }
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

        #region Prop_Scope

        private string _propScope;

        /// <summary>
        /// Область доступа (Scope).
        /// Опционально, зависит от провайдера.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Scope")]
        public string Prop_Scope
        {
            get => _propScope;
            set { _propScope = value; InvokePropertyChanged(this, nameof(Prop_Scope)); }
        }

        #endregion

        // ── Входные параметры: Password Grant ──────────────────────────────

        #region Prop_Username

        private string _propUsername;

        /// <summary>
        /// Имя пользователя (для Password Grant).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Password Grant"),
         System.ComponentModel.DisplayName("Username")]
        public string Prop_Username
        {
            get => _propUsername;
            set { _propUsername = value; InvokePropertyChanged(this, nameof(Prop_Username)); }
        }

        #endregion

        #region Prop_Password

        private string _propPassword;

        /// <summary>
        /// Пароль пользователя (для Password Grant).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Password Grant"),
         System.ComponentModel.DisplayName("Password")]
        public string Prop_Password
        {
            get => _propPassword;
            set { _propPassword = value; InvokePropertyChanged(this, nameof(Prop_Password)); }
        }

        #endregion

        // ── Входные параметры: Authorization Code Grant ────────────────────

        #region Prop_AuthorizationCode

        private string _propAuthorizationCode;

        /// <summary>
        /// Код авторизации (для Authorization Code Grant).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Authorization Code"),
         System.ComponentModel.DisplayName("Code")]
        public string Prop_AuthorizationCode
        {
            get => _propAuthorizationCode;
            set { _propAuthorizationCode = value; InvokePropertyChanged(this, nameof(Prop_AuthorizationCode)); }
        }

        #endregion

        #region Prop_RedirectUri

        private string _propRedirectUri;

        /// <summary>
        /// URI перенаправления (для Authorization Code Grant).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Authorization Code"),
         System.ComponentModel.DisplayName("Redirect URI")]
        public string Prop_RedirectUri
        {
            get => _propRedirectUri;
            set { _propRedirectUri = value; InvokePropertyChanged(this, nameof(Prop_RedirectUri)); }
        }

        #endregion

        // ── Выходные параметры ────────────────────────────────────────────

        #region Prop_AccessToken

        private string _propAccessToken;

        /// <summary>
        /// Токен доступа (Access Token).
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

        #region Prop_RefreshToken

        private string _propRefreshToken;

        /// <summary>
        /// Токен обновления (Refresh Token).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Refresh Token")]
        public string Prop_RefreshToken
        {
            get => _propRefreshToken;
            set { _propRefreshToken = value; InvokePropertyChanged(this, nameof(Prop_RefreshToken)); }
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

        #region Prop_TokenType

        private string _propTokenType;

        /// <summary>
        /// Тип токена (обычно "Bearer").
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Token Type")]
        public string Prop_TokenType
        {
            get => _propTokenType;
            set { _propTokenType = value; InvokePropertyChanged(this, nameof(Prop_TokenType)); }
        }

        #endregion

        // ── Конструктор ───────────────────────────────────────────────────

        /// <summary>
        /// Конструктор активности «HTTP: OAuth2 токен».
        /// </summary>
        public HttpOAuth2TokenBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "HTTP: OAuth2 токен";
            sdkComponentHelp =
                "Получает OAuth2 токен доступа.\n" +
                "\n" +
                "── Grant Types ────────────────────────────────\n" +
                "ClientCredentials  — авторизация приложения\n" +
                "Password           — авторизация по паролю\n" +
                "AuthorizationCode  — авторизация по коду\n" +
                "\n" +
                "── Основные параметры ─────────────────────────\n" +
                "Token URL     — endpoint для получения токена\n" +
                "Client ID     — идентификатор клиента\n" +
                "Client Secret — секрет клиента\n" +
                "Scope         — область доступа (опционально)\n" +
                "\n" +
                "── Password Grant ─────────────────────────────\n" +
                "Username — имя пользователя\n" +
                "Password — пароль\n" +
                "\n" +
                "── Authorization Code Grant ───────────────────\n" +
                "Code         — код авторизации\n" +
                "Redirect URI — URI перенаправления\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Access Token  — токен доступа\n" +
                "Refresh Token — токен обновления\n" +
                "Expires In    — время жизни (сек)\n" +
                "Token Type    — тип токена";

            sdkComponentIcon = ActivityIcons.Http;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<string>("Prop_TokenUrl", "URL endpoint для токена"),
                PropertyBuilder.Enum<OAuth2GrantType>("Prop_GrantType", "Тип Grant"),
                PropertyBuilder.Script<string>("Prop_ClientId", "Client ID"),
                PropertyBuilder.Script<string>("Prop_ClientSecret", "Client Secret"),
                PropertyBuilder.Script<string>("Prop_Scope", "Scope (опционально)"),
                PropertyBuilder.Script<string>("Prop_Username", "Username (для Password Grant)"),
                PropertyBuilder.Script<string>("Prop_Password", "Password (для Password Grant)"),
                PropertyBuilder.Script<string>("Prop_AuthorizationCode", "Code (для Authorization Code)"),
                PropertyBuilder.Script<string>("Prop_RedirectUri", "Redirect URI"),
                PropertyBuilder.BooleanObject("Prop_IgnoreSslErrors", "Игнорировать ошибки SSL"),
                PropertyBuilder.Variable<string>("Prop_AccessToken", "Access Token"),
                PropertyBuilder.Variable<string>("Prop_RefreshToken", "Refresh Token"),
                PropertyBuilder.Variable<int>("Prop_ExpiresIn", "Время жизни (сек)"),
                PropertyBuilder.Variable<string>("Prop_TokenType", "Тип токена")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_TokenUrl = "\"https://oauth.example.com/token\"";
            this.Prop_GrantType = OAuth2GrantType.ClientCredentials;
            this.Prop_ClientId = "\"client_id\"";
            this.Prop_ClientSecret = "\"client_secret\"";
            this.Prop_Scope = "\"\"";
            this.Prop_Username = "\"\"";
            this.Prop_Password = "\"\"";
            this.Prop_AuthorizationCode = "\"\"";
            this.Prop_RedirectUri = "\"\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────────

        /// <summary>
        /// Основной метод получения OAuth2 токена.
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
                string scope = GetPropertyValue<string>(this.Prop_Scope, nameof(Prop_Scope), sd);

                // ── Создание HttpClient ───────────────────────────────────
                using (var client = CreateHttpClient(sd, 60))
                {
                    // ── Формирование параметров запроса ────────────────────
                    var parameters = BuildTokenRequestParameters(sd);

                    // ── Выполнение запроса ─────────────────────────────────
                    var content = new FormUrlEncodedContent(parameters);
                    var response = client.PostAsync(tokenUrl, content).ConfigureAwait(false).GetAwaiter().GetResult();

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                        return new ExecutionResult
                        {
                            IsSuccess = false,
                            ErrorMessage = $"Ошибка получения токена: HTTP {(int)response.StatusCode} — {response.ReasonPhrase}. {errorContent}"
                        };
                    }

                    // ── Парсинг ответа ────────────────────────────────────
                    string responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    var tokenResponse = ParseTokenResponse(responseContent);

                    // ── Запись выходных параметров ────────────────────────
                    if (!string.IsNullOrWhiteSpace(this.Prop_AccessToken))
                        SetVariableValue(this.Prop_AccessToken, tokenResponse.AccessToken, sd);

                    if (!string.IsNullOrWhiteSpace(this.Prop_RefreshToken))
                        SetVariableValue(this.Prop_RefreshToken, tokenResponse.RefreshToken ?? string.Empty, sd);

                    if (!string.IsNullOrWhiteSpace(this.Prop_ExpiresIn))
                        SetVariableValue(this.Prop_ExpiresIn, tokenResponse.ExpiresIn, sd);

                    if (!string.IsNullOrWhiteSpace(this.Prop_TokenType))
                        SetVariableValue(this.Prop_TokenType, tokenResponse.TokenType ?? "Bearer", sd);

                    return new ExecutionResult
                    {
                        IsSuccess = true,
                        SuccessMessage = $"[HTTP: OAuth2 токен] Получен токен ({this.Prop_GrantType}), expires_in: {tokenResponse.ExpiresIn} сек"
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
                    ErrorMessage = $"Ошибка [HTTP: OAuth2 токен]: {ex.Message}"
                };
            }
        }

        // ── Приватные методы ──────────────────────────────────────────────

        /// <summary>
        /// Формирует параметры запроса токена в зависимости от Grant Type.
        /// </summary>
        private Dictionary<string, string> BuildTokenRequestParameters(ScriptingData sd)
        {
            var parameters = new Dictionary<string, string>
            {
                { "grant_type", GetGrantTypeString() }
            };

            string clientId = GetPropertyValue<string>(this.Prop_ClientId, nameof(Prop_ClientId), sd);
            string clientSecret = GetPropertyValue<string>(this.Prop_ClientSecret, nameof(Prop_ClientSecret), sd);
            string scope = GetPropertyValue<string>(this.Prop_Scope, nameof(Prop_Scope), sd);

            // Client credentials (Basic Auth или в теле)
            if (!string.IsNullOrWhiteSpace(clientId))
                parameters["client_id"] = clientId;
            if (!string.IsNullOrWhiteSpace(clientSecret))
                parameters["client_secret"] = clientSecret;
            if (!string.IsNullOrWhiteSpace(scope))
                parameters["scope"] = scope;

            switch (this.Prop_GrantType)
            {
                case OAuth2GrantType.Password:
                    string username = GetPropertyValue<string>(this.Prop_Username, nameof(Prop_Username), sd);
                    string password = GetPropertyValue<string>(this.Prop_Password, nameof(Prop_Password), sd);
                    parameters["username"] = username ?? string.Empty;
                    parameters["password"] = password ?? string.Empty;
                    break;

                case OAuth2GrantType.AuthorizationCode:
                    string code = GetPropertyValue<string>(this.Prop_AuthorizationCode, nameof(Prop_AuthorizationCode), sd);
                    string redirectUri = GetPropertyValue<string>(this.Prop_RedirectUri, nameof(Prop_RedirectUri), sd);
                    parameters["code"] = code ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(redirectUri))
                        parameters["redirect_uri"] = redirectUri;
                    break;
            }

            return parameters;
        }

        /// <summary>
        /// Возвращает строковое представление Grant Type.
        /// </summary>
        private string GetGrantTypeString()
        {
            switch (this.Prop_GrantType)
            {
                case OAuth2GrantType.ClientCredentials:
                    return "client_credentials";
                case OAuth2GrantType.Password:
                    return "password";
                case OAuth2GrantType.AuthorizationCode:
                    return "authorization_code";
                default:
                    return "client_credentials";
            }
        }

        /// <summary>
        /// Парсит ответ OAuth2 сервера.
        /// </summary>
        /// <param name="json">JSON-ответ от OAuth2 сервера.</param>
        /// <returns>Распарсенный ответ с токенами.</returns>
        /// <exception cref="InvalidOperationException">При ошибке парсинга JSON.</exception>
        private OAuth2TokenResponse ParseTokenResponse(string json)
        {
            var response = new OAuth2TokenResponse();

            try
            {
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                response.AccessToken = obj["access_token"]?.ToString();
                response.RefreshToken = obj["refresh_token"]?.ToString();
                response.TokenType = obj["token_type"]?.ToString() ?? "Bearer";

                if (int.TryParse(obj["expires_in"]?.ToString(), out int expiresIn))
                    response.ExpiresIn = expiresIn;
            }
            catch (Newtonsoft.Json.JsonException jex)
            {
                throw new InvalidOperationException($"Ошибка парсинга ответа OAuth2: {jex.Message}. Ответ сервера: {json?.Substring(0, Math.Min(json?.Length ?? 0, 200))}", jex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Непредвиденная ошибка при парсинге ответа OAuth2: {ex.Message}", ex);
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
            public string TokenType { get; set; }
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

            if (string.IsNullOrWhiteSpace(this.Prop_ClientId))
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = "Prop_ClientId",
                    Error = "Client ID не может быть пустым"
                });
            }

            // Валидация параметров для Password Grant
            if (this.Prop_GrantType == OAuth2GrantType.Password)
            {
                if (string.IsNullOrWhiteSpace(this.Prop_Username))
                {
                    ret.Items.Add(new ValidationResult.ValidationItem
                    {
                        PropertyName = "Prop_Username",
                        Error = "Username обязателен для Password Grant"
                    });
                }

                if (string.IsNullOrWhiteSpace(this.Prop_Password))
                {
                    ret.Items.Add(new ValidationResult.ValidationItem
                    {
                        PropertyName = "Prop_Password",
                        Error = "Password обязателен для Password Grant"
                    });
                }
            }

            // Валидация параметров для Authorization Code Grant
            if (this.Prop_GrantType == OAuth2GrantType.AuthorizationCode)
            {
                if (string.IsNullOrWhiteSpace(this.Prop_AuthorizationCode))
                {
                    ret.Items.Add(new ValidationResult.ValidationItem
                    {
                        PropertyName = "Prop_AuthorizationCode",
                        Error = "Code обязателен для Authorization Code Grant"
                    });
                }
            }

            return ret;
        }
    }
}
