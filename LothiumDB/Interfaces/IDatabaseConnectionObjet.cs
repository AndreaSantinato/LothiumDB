// System Class
using System;

namespace LothiumDB.Interfaces
{
    internal interface IDatabaseConnectionObjet
    {
        /// <summary>
        /// Open the current generated connection
        /// </summary>
        void OpenDatabaseConnection();

        /// <summary>
        /// Close the current generated connection
        /// </summary>
        void CloseDatabaseConnection();
    }
}
