// System Class
using System;
using System.Data;
// Custom Class
using LothiumDB.Core.Helpers;
using LothiumDB.Enumerations;
// NuGet Packages
using MySql.Data.MySqlClient;

namespace LothiumDB.Providers
{
    /// <summary>
    /// Defines A Provider For A MariaDB's Database Instance
    /// </summary>
    public sealed class MariaDbProvider : IDatabaseProvider
    {
        public ProviderTypesEnum ProviderType() => ProviderTypesEnum.MariaDB;

        public string VariablePrefix() => "@";

        public string CreateConnectionString(params object[] args)
        {
            if (!args.Any()) return string.Empty;
            return  new MySqlConnectionStringBuilder()
            {
                DefaultCommandTimeout = 30,
                Server = (string)args[0],
                UserID = (string)args[1],
                Password = (string)args[2],
                Database = (string)args[3],
                SqlServerMode = (bool)args[4]
            }.ConnectionString;
        }

        public IDbConnection CreateConnection(string connectionString)
        {
            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
            return new MySqlConnection(connectionString);
        }

        public SqlBuilder BuildPageQuery<T>(PageObject<T> pageObj, SqlBuilder sql)
        {
            sql.Append($"LIMIT {pageObj.ItemsForEachPage}").Append($"OFFSET {pageObj.ItemsToBeSkipped}");
            return sql;
        }
    }
}
