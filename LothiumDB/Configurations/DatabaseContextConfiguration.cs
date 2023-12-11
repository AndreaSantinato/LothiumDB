using LothiumDB.Interfaces;

namespace LothiumDB.Configurations;

public class DatabaseContextConfiguration
{
    /// <summary>
    /// Contains the database communication's provider
    /// </summary>
    public IDatabaseProvider? Provider { get; set; }
    
    /// <summary>
    /// Contains the connection string to a database instance
    /// </summary>
    public string? ConnectionString { get; set; }
    
    /// <summary>
    /// Indicates the timeout for the execution of all the query
    /// </summary>
    public int QueryTimeOut { get; set; }
    
    /// <summary>
    /// Indicates if the audit mode is enabled or not
    /// </summary>
    public bool AuditMode { get; set; }
    
    /// <summary>
    /// Indicates the user that will be used to track audit events
    /// </summary>
    public string? AuditUser { get; set; }
}