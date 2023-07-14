// System Class
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Custom Class
using LothiumDB.Enumerations;

namespace LothiumDB.Interfaces
{
    public interface IDbProvider
    {
        /// <summary>
        /// Genereate a new Specific Connection String for the specific Database Connection provider's type
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        abstract string GenerateConnectionString(params object[] args);

        /// <summary>
        /// Return the Variable Character for the specific Database Connection provider's type
        /// </summary>
        /// <returns>Return a string</returns>
        abstract string VariablePrefix();

        /// <summary>
        /// Return the Provider's Type
        /// </summary>
        /// <returns>Return a ProviderTypes Enum Value</returns>
        abstract ProviderTypes ProviderType();

        /// <summary>
        /// Genereate a new query for a pagination element data
        /// </summary>
        /// <param name="query">Contains the Select All query to add the pagination values</param>
        /// <param name="offset">Contains the numnber of element to skip before start retrieving data, if 0 skip no element</param>
        /// <param name="element">Contains the number of element to retrie, if 0 retrive all</param>
        /// <returns>Return a string with the pagination query</returns>
        abstract string BuildPageQuery(string query, long offset, long element);
    }
}
