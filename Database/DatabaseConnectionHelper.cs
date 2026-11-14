using Primo.MIA.Common;
using System;
using System.Data.Common;
using System.Security;

namespace Primo.MIA
{
    public static class DatabaseConnectionHelper
    {
        /// <summary>
        /// Плейсхолдер пароля в строке подключения (issue #26): строка хранится
        /// в workflow без пароля, пароль подставляется в рантайме из
        /// RepoDict.SecDict (элемент «БД: Задать пароль подключения»).
        /// </summary>
        public const string PasswordPlaceholder = "{password}";

        /// <summary>Ключ пароля подключения в RepoDict.SecDict.</summary>
        public const string PasswordKey = "DatabasePassword";

        /// <summary>
        /// Подставляет пароль вместо плейсхолдера {password} в строке подключения.
        /// Без плейсхолдера строка возвращается как есть (обратная совместимость);
        /// с плейсхолдером и без заданного пароля — DatabaseException.
        /// </summary>
        public static string ApplyPassword(string connectionString, SecureString password)
        {
            if (string.IsNullOrEmpty(connectionString))
                return connectionString;

            if (!connectionString.Contains(PasswordPlaceholder))
                return connectionString;

            if (password == null)
                throw new DatabaseException(
                    $"Строка подключения содержит плейсхолдер {PasswordPlaceholder}, " +
                    "но пароль не задан — добавьте активность «БД: Задать пароль подключения».");

            var plain = ToPlainString(password);
            return connectionString.Replace(PasswordPlaceholder, plain);
        }

        /// <summary>
        /// Резолвит строку подключения: подстановка пароля из RepoDict.SecDict
        /// при наличии плейсхолдера. Единая точка для всех DB-активностей
        /// (все проходят через OpenConnection).
        /// </summary>
        public static string ResolveConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)
                || !connectionString.Contains(PasswordPlaceholder))
                return connectionString;

            RepoDict.SecDict.TryGetValue(PasswordKey, out var password);
            return ApplyPassword(connectionString, password);
        }

        /// <summary>
        /// Декодирует SecureString в plain-строку (BSTR освобождается обнулением
        /// сразу после чтения — расшифровка живёт только на время сборки строки).
        /// </summary>
        public static string ToPlainString(SecureString secure)
        {
            if (secure == null)
                return null;
            if (secure.Length == 0)
                return string.Empty;

            var ptr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(secure);
            try
            {
                return System.Runtime.InteropServices.Marshal.PtrToStringBSTR(ptr);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(ptr);
            }
        }

        public static DbProviderFactory GetFactory(string providerInvariantName)
        {
            var provider = string.IsNullOrWhiteSpace(providerInvariantName)
                ? DatabaseHelper.DefaultProviderInvariantName
                : providerInvariantName.Trim();

            try
            {
                return DbProviderFactories.GetFactory(provider);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Не удалось получить DbProviderFactory для '{provider}'. " +
                    "Проверьте invariant name и установлен ли ADO.NET провайдер.",
                    ex);
            }
        }

        public static DbConnection OpenConnection(string providerInvariantName, string connectionString)
        {
            Guard.NotNullOrWhiteSpace(connectionString, nameof(connectionString));

            var factory = GetFactory(providerInvariantName);
            var connection = factory.CreateConnection();
            if (connection == null)
                throw new InvalidOperationException("Провайдер не смог создать объект подключения.");

            connection.ConnectionString = ResolveConnectionString(connectionString);
            connection.Open();
            return connection;
        }

        public static string GetProviderInvariantName(DbConnection connection)
        {
            return connection?.GetType().Namespace ?? DatabaseHelper.DefaultProviderInvariantName;
        }
    }
}
