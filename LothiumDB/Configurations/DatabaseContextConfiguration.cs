// Custom Classes
using LothiumDB.Interfaces;

// Configurations Namespace
namespace LothiumDB.Configurations;

public class DatabaseContextConfiguration
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