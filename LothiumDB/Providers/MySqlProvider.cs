// System Class
using System;
using System.Data;
// Custom Class
using LothiumDB.Helpers;
using LothiumDB.Enumerations;
using LothiumDB.Interfaces;
using LothiumDB.Extensions;
// NuGet Packages
using MySql.Data.Common;
using MySql.Data.MySqlClient;

namespace LothiumDB.Providers
{
    /// <summary>
    /// Defines A Provider For A MySQL's Database Istance
    /// </summary>
    public class MySqlProvider : IDbProvider
    {
        public ProviderTypes ProviderType() => ProviderTypes.MySql;

        public string VariablePrefix() => "@";

        public string CreateConnectionString(params object[] args)
        {
            foreach (object arg in args)
            {
                Type objType = arg.GetType();
                if (arg == null || objType == typeof(bool)) break;
                if (DatabaseUtility.VerifyConnectionStringParameters(arg)) return string.Empty;
            }

            MySqlConnectionStringBuilder MySqlConnStringBuilder = new MySqlConnectionStringBuilder()
            {
                DefaultCommandTimeout = 30,
                Server = (string)args[0],
                UserID = (string)args[1],
                Password = (string)args[2],
                Database = (string)args[3],
                SqlServerMode = (bool)args[4]
            };

            return MySqlConnStringBuilder.ConnectionString;
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
