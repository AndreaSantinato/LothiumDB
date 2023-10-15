// System Class
using System;

namespace LothiumDB.Core
{
    internal interface IDatabaseConnectionObject
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
