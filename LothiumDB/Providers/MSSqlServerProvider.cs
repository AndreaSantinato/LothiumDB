// System Class
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Collections;
using System.Xml.Linq;
// Custom Class
using Microsoft.Data.SqlClient;
using LothiumDB.Helpers;
using LothiumDB.Enumerations;
using LothiumDB.Interfaces;
using LothiumDB.Extensions;

namespace LothiumDB.Providers
{
    /// <summary>
    /// Defines A Provider For A Microsoft SQL Server's Database Istance
    /// </summary>
    public sealed class MSSqlServerProvider : IDbProvider
    {
        public ProviderTypes ProviderType() => ProviderTypes.MSSql;

        public string VariablePrefix() => "@";

        public string GenerateConnectionString(params object[] args)
        {
            foreach (object arg in args)
            {
                Type objType = arg.GetType();
                if (arg == null || objType == typeof(bool)) break;
                if (DatabaseUtility.VerifyConnectionStringParameters(arg)) return string.Empty;
            }

            SqlConnectionStringBuilder MSSqlConnStringBuilder = new SqlConnectionStringBuilder()
            {
                ConnectRetryCount = 2,
                CommandTimeout = 30,
                Authentication = SqlAuthenticationMethod.SqlPassword,
                DataSource = (string)args[0],
                UserID = (string)args[1],
                Password = (string)args[2],
                InitialCatalog = (string)args[3],
                CurrentLanguage = (string)args[4],
                Encrypt = (bool)args[5],
                TrustServerCertificate = (bool)args[6]
            };

            return MSSqlConnStringBuilder.ConnectionString;
        }

        public SqlBuilder BuildPageQuery<T>(PageObject<T> pageObj, SqlBuilder sql) 
        {
            sql.Append($"OFFSET {pageObj.ItemsForEachPage} ROWS").Append($"FETCH NEXT {pageObj.ItemsToBeSkipped} ROWS ONLY");
            return sql;
        }
    }
}
