using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LothiumDB.Interfaces
{
    public interface IProviderBuilder
    {
        /// <summary>
        /// Contains the current loaded db provider's connections string
        /// </summary>
        string ConnectionString => GetConnectionString();

        /// <summary>
        /// Load the choosen database provider's type
        /// </summary>
        /// <param name="type">Contains the db provider's type</param>
        void LoadDBProvider(ProviderTypes type);

        /// <summary>
        /// Return the loaded database provider's type
        /// </summary>
        /// <returns></returns>
        IDbProvider GetDatabaseProvider();

        /// <summary>
        /// Return the generated connection's string
        /// </summary>
        /// <returns>A String with all the connection needed informations</returns>
        string GetConnectionString();

        /// <summary>
        /// Generate the connection string based on the db provider's type
        /// </summary>
        /// <param name="connStringValues">Contains all the values needed for the connection</param>
        void SetConnectionString(params object[] connStringValues);
    }
}
