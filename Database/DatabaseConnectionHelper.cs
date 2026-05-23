using Primo.MIA.Common;
using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace Primo.MIA
{
    public static class DatabaseConnectionHelper
    {
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

            connection.ConnectionString = connectionString;
            connection.Open();
            return connection;
        }

        public static string GetProviderInvariantName(DbConnection connection)
        {
            if (connection is SqlConnection)
                return DatabaseHelper.DefaultProviderInvariantName;

            return connection != null ? connection.GetType().Namespace : DatabaseHelper.DefaultProviderInvariantName;
        }
    }
}
