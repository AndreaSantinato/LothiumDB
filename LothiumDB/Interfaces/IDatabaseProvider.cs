// System Class
using System.Data;
// Custom Class
using LothiumDB.Enumerations;
using LothiumDB.Extensions;

namespace LothiumDB.Interfaces;

/// <summary>
/// Define the logic structure of a Database's Provider
/// </summary>
public interface IDatabaseProvider
{
    /// <summary>
    /// Return the Variable Character for the specific Database Connection provider's type
    /// </summary>
    /// <returns>Return a string</returns>
    abstract string VariablePrefix();

    /// <summary>
    /// Return the Provider's Type
    /// </summary>
    /// <returns>Return a ProviderTypes Enum Value</returns>
    abstract ProviderTypesEnum ProviderType();
    
    /// <summary>
    /// Create a new Specific Connection String for the specific Database Connection provider's type
    /// </summary>
    /// <param name="args"></param>
    /// <returns>The final formatted database connection string</returns>
    abstract string CreateConnectionString(params object[] args);

    /// <summary>
    /// Create a new Specific Connection from a database's connection string
    /// </summary>
    /// <returns>A database connection object</returns>
    abstract IDbConnection CreateConnection(string connectionString);

    /// <summary>
    /// Generate a new query for a pagination element data
    /// </summary>
    /// <param name="pageObj">Contains the page object that store all the information for the pagination</param>
    /// <param name="sql">Contains the actual query for retrieving data from the database to add the pagination values</param>
    /// <returns>A string with the pagination query</returns>
    abstract SqlBuilder BuildPageQuery<T>(PageObject<T> pageObj, SqlBuilder sql);

    /// <summary>
    /// Generate a new query for the creation of the audit table inside the provided database instance
    /// </summary>
    /// <returns>An SqlBuilder object</returns>
    abstract SqlBuilder CreateAuditTable();
}