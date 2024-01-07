// Custom Class
using LothiumDB.Enumerations;

namespace LothiumDB.Data.Providers;

/// <summary>
/// Defines A Provider For A MariaDB's Database Instance
/// </summary>
public class MariaDbProvider : MySqlProvider
{
    /// <summary>
    /// Create a new instance of the MariaDB database provider
    /// </summary>
    /// <param name="connectionString">Contains the specific connection string</param>
    public MariaDbProvider(string connectionString) : base(connectionString)
    {
        base.Type = ProviderTypesEnum.MariaDb;
    }

    /// <summary>
    /// Create a new instance of the MariaDB database provider
    /// </summary>
    /// <param name="server">Contains the server instance</param>
    /// <param name="userId">Contains the user</param>
    /// <param name="password">Contains the password</param>
    /// <param name="database">Contains the database's name</param>
    /// <param name="sqlServerMode">Indicates if the connection must use the sql mode</param>
    public MariaDbProvider(
        string server, 
        string userId,
        string password,
        string database, 
        bool sqlServerMode
    ) : base(
        server,
        userId, 
        password,
        database, 
        sqlServerMode
    )
    {
        base.Type = ProviderTypesEnum.MariaDb;
    }
}