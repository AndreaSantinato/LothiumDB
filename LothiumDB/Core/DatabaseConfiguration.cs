// Custom Classes
using LothiumDB.Core.Interfaces;

// Configurations Namespace
namespace LothiumDB.Core;

/// <summary>
/// Centralized class that contains all the required object
/// for the database class
/// </summary>
internal class DatabaseConfiguration
{
    /// <summary>
    /// Contains the database communication's provider
    /// </summary>
    public IProvider? Provider { get; set; }

    /// <summary>
    /// Indicates the timeout for the execution of all the query
    /// </summary>
    public int QueryTimeOut { get; set; }

    /// <summary>
    /// Indicates if the audit mode is enabled or not
    /// </summary>
    public bool AuditMode { get; set; }
}