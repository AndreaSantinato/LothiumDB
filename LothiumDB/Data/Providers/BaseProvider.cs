using LothiumDB.Enumerations;

// Providers Namespace
namespace LothiumDB.Data.Providers;

public class BaseProvider
{
    /// <summary>
    /// Indicates the type of the provider
    /// </summary>
    protected ProviderTypesEnum Type { get; init; }

    /// <summary>
    /// Indicates the connection string that will be utilize to connect
    /// to the database instance
    /// </summary>
    protected string? ConnectionString { get; init; }

    /// <summary>
    /// Indicates prefix that will be used to generate new parameter's variable
    /// during query generations
    /// </summary>
    protected string? VariablePrefix { get; init; }
    
    /// <summary>
    /// Define a new generic provider instance
    /// </summary>
    /// <param name="dbProvType">Contains the chosen provider's type</param>
    /// <param name="connectionString">Contains the actual string for connecting to a database instance</param>
    /// <param name="variablePrefix">Contains the prefix needed to generate new parameter's variables</param>
    protected BaseProvider(ProviderTypesEnum dbProvType, string connectionString, string variablePrefix)
    {
        Type = dbProvType;
        ConnectionString = connectionString;
        VariablePrefix = variablePrefix;
    }
}