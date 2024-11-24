using System;
using System.Data;

namespace LothiumDB;

/// <summary>
/// Define the core connection methods to manage a database's connection instance
/// </summary>
public interface IDatabaseConnection
{
    /// <summary>
    /// Open a new connection in a provided instance
    /// </summary>
    void OpenConnection();
    
    /// <summary>
    /// Close a previously opened connection in a provided instance
    /// </summary>
    void CloseConnection();

#if ASYNC

    /// <summary>
    /// Open a new asynchronous connection in a provided instance
    /// </summary>
    Task OpenConnectionAsync();

    /// <summary>
    /// Open a new asynchronous connection in a provided instance
    /// </summary>
    Task OpenConnectionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Close a previously opened asynchronous connection in a provided instance
    /// </summary>
    Task CloseConnectionAsync();
    
#endif
}

/// <summary>
/// Define the core transaction methods to manage a database's transaction
/// to an existing database's connection instance
/// </summary>
public interface IDatabaseTransaction
{
    /// <summary>
    /// Create and start a new transaction inside a provided instance
    /// </summary>
    void BeginTransaction();

    /// <summary>
    /// Create and start a new transaction inside a provided instance
    /// </summary>
    /// <param name="isolationLevel">Define an isolation level to use during the execution of the transaction</param>
    public void BeginTransaction(IsolationLevel isolationLevel);
    
    /// <summary>
    /// Revert all the operation performed during the transaction close the active transaction
    /// </summary>
    void RollbackTransaction();
    
    /// <summary>
    /// Confirm all the operation performed during the transaction close the active transaction
    /// </summary>
    void CommitTransaction();

#if ASYNC
    
    /// <summary>
    /// Create and start a new asynchronous transaction inside a provided instance
    /// </summary>
    Task BeginTransactionAsync();
    
    /// <summary>
    /// Create and start a new asynchronous transaction inside a provided instance
    /// </summary>
    /// <param name="cancellationToken">Define a token to use if the async caller must be killed</param>
    Task BeginTransactionAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Create and start a new asynchronous transaction inside a provided instance
    /// </summary>
    /// <param name="isolationLevel">Define an isolation level to use during the execution of the transaction</param>
    Task BeginTransactionAsync(IsolationLevel isolationLevel);
    
    /// <summary>
    /// Create and start a new asynchronous transaction inside a provided instance
    /// </summary>
    /// <param name="cancellationToken">Define a token to use if the async caller must be killed</param>
    /// <param name="isolationLevel">Define an isolation level to use during the execution of the transaction</param>
    Task BeginTransactionAsync(CancellationToken cancellationToken, IsolationLevel isolationLevel);
    
    /// <summary>
    /// Revert all the operation performed during the asynchronous transaction close the active transaction
    /// </summary>
    Task RollbackTransactionAsync();
    
    /// <summary>
    /// Confirm all the operation performed during the asynchronous transaction close the active transaction
    /// </summary>
    Task CommitTransactionAsync();

#endif
}

/// <summary>
/// Define the core commands methods to manage all the primary operation
/// to an existing database's connection instance
/// </summary>
public interface IDatabaseCommands
{
    object? Scalar<T>(string sql, object[] args);

    int Execute(string sql, object[] args);
    
    IEnumerable<T>? Query<T>(string sql, object[] args);
    
#if ASYNC
    
    Task<object?>? ScalarAsync<T>(string sql, object[] args);

    Task<int> ExecuteAsync(string sql, object[] args);
    
    Task<IEnumerable<T>?>? QueryAsync<T>(string sql, object[] args);
    
#endif
}

/// <summary>
/// Define a set of extended commands methods to manage additional operation
/// to an existing database's connection instance
/// </summary>
public interface IDatabaseExtendedCommands : IDatabaseCommands
{
    /// <summary>
    /// Select all the elements inside a table without specify the Sql query
    /// </summary>
    /// <returns>A value based of the object type</returns>
    public List<T>? FindAll<T>();

    /// <summary>
    /// Select all the elements inside a table using a specific Sql query
    /// </summary>
    /// <returns>A value based of the object type</returns>
    public List<T>? FindAll<T>(SqlBuilder sql);

    /// <summary>
    /// Select a single specific element inside a table
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <returns>A value based of the object type</returns>
    /// <returns></returns>
    public T? FindSingle<T>(SqlBuilder sql);

    /// <summary>
    /// Search inside the database's if a record exist
    /// </summary>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns></returns>
    public bool Exist(SqlBuilder sql);

    /// <summary>
    /// Search inside the database's if a record exist
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <returns></returns>
    public bool Exist<T>(object obj);

    /// <summary>
    /// Insert the passed object inside a table of the database in the form of a row
    /// </summary>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Insert<T>(object obj);

    /// <summary>
    /// Update a number of element inside a table of the database
    /// </summary>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Update<T>(object obj);

    /// <summary>
    /// Delete a number of element inside a table of the database
    /// </summary>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Delete<T>(object obj);

    /// <summary>
    /// If an object already exist inside the database will update it, otherwise will cretae it
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Save<T>(object obj);
}

public interface IDatabase : IDisposable, IDatabaseConnection, IDatabaseTransaction, IDatabaseExtendedCommands
{ }