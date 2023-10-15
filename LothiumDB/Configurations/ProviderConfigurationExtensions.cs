// Custom Class
using LothiumDB.Providers;

namespace LothiumDB.Configurations;

/// <summary>
/// Defines a Database Provider Configuration
/// </summary>
public sealed class ProviderConfiguration
{
    /// <summary>
    /// Defines the type of the chosen database provider
    /// </summary>
    internal IDatabaseProvider? DbProvider { get; private set; }
    
    /// <summary>
    /// Contains the full formatted connection string
    /// </summary>
    internal string? ConnectionString { get; private set; }
    
    /// <summary>
    /// Defines a provider configuration instance
    /// </summary>
    internal ProviderConfiguration()
    {
        DbProvider = null;
        ConnectionString = null;
    }
    
    /// <summary>
    /// This method retrieve the database's provider class from a specific name
    /// </summary>
    /// <param name="name">Contains the name of the provider</param>
    /// <returns>A database specific provider</returns>
    internal IDatabaseProvider InitializeProviderByName(string name)
    {
        DbProvider = name switch
        {
            nameof(MsSqlServerProvider) => new MsSqlServerProvider(),
            nameof(MySqlProvider) => new MySqlProvider(),
            _ => DbProvider
        };

        return DbProvider ??= new MsSqlServerProvider();
    }
    
    /// <summary>
    /// Add the full formatted connection string
    /// </summary>
    /// <param name="connectionString">Contains the connection string</param>
    /// <returns>A ProviderConfiguration Object</returns>
    internal ProviderConfiguration SetConnectionString(string connectionString)
    {
        if (DbProvider == null) throw new ArgumentNullException(nameof(DbProvider));
        
        ConnectionString = connectionString;
        return this;
    }
    
    /// <summary>
    /// Add the full formatted connection string
    /// </summary>
    /// <param name="connectionStringValues">Contains all the values to generate a new connection string</param>
    /// <returns>A ProviderConfiguration Object</returns>
    internal ProviderConfiguration SetConnectionStringFromValues(object[] connectionStringValues)
    {
        if (!connectionStringValues.Any()) throw new ArgumentNullException(nameof(connectionStringValues));
        if (DbProvider == null) throw new ArgumentNullException(nameof(DbProvider));
        
        ConnectionString = DbProvider.CreateConnectionString(connectionStringValues);
        return this;
    }
}

/// <summary>
/// Provide a set of methods to build a new provider configuration
/// </summary>
public static class ProviderConfigurationBuilder
{
    /// <summary>
    /// Add a new provider to the configuration by the name
    /// </summary>
    /// <param name="configuration">Contains the current provider configuration</param>
    /// <param name="providerName">Contains the name of the provider</param>
    /// <returns>A database provider configuration object</returns>
    public static ProviderConfiguration AddProvider(this ProviderConfiguration configuration, string providerName)
    {
        configuration.InitializeProviderByName(providerName);
        return configuration;
    }

    /// <summary>
    /// Set the provider connection string
    /// </summary>
    /// <param name="configuration">Contains the current provider configuration</param>
    /// <param name="connectionString"></param>
    /// <returns>A database provider configuration object</returns>
    /// <exception cref="ArgumentNullException">Return an exception if the provider is not initialized</exception>
    public static ProviderConfiguration AddConnectionString(this ProviderConfiguration configuration, string connectionString)
    {
        if (configuration.DbProvider == null) throw new ArgumentNullException(nameof(configuration.DbProvider));

        configuration.SetConnectionString(connectionString);
        return configuration;
    }
    
    /// <summary>
    /// Set the provider connection string from as set of values
    /// </summary>
    /// <param name="configuration">Contains the current provider configuration</param>
    /// <param name="values">Contains all the connection string values</param>
    /// <returns>A database provider configuration object</returns>
    /// <exception cref="ArgumentNullException">Return an exception if the connection string values are null</exception>
    public static ProviderConfiguration AddConnectionString(this ProviderConfiguration configuration, params object[] values)
    {
        if (!values.Any()) throw new ArgumentNullException(nameof(values));
        configuration.SetConnectionStringFromValues(values);
        return configuration;
    }
}