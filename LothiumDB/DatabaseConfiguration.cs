using System.Data;
using LothiumDB.Enumerations;

namespace LothiumDB;

/// <summary>
/// Define a set of property needed by the Database class to perform operation
/// inside an existing database's instance
/// </summary>
public class DatabaseConfiguration()
{
    /// <summary>
    /// Contains a connection object to a chosen database's instance
    /// </summary>
    public IDbConnection? Connection { get; set; }

    /// <summary>
    /// Indicates the type of the connection object provided
    /// </summary>
    public DatabaseProviderTypesEnum Type { get; set; }
    
    /// <summary>
    /// Indicates what type of character is used by the connection object to identify variables
    /// </summary>
    public string? VariablePrefix { get; set; }
    
    /// <summary>
    /// Indicates the total time to wait for a command to be performed
    /// into the provided database's connection object
    /// </summary>
    public int? CommandTimeout { get; set; }
}