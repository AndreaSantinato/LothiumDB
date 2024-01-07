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
    /// <summary>
    /// Create a new instance of the MySql database provider
    /// </summary>
    /// <param name="connectionString">Contains the specific connection string</param>
    public MySqlProvider(string connectionString) : base(ProviderTypesEnum.MySql, connectionString, "@") { }

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
        var conn = new MySqlConnection(base.ConnectionString);
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
        sql.Append($"LIMIT {pageObj.ItemsForEachPage}").Append($"OFFSET {pageObj.ItemsToBeSkipped}");
        return sql;
    }
    
    /// <summary>
    /// Generate a query to check if the audit table exists inside the database instance
    /// </summary>
    /// <returns></returns>
    public SqlBuilder CheckIfAuditTableExists()
    {
        return new SqlBuilder(@"
            SHOW TABLES LIKE 'AuditEvents';
            SELECT FOUND_ROWS();
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