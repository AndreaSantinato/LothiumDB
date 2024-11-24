using System.Data;
using LothiumDB.Exceptions;

namespace LothiumDB;

public class DatabaseBuilder : IDatabaseBuilder
{
    private readonly DatabaseConfiguration _configuration = new DatabaseConfiguration();
    
    /// <summary>
    /// Initialize a new builder that will be used to create a new instance the Database class
    /// </summary>
    /// <returns>A new builder with no configuration set</returns>
    public static DatabaseBuilder CreateBuilder()
        => new DatabaseBuilder();

    /// <summary>
    /// Define a new provider for an existing database instance server
    /// </summary>
    /// <param name="type">Indicate the provider's type (MSSql, MySql, ecc...)</param>
    /// <param name="connection">Contains the actual connection object</param>
    /// <param name="variablePrefix">Indicates what type of variable the provider will use inside the sql commands</param>
    /// <returns></returns>
    public DatabaseBuilder AddProvider(DatabaseProviderTypesEnum type, IDbConnection connection, string variablePrefix)
    {
        _configuration.Type = type;
        _configuration.Connection = connection;
        _configuration.VariablePrefix = variablePrefix;
        
        return this;
    }

    /// <summary>
    /// Set a new value that indicates the maximum time to wait for each database command to be executed
    /// If the operation take much time it will generate an error during the runtime
    /// </summary>
    /// <param name="commandTimeOut">The maximum value to wait during the execution of all database operations</param>
    /// <returns>The current builder updated with new configurations</returns>
    public DatabaseBuilder SetCommandTimeOut(int commandTimeOut)
    {
        _configuration.CommandTimeout = commandTimeOut;
        
        return this;
    }

    /// <summary>
    /// Create a new instance of the Database class using the provided configurations
    /// </summary>
    /// <returns>A new object of the database class</returns>
    public Database Build()
    {
        if (_configuration.Type.Equals(DatabaseProviderTypesEnum.None))
            throw new DatabaseException("Specify a valid provider's type!");
        
        if (_configuration.Connection is null)
            throw new DatabaseException("Specify a valid connection object!");
        
        if (string.IsNullOrEmpty(_configuration.VariablePrefix))
            throw new DatabaseException("Specify a valid variable prefix!");
        
        if (_configuration.CommandTimeout is null or <= 0)
            throw new DatabaseException("Specify a valid command timeout!");
        
        return new Database(_configuration);
    }
}