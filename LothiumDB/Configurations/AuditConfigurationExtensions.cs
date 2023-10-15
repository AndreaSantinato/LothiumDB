namespace LothiumDB.Configurations;

/// <summary>
/// Defines a Database Audit Configuration
/// </summary>
public sealed class AuditConfiguration
{
    /// <summary>
    /// Defines if the audit is enabled or disabled
    /// </summary>
    internal bool IsEnable { get; private set; } = false;

    /// <summary>
    /// Defines the audit user
    /// </summary>
    internal string AuditUser { get; private set; } = string.Empty;

    /// <summary>
    /// Enable the writing of audit events
    /// </summary>
    /// <returns>An audit configuration</returns>
    internal AuditConfiguration Enable()
    {
        IsEnable = true;
        return this;
    }

    /// <summary>
    /// Disable the writing of audit events
    /// </summary>
    /// <returns>An audit configuration</returns>
    internal AuditConfiguration Disable()
    {
        IsEnable = false;
        return this;
    }

    /// <summary>
    /// Add the user that will write all the audit events inside the specific database table
    /// </summary>
    /// <param name="user">Contains the user's name</param>
    /// <returns>An audit configuration</returns>
    internal AuditConfiguration User(string user)
    {
        AuditUser = user;
        return this;
    }
}

/// <summary>
/// Provide a set of methods to build a new audit configuration
/// </summary>
public static class AuditConfigurationBuilder
{
    /// <summary>
    /// Add and enable the writings of the audit events
    /// </summary>
    /// <param name="configuration">Contains the current audit configuration</param>
    /// <returns>An Audit Configuration Object</returns>
    public static AuditConfiguration AddAudit(this AuditConfiguration configuration)
    {
        configuration.Enable();
        configuration.User("System");
        return configuration;
    }

    /// <summary>
    /// Set the user for the writing of the audit events
    /// </summary>
    /// <param name="configuration">Contains the current audit configuration</param>
    /// <param name="auditUser">Contains the user that will be used during the writings of audit events</param>
    /// <returns>An Audit Configuration Object</returns>
    public static AuditConfiguration SetUser(this AuditConfiguration configuration, string auditUser)
    {
        configuration.User(auditUser);
        return configuration;
    }
    
    /// <summary>
    /// Remove and disable the writings of the audit events
    /// </summary>
    /// <param name="configuration">Contains the current audit configuration</param>
    /// <returns>An Audit Configuration Object</returns>
    public static AuditConfiguration RemoveAudit(this AuditConfiguration configuration)
    {
        configuration.Disable();
        configuration.User(string.Empty);
        return configuration;
    }
}