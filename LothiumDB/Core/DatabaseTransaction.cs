using System.Data;
using LothiumDB.Core.Interfaces;

namespace LothiumDB.Core;

/// <summary>
/// Database Transaction Object
/// </summary>
internal class DatabaseTransaction
{
    private IProvider _provider;
    private IDbConnection _connection;
    private IDbTransaction? _transaction;

    /// <summary>
    /// The database's connection used for the transaction object
    /// </summary>
    public IDbConnection Connection
    {
        get => _connection;
    }

    /// <summary>
    /// The database's generated transaction
    /// </summary>
    public IDbTransaction? Transaction 
    {
        get => _transaction;
    }

    /// <summary>
    /// Indicates if there is an opened usable transaction
    /// </summary>
    public bool Usable
    {
        get
        {
            if (_connection is null) return false;
            return (_transaction is null)
                ? false
                : true;
        }
    }

    /// <summary>
    /// Class Constructor
    /// </summary>
    /// <param name="connection">contains the database connection passed by the database class</param>
    public DatabaseTransaction(IProvider provider, IDbConnection connection)
    {
        _provider = provider;
        _connection = connection;
        _transaction = null;
    }

    #region Class Methods

    /// <summary>
    /// Begin a new database's transaction
    /// </summary>
    public void BeginTransaction()
    {
        DatabaseHelper.OpenSafeConnection(_provider, _connection);
        _transaction = Connection.BeginTransaction();
    }

    /// <summary>
    /// Commit all operation executed during a database's transaction
    /// </summary>
    public void CommitTransaction()
    {
        _transaction?.Commit();
        _transaction?.Dispose();
        _transaction = null;
        DatabaseHelper.CloseSafeConnection(_provider, _connection);
    }

    /// <summary>
    /// Abort all operation executed during a database's transaction
    /// </summary>
    public void RollbackTransaction()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
        DatabaseHelper.CloseSafeConnection(_provider, _connection);
    }

    #endregion
}