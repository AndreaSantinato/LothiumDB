// System Class
using System;

namespace LothiumDB.Core
{
    internal interface IDatabaseTransactionObject
    {
        /// <summary>
        /// Begim a new database's transaction
        /// </summary>
        void BeginDatabaseTransaction();

        /// <summary>
        /// Commit all operation executed during a database's transaction
        /// </summary>
        void CommitDatabaseTransaction();

        /// <summary>
        /// Abort all operation executed during a database's transaction
        /// </summary>
        void RollbackDatabaseTransaction();
    }
}
