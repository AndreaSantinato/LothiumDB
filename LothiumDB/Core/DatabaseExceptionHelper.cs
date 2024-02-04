using System.Data;
using System.Diagnostics.CodeAnalysis;
using LothiumDB.Configurations;
using LothiumDB.Core.Enumerations;

namespace LothiumDB.Core;

/// <summary>
/// Helper class that validate specific components used inside the database context classes
/// </summary>
internal static class DatabaseExceptionHelper
{
    /// <summary>
    /// Check through all database configuration to validate each property
    /// If one required property is not valid it will throw an exception
    /// </summary>
    /// <param name="dbConfiguration">Contains the database context's configuration</param>
    /// <exception cref="ArgumentException">Contains the dedicated error for every property</exception>
    internal static void ValidateDatabaseContextConfiguration([NotNull] DatabaseConfiguration? dbConfiguration)
    {
        ArgumentNullException.ThrowIfNull(dbConfiguration);
        ArgumentNullException.ThrowIfNull(dbConfiguration.Provider);
        if (dbConfiguration.Provider.GetProviderType() == ProviderTypesEnum.None)
            throw new ArgumentException("Provider's Type Not Valid!");
        if (string.IsNullOrEmpty(dbConfiguration.Provider.GetConnectionString()))
            throw new ArgumentException("Connection String Not Specified!");
        if (string.IsNullOrEmpty(dbConfiguration.Provider.GetVariablePrefix()))
            throw new ArgumentException("Variable Prefix Not Specified!");
    }

    /// <summary>
    /// Check through the database connection to validate its values
    /// </summary>
    /// <param name="dbConnection">Contains the database context's connection</param>
    /// <exception cref="ArgumentException">Contains a dedicated error for the connection object</exception>
    internal static void ValidateDatabaseContextConnection([NotNull] IDbConnection? dbConnection)
    {
        ArgumentNullException.ThrowIfNull(dbConnection);
        if (string.IsNullOrEmpty(dbConnection.ConnectionString))
            throw new ArgumentException("Connection String Not Specified!");
    }

    internal static void ValidateDbContextTransaction()
    {
        throw new NotImplementedException();
    }
}