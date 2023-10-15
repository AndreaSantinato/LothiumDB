// System Class
using System.Data;

namespace LothiumDB.Core
{
    /// <summary>
    /// Database Transaction Object
    /// </summary>
    internal class DatabaseTransactionObject : IDatabaseTransactionObject
    {
        private readonly DatabaseConnectionObject connObj;
        private IDbTransaction? _transaction = null;

        #region Public Properties

        /// <summary>
        /// The database's connection used for the transaction object
        /// </summary>
        public IDbConnection Connection => connObj.DatabaseConnection;

        /// <summary>
        /// The database's generated transaction
        /// </summary>
        public IDbTransaction? Transaction => _transaction;

        #endregion

        #region Class Constructor

        /// <summary>
        /// Class Constructor
        /// </summary>
        /// <param name="databaseConnectionObjet">contains the database connection object inizialized by the database class</param>
        public DatabaseTransactionObject(DatabaseConnectionObject databaseConnectionObjet)
        {
            connObj = databaseConnectionObjet;
        }

        #endregion

        #region Class Methods

        /// <summary>
        /// Begim a new database's transaction
        /// </summary>
        public void BeginDatabaseTransaction()
        {
            connObj.OpenDatabaseConnection();
            _transaction = connObj.CreateNewTransaction();
        }

        /// <summary>
        /// Commit all operation executed during a database's transaction
        /// </summary>
        public void CommitDatabaseTransaction() 
        {
            _transaction?.Commit();
            _transaction?.Dispose();
            _transaction = null;

            connObj.CloseDatabaseConnection();
        }

        /// <summary>
        /// Abort all operation executed during a database's transaction
        /// </summary>
        public void RollbackDatabaseTransaction() 
        {
            _transaction?.Commit();
            _transaction?.Dispose();
            _transaction = null;

            connObj.CloseDatabaseConnection();
        }

        #endregion
    }
}
