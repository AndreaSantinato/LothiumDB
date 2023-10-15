// System Class
using System.Data;
// Custom Class
using LothiumDB.Providers;

namespace LothiumDB.Core
{
    /// <summary>
    /// Database Connection Object
    /// </summary>
    internal class DatabaseConnectionObject : IDatabaseConnectionObject
    {
        private string? _connectionString = null;

        #region Public Properties

        /// <summary>
        /// The opened database's connection
        /// </summary>
        /// <returns></returns>
        public IDatabaseProvider DatabaseProvider { get; }

        /// <summary>
        /// The opened database's connection
        /// </summary>
        /// <returns></returns>
        public IDbConnection? DatabaseConnection { get; private set; }

        /// <summary>
        /// Return true if the connection is open
        /// </summary>
        public bool IsOpen
        {
            get
            {
                // If the connection is null will return false  
                if (DatabaseConnection is null) return false;
                return DatabaseConnection.State switch
                {
                    // Check the connection status
                    ConnectionState.Open => true,
                    ConnectionState.Connecting => true,
                    ConnectionState.Executing => true,
                    ConnectionState.Fetching => true,
                    ConnectionState.Closed => false,
                    ConnectionState.Broken => true,
                    _ => false
                };
            }
        }

        /// <summary>
        /// Return true if the connection is closed
        /// </summary>
        public bool IsClosed
        {
            get
            {
                // If the connection is null will return false  
                if (DatabaseConnection is null) return false;
                return DatabaseConnection.State switch
                {
                    // Check the connection status
                    ConnectionState.Open => false,
                    ConnectionState.Connecting => false,
                    ConnectionState.Executing => false,
                    ConnectionState.Fetching => false,
                    ConnectionState.Closed => true,
                    ConnectionState.Broken => true,
                    _ => false
                };
            }
        }

        #endregion

        #region Class Constructor

        /// <summary>
        /// Generate a new Connection's Object from a specific provider and a connection string's arguments
        /// </summary>
        /// <param name="provider">Contains the choosen database's provider</param>
        /// <param name="connectionStringArgs">Contains the connection string's arguments</param>
        public DatabaseConnectionObject(IDatabaseProvider provider, params object[] connectionStringArgs)
        {
            DatabaseProvider = provider;
            _connectionString = DatabaseProvider.CreateConnectionString(connectionStringArgs);
        }

        /// <summary>
        /// Generate a new Connection's Object from a specific provider and a connection string
        /// </summary>
        /// <param name="provider">Contains the choosen database's provider</param>
        /// <param name="connectionString">Contains the provider's connection string</param>
        public DatabaseConnectionObject(IDatabaseProvider provider, string connectionString)
        {
            DatabaseProvider = provider;
            _connectionString = connectionString;
        }

        #endregion

        #region Class Methods
        
        /// <summary>
        /// Open the current generated connection
        /// </summary>
        /// <exception cref="ArgumentNullException">Contains an argument null exception</exception>
        public void OpenDatabaseConnection() 
        {
            if (string.IsNullOrEmpty(_connectionString)) throw new ArgumentNullException(nameof(_connectionString));
            DatabaseConnection = DatabaseProvider.CreateConnection(_connectionString);
            DatabaseConnection.ConnectionString = _connectionString;
            DatabaseConnection.Open();
        }

        /// <summary>
        /// Close the current generated connection
        /// </summary>
        public void CloseDatabaseConnection() 
        {
            DatabaseConnection?.Close();
            DatabaseConnection?.Dispose();
            DatabaseConnection = null;
        }

        /// <summary>
        /// Create a new transaction
        /// If the connection is not open will automatically open a new one
        /// </summary>
        /// <returns>A Database's transaction object</returns>
        /// <exception cref="ArgumentNullException">Contains an argument null exception for the connection object</exception>
        public IDbTransaction CreateNewTransaction()
        {
            if (DatabaseConnection is null) throw new ArgumentNullException(nameof(DatabaseConnection));
            if (!IsOpen) OpenDatabaseConnection();
            return DatabaseConnection.BeginTransaction();
        }

        #endregion
    }
}
