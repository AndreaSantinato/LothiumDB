using System.Data;
using LothiumDB.Core.Enumerations;
using LothiumDB.Core.Interfaces;
using Npgsql;

// Namespace
namespace LothiumDB.DatabaseProviders;

/// <summary>
/// Defines A Provider For A PostgreSQL Database Instance
/// </summary>
public sealed class PostgreSqlProvider : BaseProvider, IProvider
{
    #region Provider's Constructors
    
    /// <summary>
    /// Create a new instance of the PostgreSql database provider
    /// </summary>
    /// <param name="connectionString">Contains the specific connection string</param>
    public PostgreSqlProvider(string connectionString) 
        : base(ProviderTypesEnum.PostgreSql, connectionString, "@") { }
    
    /// <summary>
    /// Create a new instance of the MsSqlServer database provider
    /// </summary>
    /// <param name="host">Contains the database instance</param>
    /// <param name="username">Contains the user</param>
    /// <param name="password">Contains the password</param>
    /// <param name="database">Contains the database's name</param>
    public PostgreSqlProvider(
        string host,
        string username,
        string password,
        string database
    ) : this(
        new NpgsqlConnectionStringBuilder()
        {
            Timeout = 30,
            Host = host,
            Username = username,
            Password = password,
            Database = database
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
        return new NpgsqlConnection(base.ConnectionString);
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

            SELECT EXISTS (
                SELECT FROM pg_tables
                WHERE  schemaname = @0
                AND    tablename  = @1
            );
        ", new NpgsqlConnectionStringBuilder(this.ConnectionString).Database!, "AuditEvents");
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

            CREATE TABLE employees (
                query_execution_date DATE NOT NULL,
                query_text VARCHAR(MAX) NULL,
                query_error BOOLEAN NOT NULL,
                query_error_message VARCHAR(MAX) NULL
            );
        ");
    }
    
    #endregion Provider's Methods
}