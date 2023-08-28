// System Class
using System.Data;
// Custom Class
using LothiumDB.Interfaces;

namespace LothiumDB.Core
{
    /// <summary>
    /// Database Connection Object
    /// </summary>
    internal class DatabaseConnectionObjet : IDatabaseConnectionObjet
    {
        private IDbProvider _provider;
        private IDbConnection? _connection = null;
        private string? _connectionString = null;

        #region Public Properties

        /// <summary>
        /// The opened database's connection
        /// </summary>
        /// <returns></returns>
        public IDbConnection DatabaseConnection
        {
            get => _connection;
        }

        /// <summary>
        /// Return true if the connection is open
        /// </summary>
        public bool IsOpen
        {
            get
            {
                // If the connection is null will return false  
                if (_connection == null)
                {
                    return false;
                }

                // Check the connection status
                if (_connection.State == ConnectionState.Open) return true;
                if (_connection.State == ConnectionState.Connecting) return true;
                if (_connection.State == ConnectionState.Executing) return true;
                if (_connection.State == ConnectionState.Fetching) return true;

                // By default it will return false
                return false;
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
                if (_connection == null)
                {
                    return false;
                }

                // Check the connection status
                if (_connection.State == ConnectionState.Closed) return true;
                if (_connection.State == ConnectionState.Broken) return true;

                // By default it will return false
                return false;
            }
        }

        #endregion

        #region Class Constructor

        /// <summary>
        /// Generate a new Connection's Object from a specific provider and a connection string's arguments
        /// </summary>
        /// <param name="provider">Contains the choosen database's provider</param>
        /// <param name="connectionStringArgs">Contains the connection string's arguments</param>
        public DatabaseConnectionObjet(IDbProvider provider, params object[] connectionStringArgs)
        {
            _provider = provider;
            _connectionString = _provider.CreateConnectionString(connectionStringArgs);
        }

        /// <summary>
        /// Generate a new Connection's Object from a specific provider and a connection string
        /// </summary>
        /// <param name="provider">Contains the choosen database's provider</param>
        /// <param name="connectionString">Contains the provider's connection string</param>
        public DatabaseConnectionObjet(IDbProvider provider, string connectionString)
        {
            _provider = provider;
            _connectionString = connectionString;
        }

        #endregion

        #region Class Methods

        /// <summary>
        /// Open the current generated connection
        /// </summary>
        public void OpenDatabaseConnection() 
        {
            if (String.IsNullOrEmpty(_connectionString)) throw new ArgumentNullException(nameof(_connectionString));
            _connection = _provider.CreateConnection(_connectionString);
            _connection.ConnectionString = _connectionString;
            _connection.Open();
        }

        /// <summary>
        /// Close the current generated connection
        /// </summary>
        public void CloseDatabaseConnection() 
        {
            _connection?.Close();
            _connection?.Dispose();
            _connection = null;
        }

        #endregion
    }
}
