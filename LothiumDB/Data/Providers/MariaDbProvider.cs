// System Class
using System.Data;
// Custom Class
using LothiumDB.Enumerations;
using LothiumDB.Extensions;
using LothiumDB.Interfaces;
// NuGet Packages
using MySql.Data.MySqlClient;

namespace LothiumDB.Data.Providers;

/// <summary>
/// Defines A Provider For A MariaDB's Database Instance
/// </summary>
public sealed class MariaDbProvider : IDatabaseProvider
{
    public ProviderTypesEnum ProviderType() => ProviderTypesEnum.MariaDb;

    public string VariablePrefix() => "@";

    public string CreateConnectionString(params object[] args)
    {
        if (!args.Any()) return string.Empty;
        return new MySqlConnectionStringBuilder()
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
        ArgumentNullException.ThrowIfNull(connectionString);
        return new MySqlConnection(connectionString);
    }

    public SqlBuilder BuildPageQuery<T>(PageObject<T> pageObj, SqlBuilder sql)
    {
        sql.Append($"LIMIT {pageObj.ItemsForEachPage}").Append($"OFFSET {pageObj.ItemsToBeSkipped}");
        return sql;
    }

    public SqlBuilder CreateAuditTable()
    {
        return new SqlBuilder(@"
            /* Create the table */

            CREATE TABLE AuditEvents
            (
	            AuditID int NOT NULL,
                AuditLevel nvarchar(32) NOT NULL,
	            AuditUser nvarchar(64) NOT NULL,
	            ExecutedOn date NOT NULL,
	            DbCommandType nvarchar(32) NOT NULL,
	            SqlCommandType nvarchar(32) NOT NULL,
	            SqlCommandOnly nvarchar(255),
	            SqlCommandComplete nvarchar(255),
                ErrorMessage nvarchar(255)
            );

            /* Add the primary key */
            ALTER TABLE AuditEvents ADD PRIMARY KEY(AuditID);

            /* Set the primary key auto-increment */
            ALTER TABLE AuditEvents MODIFY COLUMN AuditID INT AUTO_INCREMENT;
        ");
    }
}