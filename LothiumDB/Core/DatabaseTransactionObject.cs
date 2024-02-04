using System.Data;
using LothiumDB.Core.Interfaces;

namespace LothiumDB.Core;

/// <summary>
/// Database Transaction Object
/// </summary>
internal class DatabaseTransactionObject : IDatabaseTransactionObject
{
    /// <summary>
    /// The database's connection used for the transaction object
    /// </summary>
    public IDbConnection Connection { get; private set; }

    /// <summary>
    /// The database's generated transaction
    /// </summary>
    public IDbTransaction? Transaction { get; private set; } = null;

    /// <summary>
    /// Class Constructor
    /// </summary>
    /// <param name="connection">contains the database connection passed by the database class</param>
    public DatabaseTransactionObject(IDbConnection connection)
    {
        Connection = connection;
        Transaction = null;
    }

    #region Class Methods

    /// <summary>
    /// Begin a new database's transaction
    /// </summary>
    public void BeginDatabaseTransaction()
    {
        Connection.Open();
        Transaction = Connection.BeginTransaction();
    }

    /// <summary>
    /// Commit all operation executed during a database's transaction
    /// </summary>
    public void CommitDatabaseTransaction()
    {
        Transaction?.Commit();
        Transaction?.Dispose();
        Transaction = null;
        Connection.Close();
    }

    /// <summary>
    /// Abort all operation executed during a database's transaction
    /// </summary>
    public void RollbackDatabaseTransaction()
    {
        Transaction?.Commit();
        Transaction?.Dispose();
        Transaction = null;
        Connection.Close();
    }

    #endregion
}