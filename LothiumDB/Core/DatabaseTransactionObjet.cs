// System Class
using System.Data;
// Custom Class
using LothiumDB.Interfaces;

namespace LothiumDB.Core
{
    /// <summary>
    /// Database Transaction Object
    /// </summary>
    internal class DatabaseTransactionObjet : IDatabaseTransactionObjet
    {
        private readonly DatabaseConnectionObjet connectionObjet;
        private IDbTransaction? _transaction = null;

        #region Public Properties

        /// <summary>
        /// The database's connection used for the transaction object
        /// </summary>
        public IDbConnection Connection
        {
            get => connectionObjet.DatabaseConnection;
        }

        /// <summary>
        /// The database's generated transaction
        /// </summary>
        public IDbTransaction Transaction
        {
            get => _transaction;
        }

        #endregion

        #region Class Constructor

        /// <summary>
        /// Class Constructor
        /// </summary>
        /// <param name="databaseConnectionObjet">contains the database connection object inizialized by the database class</param>
        public DatabaseTransactionObjet(DatabaseConnectionObjet databaseConnectionObjet)
        {
            connectionObjet = databaseConnectionObjet;
        }

        #endregion

        #region Class Methods

        /// <summary>
        /// Begim a new database's transaction
        /// </summary>
        public void BeginDatabaseTransaction()
        {
            connectionObjet.OpenDatabaseConnection();
            _transaction = connectionObjet.DatabaseConnection.BeginTransaction();
        }

        /// <summary>
        /// Commit all operation executed during a database's transaction
        /// </summary>
        public void CommitDatabaseTransaction() 
        {
            _transaction?.Commit();
            _transaction?.Dispose();
            _transaction = null;
            connectionObjet.CloseDatabaseConnection();
        }

        /// <summary>
        /// Abort all operation executed during a database's transaction
        /// </summary>
        public void RollbackDatabaseTransaction() 
        {
            _transaction?.Commit();
            _transaction?.Dispose();
            _transaction = null;
            connectionObjet.CloseDatabaseConnection();
        }

        #endregion
    }
}
