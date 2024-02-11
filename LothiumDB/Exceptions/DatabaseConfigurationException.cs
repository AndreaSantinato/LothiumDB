using System.Diagnostics.CodeAnalysis;
using LothiumDB.Core;
using LothiumDB.Core.Enumerations;

namespace LothiumDB.Exceptions;

/// <summary>
/// Define a DatabaseConfiguration custom exception
/// It provided a set of methods to validate the db's configuration object
/// </summary>
internal class DatabaseConfigurationException : Exception
{
    #region Constructors

    public DatabaseConfigurationException() : base() { }

    public DatabaseConfigurationException(string? message) : base(message) { }

    public DatabaseConfigurationException(string? message, DatabaseConfigurationException? innerException) : base(message, innerException) { }

    #endregion Constructors

    /// <summary>
    /// Throw an exception if the passed configuration is null
    /// </summary>
    /// <param name="configuration">Contains the db's configuration to check</param>
    /// <exception cref="DatabaseConfigurationException"></exception>
    public static void ThrowIfConfigurationIsNull([NotNull] DatabaseConfiguration? configuration)
    {
        if (configuration is null) 
            ThrowException("The provided configuration is not valid!");
    }

    /// <summary>
    /// Throw an exception if the passed configuration is null or one of it's property is not correctly set
    /// </summary>
    /// <param name="configuration">Contains the db's configuration to check</param>
    /// <exception cref="DatabaseConfigurationException"></exception>
    public static void ThrowIfConfigurationIsNotValid([NotNull] DatabaseConfiguration? configuration)
    {
        ThrowIfConfigurationIsNull(configuration);
        
        if (configuration.Provider is null)
            ThrowException("The provider is not valid!");

        if (configuration.Provider.GetProviderType() == ProviderTypesEnum.None)
            ThrowException("Provider's Type Not Valid!");

        if (string.IsNullOrEmpty(configuration.Provider.GetConnectionString()))
            ThrowException("Connection String Not Specified!");

        if (string.IsNullOrEmpty(configuration.Provider.GetVariablePrefix()))
            ThrowException("Variable Prefix Not Specified!");
    }

    [DoesNotReturn]
    private static void ThrowException(string? errorMessage) 
        => throw new DatabaseConfigurationException(errorMessage);
}
