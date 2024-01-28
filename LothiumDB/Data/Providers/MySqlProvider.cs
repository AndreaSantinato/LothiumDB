// System Classes
using System.Data;
// Custom Classes
using LothiumDB.Enumerations;
using LothiumDB.Extensions;
using LothiumDB.Interfaces;
// NuGet Packages
using MySql.Data.MySqlClient;

// Providers Namespace
namespace LothiumDB.Data.Providers;

/// <summary>
/// Defines A Provider For A MySql Database Instance
/// </summary>
public class MySqlProvider : BaseProvider, IProvider
{
    #region Provider's Constructors
    
    /// <summary>
    /// Create a new instance of the MySql database provider
    /// </summary>
    /// <param name="connectionString">Contains the specific connection string</param>
    public MySqlProvider(string connectionString) 
        : base(ProviderTypesEnum.MySql, connectionString, "@") { }

    /// <summary>
    /// Create a new instance of the MySql database provider
    /// </summary>
    /// <param name="server">Contains the server instance</param>
    /// <param name="userId">Contains the user</param>
    /// <param name="password">Contains the password</param>
    /// <param name="database">Contains the database's name</param>
    /// <param name="sqlServerMode">Indicates if the connection must use the sql mode</param>
    public MySqlProvider(
        string server,
        string userId,
        string password, 
        string database, 
        bool sqlServerMode
    ) : this(
        new MySqlConnectionStringBuilder()
        {
            DefaultCommandTimeout = 30,
            Server = server,
            UserID = userId,
            Password = password,
            Database = database,
            SqlServerMode = sqlServerMode
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
        return new MySqlConnection(base.ConnectionString);
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
            .Append($"LIMIT {pageObj.ItemsForEachPage}")
            .Append($"OFFSET {pageObj.ItemsToBeSkipped}");
    }
    
    /// <summary>
    /// Generate a query to check if the audit table exists inside the database instance
    /// </summary>
    /// <returns></returns>
    public SqlBuilder CheckIfAuditTableExists()
    {
        return new SqlBuilder(@"
            /* Check if audit table exists inside the database instance */

            SELECT  * 
            FROM    information_schema.tables
            WHERE   table_schema = @0 
                    AND table_name = @1
            LIMIT 1;
        ", new MySqlConnectionStringBuilder(this.ConnectionString).Database, "AuditEvents");
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

            CREATE TABLE AuditEvents
            (
	            query_execution_date date NOT NULL,
                query_text nvarchar(max) NULL,
                query_error bit NOT NULL,
                query_error_message nvarchar(max) NULL
            );
        ");
    }
    
    #endregion Provider's Methods
}