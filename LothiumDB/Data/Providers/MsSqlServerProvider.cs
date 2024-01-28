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
    #region Provider's Constructors
    
    /// <summary>
    /// Create a new instance of the MsSqlServer database provider
    /// </summary>
    /// <param name="connectionString">Contains the specific connection string</param>
    public MsSqlServerProvider(string connectionString) 
        : base(ProviderTypesEnum.MicrosoftSqlServer, connectionString, "@") { }

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

    #endregion Provider's Constructors
    
    #region Provider's Property
    
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

    #endregion Provider's Property
    
    #region Provider's Methods
    
    /// <summary>
    /// Create a new connection based on specified connection string
    /// </summary>
    /// <returns>A Formatted connection string for the MsSqlServer provider</returns>
    /// <exception cref="ArgumentNullException">Generate an exception if the connection string is null or empty</exception>
    public IDbConnection CreateConnection()
    {
        // 1) Check if the passed/generated connection string is correct before create a new database's connection
        // 2) Generate the new connection string object
        // 3) Return the final result
        
        ArgumentNullException.ThrowIfNull(base.ConnectionString);
        return new SqlConnection(base.ConnectionString);
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
        return sql
            .Append($"OFFSET {pageObj.ItemsForEachPage} ROWS")
            .Append($"FETCH NEXT {pageObj.ItemsToBeSkipped} ROWS ONLY");
    }

    /// <summary>
    /// Generate a query to check if the audit table exists inside the database instance
    /// </summary>
    /// <returns></returns>
    public SqlBuilder CheckIfAuditTableExists()
    {
        return new SqlBuilder(@"
            /* Check if audit table exists inside the database instance */

            SELECT  COUNT(*)
            FROM    INFORMATION_SCHEMA.TABLES
            WHERE   TABLE_SCHEMA = @0
                    AND TABLE_NAME = @1
        ", "dbo", "AuditEvents");
    }

    /// <summary>
    /// Generate a query for the 'AuditTable' creation script
    /// </summary>
    /// <returns></returns>
    public SqlBuilder CreateAuditTable()
    {
        return new SqlBuilder($@"
            /* 
                Script Generated On: {DateTime.Now:yyyy/MM/dd hh:mm:ss}
                Script Description: Create the 'AuditEvents' table inside the database instance 
            */

            CREATE TABLE [dbo].[AuditEvents]
            (
	            [query_execution_date] [date] NOT NULL,
	            [query_text] [nvarchar](max) NULL,
                [query_error] [bit] NOT NULL,
                [query_error_message] [nvarchar](max) NULL
            )
        ");
    }
    
    #endregion Provider's Methods
}