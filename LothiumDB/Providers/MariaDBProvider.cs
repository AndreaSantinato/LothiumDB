// System Class
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using MySql.Data.Common;

// Custom Class
using MySql.Data.MySqlClient;
using LothiumDB.Helpers;
using LothiumDB.Enumerations;
using LothiumDB.Interfaces;

namespace LothiumDB.Providers
{
    /// <summary>
    /// Defines A Provider For A MariaDB's Database Istance
    /// </summary>
    public class MariaDBProvider : IDbProvider
    {
        public ProviderTypes ProviderType() => ProviderTypes.MariaDB;

        public string VariablePrefix() => "@";

        public string GenerateConnectionString(params object[] args)
        {
            foreach (object arg in args)
            {
                Type objType = arg.GetType();
                if (arg == null || objType == typeof(bool)) break;
                if (DatabaseUtility.VerifyConnectionStringParameters(arg)) return string.Empty;
            }

            MySqlConnectionStringBuilder MariaDBConnStringBuilder = new MySqlConnectionStringBuilder()
            {
                DefaultCommandTimeout = 30,
                Server = (string)args[0],
                UserID = (string)args[1],
                Password = (string)args[2],
                Database = (string)args[3],
                SqlServerMode = (bool)args[4]
            };

            return MariaDBConnStringBuilder.ConnectionString;
        }

        public string BuildPageQuery(string query, long offset, long element) => DatabaseUtility.BuildPageQuery(query, offset, element);
    }
}
