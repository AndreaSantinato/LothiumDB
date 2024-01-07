// System Classes
using System.Data;
using System.Data.SqlClient;
// Custom Classes
using LothiumDB.Enumerations;
using LothiumDB.Extensions;
using LothiumDB.Interfaces;

// Providers Namespace
namespace LothiumDB.Data.Providers;

/// <summary>
/// Defines A Provider For A Microsoft SQL Server's Database Instance
/// </summary>
public class MsSqlServerProvider : BaseProvider, IProvider
{
    /// <summary>
    /// Create a new instance of the MsSqlServer database provider
    /// </summary>
    /// <param name="connectionString">Contains the specific connection string</param>
    public MsSqlServerProvider(string connectionString) : base(ProviderTypesEnum.MicrosoftSqlServer, connectionString, "@") { }

    /// <summary>
    /// Create a new instance of the MsSqlServer database provider
    /// </summary>
    /// <param name="dataSource">Contains the data source</param>
    /// <param name="userId">Contains the user</param>
    /// <param name="password">Contains the password</param>
    /// <param name="initialCatalog">Contains the database's name</param>
    /// <param name="currentLanguage">Contains the chosen language</param>
    /// <param name="encrypt">Indicates if the connection must by encrypted</param>
    /// <param name="trustServerCertificate">Indicates if the database instance need a trusted certificate</param>
    public MsSqlServerProvider(
        string dataSource,
        string userId,
        string password,
        string initialCatalog,
        string currentLanguage,
        bool encrypt,
        bool trustServerCertificate
    ) : this(
        new SqlConnectionStringBuilder()
        {
            ConnectRetryCount = 2,
            ConnectTimeout = 30,
            DataSource = dataSource,
            UserID = userId,
            Password = password,
            InitialCatalog = initialCatalog,
            CurrentLanguage = currentLanguage,
            Encrypt = encrypt,
            TrustServerCertificate = trustServerCertificate
        }.ConnectionString
    ) { }

    /// <summary>
    /// Get the provider's type
    /// </summary>
    /// <returns>Current Provider's Type Enum</returns>
    public ProviderTypesEnum GetProviderType() => base.Type;

    /// <summary>
    /// Get the provider's connection string
    /// </summary>
    /// <returns>A Formatted Connection String</returns>
    public string? GetConnectionString() => base.ConnectionString;

    /// <summary>
    /// Get the provider parameter's variable prefix
    /// </summary>
    /// <returns>A Specific Variable's Prefix For Database Parameters</returns>
    public string? GetVariablePrefix() => base.VariablePrefix;

    /// <summary>
    /// Create a new connection based on specified connection string
    /// </summary>
    /// <returns>A Formatted connection string for the MsSqlServer provider</returns>
    /// <exception cref="ArgumentNullException">Generate an exception if the connection string is null or empty</exception>
    public IDbConnection CreateConnection()
    {
        // Check if the passed or generated connection string is null or empty
        // before create a new database's connection
        ArgumentNullException.ThrowIfNull(base.ConnectionString);
        
        // Create the new database connection and set the connection string
        var conn = new SqlConnection(base.ConnectionString);
        conn.ConnectionString = base.ConnectionString;
        
        // Return the final generated connection
        return conn;
    }

    /// <summary>
    /// Create a query for retrieve data in a paginated result
    /// </summary>
    /// <typeparam name="T">Indicates the type of the database's table</typeparam>
    /// <param name="pageObj">Contains the pagination object</param>
    /// <param name="sql">Contains the actual query</param>
    /// <returns></returns>
    public SqlBuilder BuildPageQuery<T>(PageObject<T> pageObj, SqlBuilder sql)
    {
        sql.Append($"OFFSET {pageObj.ItemsForEachPage} ROWS");
        sql.Append($"FETCH NEXT {pageObj.ItemsToBeSkipped} ROWS ONLY");
        return sql;
    }

    /// <summary>
    /// Generate a query to check if the audit table exists inside the database instance
    /// </summary>
    /// <returns></returns>
    public SqlBuilder CheckIfAuditTableExists()
    {
        return new SqlBuilder(@"
            EXEC sp_tables 'AuditEvents'
            SELECT @@ROWCOUNT
        ");
    }

    /// <summary>
    /// Generate a query for the 'AuditTable' creation script
    /// </summary>
    /// <returns></returns>
    public SqlBuilder CreateAuditTable()
    {
        return new SqlBuilder(@"
            /* Create the table */
            CREATE TABLE [dbo].[AuditEvents]
            (
	            [ExecutedOn] [date] NOT NULL,
	            [SqlQuery] [nvarchar](max) NULL,
                [IsError] [bit] NOT NULL,
                [ErrorMessage] [nvarchar](max) NULL
            )
        ");
    }
}