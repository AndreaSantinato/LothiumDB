// System Class
using System.Data;
// Custom Class
using LothiumDB.Enumerations;
using LothiumDB.Extensions;
using LothiumDB.Interfaces;
// NuGet Packages
using Npgsql;

// Namespace
namespace LothiumDB.Data.Providers;

/// <summary>
/// Defines A Provider For A PostgreSQL Database Instance
/// </summary>
public sealed class PostgreSqlProvider : BaseProvider, IProvider
{
    /// <summary>
    /// Create a new instance of the PostgreSql database provider
    /// </summary>
    /// <param name="connectionString">Contains the specific connection string</param>
    public PostgreSqlProvider(string connectionString) : base(ProviderTypesEnum.PostgreSql, connectionString, "@") { }
    
    /// <summary>
    /// Create a new instance of the MsSqlServer database provider
    /// </summary>
    /// <param name="host">Contains the database instance</param>
    /// <param name="username">Contains the user</param>
    /// <param name="password">Contains the password</param>
    /// <param name="database">Contains the database's name</param>
    /// <param name="trustServerCertificate">Indicates if the database instance need a trusted certificate</param>
    public PostgreSqlProvider(
        string host,
        string username,
        string password,
        string database,
        bool trustServerCertificate
    ) : this(
        new NpgsqlConnectionStringBuilder()
        {
            Timeout = 30,
            Host = host,
            Username = username,
            Password = password,
            Database = database,
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
        var conn = new NpgsqlConnection(base.ConnectionString);
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
        throw new NotImplementedException();
    }

    /// <summary>
    /// Generate a query to check if the audit table exists inside the database instance
    /// </summary>
    /// <returns></returns>
    public SqlBuilder CheckIfAuditTableExists()
    {
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// Generate a query for the 'AuditTable' creation script
    /// </summary>
    /// <returns></returns>
    public SqlBuilder CreateAuditTable()
    {
        throw new NotImplementedException();
    }
}