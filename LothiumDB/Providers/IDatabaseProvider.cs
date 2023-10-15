// System Class
using System;
using System.Data;
using LothiumDB.Core;
// Custom Class
using LothiumDB.Enumerations;

namespace LothiumDB.Providers
{
    /// <summary>
    /// Define the logic structure of a Database's Provider
    /// </summary>
    public interface IDatabaseProvider
    {
        /// <summary>
        /// Create a new Specific Connection String for the specific Database Connection provider's type
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        abstract string CreateConnectionString(params object[] args);

        /// <summary>
        /// Create a new Specific Connection from a databbase's connection string
        /// </summary>
        /// <returns></returns>
        abstract IDbConnection CreateConnection(string connectionString);

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
        /// Genereate a new query for a pagination element data
        /// </summary>
        /// <param name="pageObj">Contains the page object that store all the information for the pagination</param>
        /// <param name="sql">Contains the actual query for retriving data from the database to add the pagination values</param>
        /// <returns>Return a string with the pagination query</returns>
        abstract SqlBuilder BuildPageQuery<T>(PageObject<T> pageObj, SqlBuilder sql);
    }
}
