namespace LothiumDB.Configurations;

/// <summary>
/// Defines a database configuration instance
/// </summary>
public class DatabaseConfiguration
{
    /// <summary>
    /// Contains the database's provider configurations
    /// </summary>
    public ProviderConfiguration Provider { get; internal set; } = new ProviderConfiguration();
    
    /// <summary>
    /// Contains the database's audit configurations
    /// </summary>
    public AuditConfiguration Audit { get; internal set; } = new AuditConfiguration();
    
    /// <summary>
    /// Build a new database instance based on the chosen configurations
    /// </summary>
    /// <returns>A new database instance</returns>
    public Database? BuildDatabase()
    {
        var prov = Provider.DbProvider;
        var connString = Provider.ConnectionString;
        var auditMode = Audit.IsEnable;
        
        if (prov == null) return null;
        if (string.IsNullOrEmpty(connString)) return null;
        
        var db = new Database(prov, connString);

        if (auditMode)
            db.EnableAuditMode(Audit.AuditUser);
        else
            db.DisableAuditMode();

        return db;
    }
}