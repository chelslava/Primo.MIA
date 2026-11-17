// =============================================================================
// SftpActivityBase.cs — базовый класс SFTP-активностей.
//
// Общие свойства подключения (хост, порт, учётные данные, приватный ключ,
// проверка host key, таймауты) и шаблон выполнения: подключение → операция →
// отключение с маппингом исключений SSH.NET в понятные сообщения.
//
// Проверка host key:
//   - задан Prop_ExpectedFingerprint (SHA256 Base64, формат ssh-keygen -lf,
//     например "SHA256:abc...") → отпечаток сверяется, при несовпадении отказ;
//   - Prop_AllowAnyHostKey = True → проверка отключена (небезопасно, тестовые среды);
//   - иначе — поведение SSH.NET по умолчанию (отказ неизвестным ключам).
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using Renci.SshNet;
using System;

namespace Primo.MIA
{
    /// <summary>
    /// Базовый класс для SFTP-активностей: общие свойства подключения и
    /// шаблон «подключить → выполнить → отключить».
    /// </summary>
    public abstract class SftpActivityBase<TView> : PrimoComponentTO<TView>
        where TView : System.Windows.Controls.UserControl, new()
    {
        protected SftpActivityBase(IWFContainer container) : base(container)
        {
        }

        // ── Свойства подключения ─────────────────────────────────────────

        private string _propHost;
        /// <summary>Хост SFTP-сервера.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpHost)]
        public string Prop_Host
        {
            get => _propHost;
            set { _propHost = value; InvokePropertyChanged(this, nameof(Prop_Host)); }
        }

        private string _propPort = "22";
        /// <summary>Порт SFTP-сервера (22 по умолчанию).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpPort)]
        public string Prop_Port
        {
            get => _propPort;
            set { _propPort = value; InvokePropertyChanged(this, nameof(Prop_Port)); }
        }

        private string _propUserName;
        /// <summary>Имя пользователя.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpUserName)]
        public string Prop_UserName
        {
            get => _propUserName;
            set { _propUserName = value; InvokePropertyChanged(this, nameof(Prop_UserName)); }
        }

        private string _propPassword;
        /// <summary>Пароль (аутентификация по паролю).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpPassword)]
        public string Prop_Password
        {
            get => _propPassword;
            set { _propPassword = value; InvokePropertyChanged(this, nameof(Prop_Password)); }
        }

        private string _propPrivateKeyPath;
        /// <summary>Путь к файлу приватного ключа (OpenSSH/PEM; опционально).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpPrivateKeyPath)]
        public string Prop_PrivateKeyPath
        {
            get => _propPrivateKeyPath;
            set { _propPrivateKeyPath = value; InvokePropertyChanged(this, nameof(Prop_PrivateKeyPath)); }
        }

        private string _propKeyPassphrase;
        /// <summary>Парольная фраза приватного ключа (если установлена).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpKeyPassphrase)]
        public string Prop_KeyPassphrase
        {
            get => _propKeyPassphrase;
            set { _propKeyPassphrase = value; InvokePropertyChanged(this, nameof(Prop_KeyPassphrase)); }
        }

        private string _propConnectionTimeoutSeconds = "30";
        /// <summary>Таймаут подключения в секундах.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpConnectionTimeoutSeconds)]
        public string Prop_ConnectionTimeoutSeconds
        {
            get => _propConnectionTimeoutSeconds;
            set { _propConnectionTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_ConnectionTimeoutSeconds)); }
        }

        private string _propOperationTimeoutSeconds = "30";
        /// <summary>Таймаут операции (чтения/записи) в секундах.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpOperationTimeoutSeconds)]
        public string Prop_OperationTimeoutSeconds
        {
            get => _propOperationTimeoutSeconds;
            set { _propOperationTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_OperationTimeoutSeconds)); }
        }

        private string _propExpectedFingerprint;
        /// <summary>Ожидаемый отпечаток host key (SHA256 Base64, как в ssh-keygen -lf).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpExpectedFingerprint)]
        public string Prop_ExpectedFingerprint
        {
            get => _propExpectedFingerprint;
            set { _propExpectedFingerprint = value; InvokePropertyChanged(this, nameof(Prop_ExpectedFingerprint)); }
        }

        private bool _propAllowAnyHostKey;
        /// <summary>Отключить проверку host key (небезопасно — только тестовые среды).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpAllowAnyHostKey)]
        public bool Prop_AllowAnyHostKey
        {
            get => _propAllowAnyHostKey;
            set { _propAllowAnyHostKey = value; InvokePropertyChanged(this, nameof(Prop_AllowAnyHostKey)); }
        }

        private string _propRetryCount;
        /// <summary>Число повторов при транзиентном сетевом сбое (0 — без повторов).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_NetworkRetryCount)]
        public string Prop_RetryCount
        {
            get => _propRetryCount;
            set { _propRetryCount = value; InvokePropertyChanged(this, nameof(Prop_RetryCount)); }
        }

        private string _propRetryDelayMs;
        /// <summary>Пауза между повторами, мс.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_NetworkRetryDelayMs)]
        public string Prop_RetryDelayMs
        {
            get => _propRetryDelayMs;
            set { _propRetryDelayMs = value; InvokePropertyChanged(this, nameof(Prop_RetryDelayMs)); }
        }

        // ── Хелперы ──────────────────────────────────────────────────────

        /// <summary>
        /// Свойства подключения для панели Studio (общие для всех SFTP-активностей).
        /// </summary>
        protected static LTools.Common.Helpers.WFHelper.PropertiesItem[] ConnectionProperties()
        {
            return new[]
            {
                PropertyBuilder.String("Prop_Host", "Хост SFTP-сервера"),
                PropertyBuilder.Int("Prop_Port", "Порт (по умолчанию 22)"),
                PropertyBuilder.String("Prop_UserName", "Имя пользователя"),
                PropertyBuilder.String("Prop_Password", "Пароль"),
                PropertyBuilder.Int("Prop_RetryCount", "Повторов при сетевом сбое (0 — без повторов)"),
                PropertyBuilder.Int("Prop_RetryDelayMs", "Пауза между повторами, мс")
            };
        }

        // ── Выполнение ───────────────────────────────────────────────────

        /// <summary>
        /// Разбирает и резолвит свойства подключения. Валидация полей — в SftpLogic.CreateClient.
        /// </summary>
        protected SftpConnectionSettings ResolveSettings(ScriptingData sd)
        {
            return new SftpConnectionSettings
            {
                Host = GetPropertyValue<string>(this.Prop_Host, nameof(Prop_Host), sd),
                Port = ParseInt(GetPropertyValue<string>(this.Prop_Port, nameof(Prop_Port), sd), 22),
                UserName = GetPropertyValue<string>(this.Prop_UserName, nameof(Prop_UserName), sd),
                Password = GetPropertyValue<string>(this.Prop_Password, nameof(Prop_Password), sd),
                PrivateKeyPath = GetPropertyValue<string>(this.Prop_PrivateKeyPath, nameof(Prop_PrivateKeyPath), sd),
                KeyPassphrase = GetPropertyValue<string>(this.Prop_KeyPassphrase, nameof(Prop_KeyPassphrase), sd),
                ConnectionTimeoutSeconds = ParseInt(GetPropertyValue<string>(this.Prop_ConnectionTimeoutSeconds, nameof(Prop_ConnectionTimeoutSeconds), sd), 30),
                OperationTimeoutSeconds = ParseInt(GetPropertyValue<string>(this.Prop_OperationTimeoutSeconds, nameof(Prop_OperationTimeoutSeconds), sd), 30),
                ExpectedFingerprint = GetPropertyValue<string>(this.Prop_ExpectedFingerprint, nameof(Prop_ExpectedFingerprint), sd),
                AllowAnyHostKey = this.Prop_AllowAnyHostKey
            };
        }

        /// <summary>
        /// Шаблон «подключить → выполнить → отключить» с маппингом исключений SSH.NET.
        /// При Prop_RetryCount &gt; 0 транзиентные сетевые сбои (соединение, транспорт)
        /// повторяются с новым подключением; детерминированные ошибки
        /// (аутентификация, путь) не повторяются (issue #43).
        /// </summary>
        /// <param name="sd">Данные скрипта.</param>
        /// <param name="operation">Операция над подключённым SftpClient.</param>
        protected ExecutionResult ExecuteSftp(ScriptingData sd, Func<SftpClient, ExecutionResult> operation)
        {
            var settings = ResolveSettings(sd);

            int retryCount = NetworkRetry.ParseRetryCount(
                GetPropertyValue<string>(this.Prop_RetryCount, nameof(Prop_RetryCount), sd));
            int retryDelayMs = NetworkRetry.ParseRetryDelayMs(
                GetPropertyValue<string>(this.Prop_RetryDelayMs, nameof(Prop_RetryDelayMs), sd));

            try
            {
                var outcome = RetryPolicy.Execute(
                    attempt =>
                    {
                        // каждая попытка — новое подключение: клиент предыдущей
                        // мог остаться в разорванном состоянии
                        SftpClient client = null;
                        try
                        {
                            client = new SftpLogic().CreateClient(settings);
                            client.Connect();
                            if (!client.IsConnected)
                                throw new Renci.SshNet.Common.SshConnectionException(ActivityStrings.Error_SftpConnectFailed);

                            return operation(client);
                        }
                        finally
                        {
                            // порядок сохранён: Disconnect → Dispose; исключение
                            // освобождения уже разорванной сессии глушится
                            SafeDispose.Quiet(
                                () =>
                                {
                                    client?.Disconnect();
                                    client?.Dispose();
                                },
                                "SFTP disconnect/dispose");
                        }
                    },
                    retryCount + 1,
                    shouldRetryException: (ex, attemptNumber) => NetworkRetry.IsTransient(ex),
                    delayMs: retryDelayMs,
                    failureCallback: (ex, attemptNumber) => Common.ActivityFileLogger.Warning(
                        sdkComponentName, $"Попытка {attemptNumber} не удалась: {ex.Message}"));

                if (outcome.LastException != null)
                    throw outcome.LastException; // попытки исчерпаны — маппинг внешним catch
                return outcome.LastResult;
            }
            catch (Renci.SshNet.Common.SshAuthenticationException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка аутентификации: {ex.Message}" };
            }
            catch (Renci.SshNet.Common.SshConnectionException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка подключения: {ex.Message}" };
            }
            catch (Renci.SshNet.Common.SftpPathNotFoundException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Путь не найден: {ex.Message}" };
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка приватного ключа: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка SFTP: {ex.Message}" };
            }
        }

        private static int ParseInt(string value, int fallback)
        {
            return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;
        }
    }
}
