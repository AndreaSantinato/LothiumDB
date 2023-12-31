// System Class
using System.Data;
using System.Data.SqlClient;
// Custom Class
using LothiumDB.Enumerations;
using LothiumDB.Extensions;
using LothiumDB.Interfaces;

namespace LothiumDB.Data.Providers;

/// <summary>
/// Defines A Provider For A Microsoft SQL Server's Database Instance
/// </summary>
public sealed class MsSqlServerProvider : IDatabaseProvider
{
    public ProviderTypesEnum DbProviderType { get; } = ProviderTypesEnum.MicrosoftSqlServer;
    public string DbConnectionString { get; private set;  } = string.Empty;
    public string DbVariablePrefix { get; private set;  } = "@";

    public void CreateConnectionString(params object[] args)
    {
        if (!args.Any()) return;
        DbConnectionString = new SqlConnectionStringBuilder()
        {
            ConnectRetryCount = 2,
            ConnectTimeout = 30,
            DataSource = (string)args[0],
            UserID = (string)args[1],
            Password = (string)args[2],
            InitialCatalog = (string)args[3],
            CurrentLanguage = (string)args[4],
            Encrypt = (bool)args[5],
            TrustServerCertificate = (bool)args[6]
        }.ConnectionString;
    }

    public IDbConnection CreateConnection(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        return new SqlConnection(connectionString);
    }

    public SqlBuilder BuildPageQuery<T>(PageObject<T> pageObj, SqlBuilder sql)
    {
        sql.Append($"OFFSET {pageObj.ItemsForEachPage} ROWS")
            .Append($"FETCH NEXT {pageObj.ItemsToBeSkipped} ROWS ONLY");
        return sql;
    }

    public SqlBuilder CreateAuditTable()
    {
        return new SqlBuilder(@"
            SET ANSI_NULLS ON
            SET QUOTED_IDENTIFIER ON

            /* Create the table */
            CREATE TABLE [dbo].[AuditEvents](
	            [AuditID] [int] IDENTITY(1,1) NOT NULL,
                [AuditLevel] [nvarchar](32) NOT NULL,
	            [AuditUser] [nvarchar](64) NOT NULL,
	            [ExecutedOn] [date] NOT NULL,
	            [DbCommandType] [nvarchar](32) NOT NULL,
	            [SqlCommandType] [nvarchar](32) NOT NULL,
	            [SqlCommandOnly] [nvarchar](max) NULL,
	            [SqlCommandComplete] [nvarchar](max) NULL,
                [ErrorMessage] [nvarchar](max) NULL
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
            ALTER TABLE [dbo].[AuditEvents] ADD  CONSTRAINT [PK_AuditEvents] PRIMARY KEY CLUSTERED 
            (
	            [AuditID] ASC
            ) WITH (
                PAD_INDEX = OFF,
                STATISTICS_NORECOMPUTE = OFF,
                SORT_IN_TEMPDB = OFF,
                IGNORE_DUP_KEY = OFF,
                ONLINE = OFF,
                ALLOW_ROW_LOCKS = ON,
                ALLOW_PAGE_LOCKS = ON
            ) ON [PRIMARY]
        ");
    }
}