using System;
using System.Data;
using System.Data.Common;
using LothiumDB.Helpers;
using LothiumDB.Linq;
using LothiumDB.Exceptions;

namespace LothiumDB;

/// <summary>
/// Default Database Class of the library
/// Provides basic operations to the connected instance
/// </summary>
public class Database : IDatabase
{
    private bool _disposed = false;
    
    private readonly DatabaseProvider _provider;
    private IDbConnection _connection;
    private IDbTransaction? _transaction;
    private int _connectionDepth;
    private int _transactionDepth;
    private readonly int _commandTimeout;
    
    #region Properties

    /// <summary>
    /// Indicates if the current database instance connection is open
    /// </summary>
    public bool IsConnectionOpen
        => Utility.CheckConnectionStatus(_connection);
    
    /// <summary>
    /// Contains the complete last executed query with all parameters replace
    /// </summary>
    public string? LastSql { get; private set; }

    /// <summary>
    /// Contains the last database's generated error
    /// </summary>
    public Exception? LastError { get; private set; }

    /// <summary>
    /// Indicates if the connection must be kept open
    /// </summary>
    public bool KeepConnectionOpen { get; set; }

    #endregion

    #region Event Handlers
    
    /// <summary>
    /// Pre-Initialization of a database's command 
    /// </summary>
    public event EventHandler<DatabaseCommandEventArgs>? CommandExecuting;
    
    /// <summary>
    /// Post-Execution of a database's command
    /// </summary>
    public event EventHandler<DatabaseCommandEventArgs>? CommandCompleted;

    /// <summary>
    /// Equals to an Exception Throwing Error
    /// </summary>
    public event EventHandler<DatabaseExceptionEventArgs>? ExceptionRaised; 
    
    /// <summary>
    /// Performed before the execution of a database command operation
    /// and contains all the related information.
    /// </summary>
    /// <param name="operation">Contains the performed operation that raised the error</param>
    /// <param name="sql">Contains the sql that will be executed into the provided database's instance</param>
    /// <param name="parameters">Contains a set of parameters used by the sql</param>
    protected virtual void OnCommandExecution(DatabaseOperationTypesEnum operation, string sql, object[] parameters)
    {
        if (string.IsNullOrEmpty(sql))
            throw new ArgumentException("There is not a valid Sql query!");

        if (sql.Contains(_provider.GetVariablePrefix()) && parameters.Length <= 0)
            throw new ArgumentException("There are no provided parameters for the query!");
        
        CommandExecuting?.Invoke(
            this,
            new DatabaseCommandEventArgs(operation, sql, parameters)
        );
    }
    
    /// <summary>
    /// Performed after the execution of a database command operation
    /// and contains all the related information
    /// </summary>
    /// <param name="operation">Contains the performed operation that raised the error</param>
    /// <param name="sql">Contains the sql executed into the provided database's instance</param>
    /// <param name="parameters">Contains a set of parameters used by the sql</param>
    /// <returns>The executed query with parameters inside a formatted string</returns>
    protected virtual string OnCommandCompleted(DatabaseOperationTypesEnum operation,string sql, object[] parameters)
    {
        CommandCompleted?.Invoke(
            this, 
            new DatabaseCommandEventArgs(operation, sql, parameters)
        );
        
        return new SqlBuilder(sql, parameters).ToFormatQuery();
    }
    
    /// <summary>
    /// Contains an actual error generated during the execution of a provided sql
    /// </summary>
    /// <param name="operation">Contains the performed operation that raised the error</param>
    /// <param name="exception">Contains the generated error by the database's instance</param>
    /// <returns>The executed query with parameters inside a formatted string</returns>
    protected virtual Exception OnGeneratedError(DatabaseOperationTypesEnum operation, Exception exception)
    {
        ExceptionRaised?.Invoke(
            this,
            new DatabaseExceptionEventArgs(operation, exception)
        );
        
        return exception;
    }
    
    #endregion
    
    #region Constructors & Destructors

    /// <summary>
    /// Create a new Database instance object from a specific Provider configuration
    /// </summary>
    /// <param name="configuration">Contains a configuration that will be used to perform database operation inside a specific database's instance</param>
    public Database(DatabaseConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configuration.Connection);
        ArgumentException.ThrowIfNullOrEmpty(configuration.VariablePrefix);
        ArgumentNullException.ThrowIfNull(configuration.CommandTimeout);
        
        if (configuration.CommandTimeout <= 0)
            throw new ArgumentException("The command timeout must be greater than zero!");
        
        _provider = new DatabaseProvider(
            configuration.Type,
            (string)configuration.VariablePrefix
        );
        _connection = configuration.Connection;
        _connectionDepth = 0;
        _transaction = null;
        _transactionDepth = 0;
        _commandTimeout = (int)configuration.CommandTimeout;
        
        LastError = null;
        LastSql = string.Empty;
    }

    /// <summary>
    /// Dispose the Database Instance Previously Created
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _connection.Dispose();
            _transaction?.Dispose();
            _transaction = null;   
        }
        
        _disposed = true;
        
        GC.SuppressFinalize(this);
    }
    
    #endregion Constructors & Destructors
    
    #region Connection & Transaction Sync Methods
    
    /// <summary>
    /// Open a new connection to the chosen database's instance
    /// </summary>
    public void OpenConnection()
    {
        if (_connectionDepth == 0)
        {
            if (_transaction is not null) return;
            if (Utility.CheckConnectionStatus(_connection)) return;
            
            if (_connection.State == ConnectionState.Broken)
                _connection.Close();

            if (_connection.State == ConnectionState.Closed)
                _connection.Open();

            if (KeepConnectionOpen)
                _connectionDepth++;
        }

        _connectionDepth++;
    }

    /// <summary>
    /// Close an existing opened connection to the chosen database's instance
    /// </summary>
    public void CloseConnection()
    {
        if (_connectionDepth > 0)
            _connectionDepth--;

        if (_connectionDepth != 0) return;
        if (_transaction is not null) return;
        if (!Utility.CheckConnectionStatus(_connection)) return;
            
        _connection.Close();
    }
    
    /// <summary>
    /// Start a new transaction for an open connection for the selected provider
    /// if the connection is not set or open will return an argument null exception
    /// </summary>
    public void BeginTransaction()
        => BeginTransaction(IsolationLevel.Unspecified);
    
    /// <summary>
    /// Start a new transaction using a specific isolation level for an open connection for the selected provider
    /// if the connection is not set or open will return an argument null exception
    /// </summary>
    public void BeginTransaction(IsolationLevel isolationLevel)
    {
        if (_transactionDepth == 0)
        {
            if (_transaction is not null)
                throw new DatabaseException("There is an existing open transaction!!");
            
            OpenConnection();
            
            _transaction = _connection.BeginTransaction(isolationLevel);
        }
        
        _transactionDepth++;
    }
    
    private void SafeCloseAndCleanUpTransaction(bool rollback)
    {
        if (_transactionDepth != 1) 
            return;
        
        if (rollback)
            _transaction?.Rollback();
        else
            _transaction?.Commit();

        _transaction?.Dispose();
        _transaction = null;
        
        CloseConnection();
            
        _transactionDepth--;
    }
    
    /// <summary>
    /// Revert all the operations executed during the active database's transaction for the open connection for the selected provider
    /// If there is any open transaction it will simply exit the method
    /// </summary>
    public void RollbackTransaction()
        => SafeCloseAndCleanUpTransaction(true);
    
    /// <summary>
    /// Close the active database's transaction for the open connection for the selected provider
    /// If there is any open transaction it will simply exit the method
    /// </summary>
    public void CommitTransaction()
        => SafeCloseAndCleanUpTransaction(false);
    
    #endregion
    #region Connection & Transaction Async Methods
    
    /// <summary>
    /// Open a new asynchronous connection to the chosen database's instance
    /// </summary>
    public async Task OpenConnectionAsync()
        => await OpenConnectionAsync(CancellationToken.None);
    
    /// <summary>
    /// Open a new asynchronous connection to the chosen database's instance
    /// </summary>
    public async Task OpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connectionDepth == 0)
        {
            if (_transaction is not null) return;
            if (Utility.CheckConnectionStatus(_connection)) return;
            
            if (_connection.State == ConnectionState.Broken)
                _connection.Close();

            if (_connection.State == ConnectionState.Closed)
            {
                try
                {
                    var conn = (DbConnection)_connection;
                    
                    await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    _connection.Open();
                }
            }

            if (KeepConnectionOpen)
                _connectionDepth++;
        }

        _connectionDepth++;
    }

    /// <summary>
    /// Close an existing asynchronous opened connection to the chosen database's instance
    /// </summary>
    public async Task CloseConnectionAsync()
    {
        if (_connectionDepth > 0)
            _connectionDepth--;

        if (_connectionDepth != 0) return;
        if (_transaction is not null) return;
        if (!Utility.CheckConnectionStatus(_connection)) return;

        try
        {
            var conn = (DbConnection)_connection;
                    
            await conn.CloseAsync().ConfigureAwait(false);
        }
        catch
        {
            _connection.Close();
        }
    }
    
    /// <summary>
    /// Start a new asynchronous transaction using a specific isolation level for an open connection for the selected provider
    /// if the connection is not set or open will return an argument null exception
    /// </summary>
    public async Task BeginTransactionAsync()
        => await BeginTransactionAsync(CancellationToken.None, IsolationLevel.Unspecified);
    
    /// <summary>
    /// Start a new asynchronous transaction using a specific isolation level for an open connection for the selected provider
    /// if the connection is not set or open will return an argument null exception
    /// </summary>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
        => await BeginTransactionAsync(cancellationToken, IsolationLevel.Unspecified);
    
    /// <summary>
    /// Start a new asynchronous transaction using a specific isolation level for an open connection for the selected provider
    /// if the connection is not set or open will return an argument null exception
    /// </summary>
    public async Task BeginTransactionAsync(IsolationLevel isolationLevel)
        => await BeginTransactionAsync(CancellationToken.None, isolationLevel);
    
    /// <summary>
    /// Start a new asynchronous transaction using a specific isolation level for an open connection for the selected provider
    /// if the connection is not set or open will return an argument null exception
    /// </summary>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken, IsolationLevel isolationLevel)
    {
        if (_transactionDepth == 0)
        {
            if (_transaction is not null)
                throw new DatabaseException("There is an existing open transaction!!");
            
            await OpenConnectionAsync(cancellationToken);

            try
            {
                var conn = (DbConnection)_connection;
                
                await conn.BeginTransactionAsync(isolationLevel, cancellationToken);
            }
            catch
            {
                _transaction = _connection.BeginTransaction(isolationLevel);
            }
            
            _transactionDepth++;
        }
    }
    
    /// <summary>
    /// Revert all the operations executed during the active asynchronous database's transaction for the open connection for the selected provider
    /// If there is any open transaction it will simply exit the method
    /// </summary>
    public async Task RollbackTransactionAsync()
        => await Task.Run(() => SafeCloseAndCleanUpTransaction(true));
    
    /// <summary>
    /// Close the active asynchronous database's transaction for the open connection for the selected provider
    /// If there is any open transaction it will simply exit the method
    /// </summary>
    public async Task CommitTransactionAsync()
        => await Task.Run(() => SafeCloseAndCleanUpTransaction(false));
    
    #endregion Connection & Transaction Async Methods
    
    #region  Scalar & ScalarAsync
    
    private object? InternalScalarOperation<T>(DatabaseOperationTypesEnum operationType, string sql, object[] args)
    {
        object? result = null;

        try
        {
            OpenConnection();

            try
            {
                OnCommandExecution(operationType, sql, args);
            
                using var cmd = Utility.CreateDatabaseCommand(
                    _provider,
                    _connection,
                    _transaction,
                    CommandType.Text,
                    _commandTimeout,
                    sql,
                    args
                );
                ArgumentNullException.ThrowIfNull(cmd, nameof(cmd));

                result = CommandManager.PerformScalarCommand<T>(cmd);
            }
            finally
            {
                LastSql = OnCommandCompleted(operationType, sql, args);
                
                CloseConnection();
            }
        }
        catch (Exception ex)
        {
            LastError = OnGeneratedError(operationType, ex);
            
            result = default;
        }

        return (T?)result;
    }
    private async Task<object?> InternalScalarOperationAsync<T>(DatabaseOperationTypesEnum operationType, string sql, object[] args, CancellationToken cancellationToken)
    {
        object? result = null;

        try
        {
            await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                OnCommandExecution(operationType, sql, args);
            
                using var cmd = Utility.CreateDatabaseCommand(
                    _provider,
                    _connection,
                    _transaction,
                    CommandType.Text,
                    _commandTimeout,
                    sql,
                    args
                );
                ArgumentNullException.ThrowIfNull(cmd, nameof(cmd));

                result = await CommandManager.PerformScalarCommandAsync<T>((DbCommand)cmd, cancellationToken);
            }
            finally
            {
                LastSql = OnCommandCompleted(operationType, sql, args);
                
                await CloseConnectionAsync();
            }
        }
        catch (Exception ex)
        {
            LastError = OnGeneratedError(operationType, ex);
            
            result = default;
        }

        return (T?)result;
    }
    
    /// <summary>
    /// Invoke the DB Scalar command in the Database Instance and return a single value of a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public object? Scalar<T>(string sql, params object[] args)
        => InternalScalarOperation<T>(DatabaseOperationTypesEnum.Scalar, sql, args);

    /// <summary>
    /// Invoke the DB Scalar command in the Database Instance and return a single value of a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>A value based of the object type</returns>
    public object? Scalar<T>(SqlBuilder sql) 
        => InternalScalarOperation<T>(DatabaseOperationTypesEnum.Scalar, sql.Query, sql.Params);

    /// <summary>
    /// Invoke the DB Scalar command in the Database Instance and return a single value of a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public async Task<object?> ScalarAsync<T>(string sql, params object[] args)
        => await ScalarAsync<T>(sql, CancellationToken.None, args);

    /// <summary>
    /// Invoke the DB Scalar command in the Database Instance and return a single value of a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="cancellationToken">Contains a token that will be used to cancel the operation</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public async Task<object?> ScalarAsync<T>(string sql, CancellationToken cancellationToken, params object[] args)
        => await InternalScalarOperationAsync<T>(DatabaseOperationTypesEnum.ScalarAsync, sql, args, cancellationToken).ConfigureAwait(false);
    
    /// <summary>
    /// Invoke the DB Scalar command in the Database Instance and return a single value of a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>A value based of the object type</returns>
    public async Task<object?> ScalarAsync<T>(SqlBuilder sql) 
        => await ScalarAsync<T>(sql, CancellationToken.None);
    
    /// <summary>
    /// Invoke the DB Scalar command in the Database Instance and return a single value of a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the SQL object</param>
    /// <param name="cancellationToken">Contains a token that will be used to cancel the operation</param>
    /// <returns>A value based of the object type</returns>
    public async Task<object?> ScalarAsync<T>(SqlBuilder sql, CancellationToken cancellationToken) 
        => await InternalScalarOperationAsync<T>(DatabaseOperationTypesEnum.ScalarAsync, sql.Query, sql.Params, cancellationToken).ConfigureAwait(false);
    
    #endregion  ScalarAsync Commands

    #region Execute & ExecuteAsync

    private int InternalExecuteOperation(DatabaseOperationTypesEnum operationType, string sql, object[] args)
    {
        var affectedRowOnCommand = 0;

        try
        {
            OpenConnection();

            try
            {
                OnCommandExecution(operationType, sql, args);

                using var cmd = Utility.CreateDatabaseCommand(
                    _provider,
                    _connection,
                    _transaction,
                    CommandType.Text,
                    _commandTimeout,
                    sql,
                    args
                );
            
                affectedRowOnCommand = CommandManager.PerformExecuteCommand(cmd);
            }
            finally
            {
                LastSql = OnCommandCompleted(operationType, sql, args);
            
                CloseConnection();
            }   
        }
        catch (Exception ex)
        {
            LastError = OnGeneratedError(operationType, ex);
            
            affectedRowOnCommand = -1;
        }

        return affectedRowOnCommand;
    }
    private async Task<int> InternalExecuteOperationAsync(DatabaseOperationTypesEnum operationType, string sql, object[] args, CancellationToken cancellationToken)
    {
        var affectedRowOnCommand = 0;

        try
        {
            await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                OnCommandExecution(operationType, sql, args);

                using var cmd = Utility.CreateDatabaseCommand(
                    _provider,
                    _connection,
                    _transaction,
                    CommandType.Text,
                    _commandTimeout,
                    sql,
                    args
                );

                affectedRowOnCommand = await CommandManager.PerformExecuteCommandAsync((DbCommand)cmd, cancellationToken);
            }
            finally
            {
                LastSql = OnCommandCompleted(operationType, sql, args);
            
                await CloseConnectionAsync();
            }   
        }
        catch (Exception ex)
        {
            LastError = OnGeneratedError(operationType, ex);
            
            affectedRowOnCommand = -1;
        }

        return affectedRowOnCommand;
    }
    
    /// <summary>
    /// Invoke the DB NonQuery command in the Database Instance and return the number of completed operations
    /// </summary>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>An int value that count all the affected table rows</returns>
    public int Execute(string sql, params object[] args)
        => InternalExecuteOperation(DatabaseOperationTypesEnum.ExecuteQuery, sql, args);

    /// <summary>
    /// Invoke the DB NonQuery command in the Database Instance and return the number of completed operations
    /// </summary>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>An int value that count all the affected table rows</returns>
    public int Execute(SqlBuilder sql)
        => InternalExecuteOperation(DatabaseOperationTypesEnum.ExecuteQuery, sql.Query, sql.Params);

    /// <summary>
    /// Invoke the DB NonQuery command in the Database Instance and return the number of completed operations
    /// </summary>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>An int value that count all the affected table rows</returns>
    public async Task<int> ExecuteAsync(string sql, params object[] args)
        => await ExecuteAsync(sql, CancellationToken.None, args);

    /// <summary>
    /// Invoke the DB NonQuery command in the Database Instance and return the number of completed operations
    /// </summary>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="cancellationToken">Contains a token to cancel the asynchronous calling</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>An int value that count all the affected table rows</returns>
    public async Task<int> ExecuteAsync(string sql, CancellationToken cancellationToken, params object[] args)
        => await InternalExecuteOperationAsync(DatabaseOperationTypesEnum.ExecuteQueryAsync, sql, args, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Invoke the DB NonQuery command in the Database Instance and return the number of completed operations
    /// </summary>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>An int value that count all the affected table rows</returns>
    public async Task<int> ExecuteAsync(SqlBuilder sql)
        => await ExecuteAsync(sql, CancellationToken.None);
    
    /// <summary>
    /// Invoke the DB NonQuery command in the Database Instance and return the number of completed operations
    /// </summary>
    /// <param name="cancellationToken">Contains a token to cancel the asynchronous calling</param>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>An int value that count all the affected table rows</returns>
    public async Task<int> ExecuteAsync(SqlBuilder sql, CancellationToken cancellationToken)
        => await InternalExecuteOperationAsync(DatabaseOperationTypesEnum.ExecuteQueryAsync, sql.Query, sql.Params, cancellationToken).ConfigureAwait(false);
    
    #endregion ExecuteAsync Commands
    
    #region Query/QueryAsync & StoredProcedure/StoredProcedureAsync
    
    private IEnumerable<T> InternalQueryOperation<T>(DatabaseOperationTypesEnum operationType, string sql, object[] args)
    {
        IEnumerable<T> result;

        try
        {
            OpenConnection();

            try
            {
                var cmdType = (
                    operationType.Equals(DatabaseOperationTypesEnum.StoredProcedure) || 
                    operationType.Equals(DatabaseOperationTypesEnum.StoredProcedureAsync)
                ) ? CommandType.StoredProcedure : CommandType.Text;

                using var cmd = Utility.CreateDatabaseCommand(
                    _provider,
                    _connection,
                    _transaction,
                    cmdType,
                    _commandTimeout,
                    sql,
                    args
                );

                result = (List<T>)CommandManager.PerformQueryCommand<T>(cmd);
            }
            finally
            {
                LastSql = OnCommandCompleted(operationType, sql, args);
            
                CloseConnection();
            }
            
            OnCommandExecution(operationType, sql, args);
        }
        catch (Exception ex)
        {
            LastError = OnGeneratedError(operationType, ex);
            
            result = [];
        }

        return result;
    }
    private async Task<IEnumerable<T>> InternalQueryOperationAsync<T>(DatabaseOperationTypesEnum operationType, string sql, object[] args, CancellationToken cancellationToken)
    {
        IEnumerable<T> result;

        try
        {
            await OpenConnectionAsync(cancellationToken);

            try
            {
                var cmdType = (
                    operationType.Equals(DatabaseOperationTypesEnum.StoredProcedure) || 
                    operationType.Equals(DatabaseOperationTypesEnum.StoredProcedureAsync)
                ) ? CommandType.StoredProcedure : CommandType.Text;

                using var cmd = Utility.CreateDatabaseCommand(
                    _provider,
                    _connection,
                    _transaction,
                    cmdType,
                    _commandTimeout,
                    sql,
                    args
                );

                result = (List<T>)await CommandManager.PerformQueryCommandAsync<T>((DbCommand)cmd, cancellationToken);
            }
            finally
            {
                LastSql = OnCommandCompleted(operationType, sql, args);
            
                await CloseConnectionAsync();
            }
            
            OnCommandExecution(operationType, sql, args);
        }
        catch (Exception ex)
        {
            LastError = OnGeneratedError(operationType, ex);
            
            result = [];
        }

        return result;
    }
    
    /// <summary>
    /// Invoke the DB Query command in the Database Instance and cast it to a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public IEnumerable<T> Query<T>(string sql, params object[] args)
        => InternalQueryOperation<T>(DatabaseOperationTypesEnum.Query, sql, args);

    /// <summary>
    /// Invoke the DB Query command in the Database Instance and cast it to a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the SQL builder</param>
    /// <returns>A value based of the object type</returns>
    public IEnumerable<T> Query<T>(SqlBuilder sql) 
        => InternalQueryOperation<T>(DatabaseOperationTypesEnum.Query, sql.Query, sql.Params);

    /// <summary>
    /// Invoke the DB Query command in the Database Instance after the execution of a stored procedure
    /// and cast it to a specific object type similar to the Query command
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="name">Contains the unique name of the procedure</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public IEnumerable<T> StoredProcedure<T>(string name, params object[] args)
        => StoredProcedure<T>(name, "dbo", args);
    
    /// <summary>
    /// Invoke the DB Query command in the Database Instance after the execution of a stored procedure
    /// and cast it to a specific object type similar to the Query command
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="name">Contains the unique name of the procedure</param>
    /// <param name="schema">Contains a specific schema associated to the procedure</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public IEnumerable<T> StoredProcedure<T>(string name, string schema, params object[] args)
        => InternalQueryOperation<T>(DatabaseOperationTypesEnum.StoredProcedure, $"{schema}.{name}", args);

    /// <summary>
    /// Invoke the DB Query command in the Database Instance after the execution of a stored procedure
    /// and cast it to a specific object type similar to the Query command
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="procedure">Contains the Stored Procedure builder</param>
    /// <returns>A value based of the object type</returns>
    public IEnumerable<T> StoredProcedure<T>(StoredProcedureBuilder procedure)
        => InternalQueryOperation<T>(DatabaseOperationTypesEnum.StoredProcedure, procedure.Procedure.Item1, procedure.Procedure.Item2);
    
    /// <summary>
    /// Invoke the DB Query command in the Database Instance and cast it to a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, params object[] args)
        => await QueryAsync<T>(sql, CancellationToken.None, args);

    /// <summary>
    /// Invoke the DB Query command in the Database Instance and cast it to a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="cancellationToken">Contains a token to cancel the current operation</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, CancellationToken cancellationToken, params object[] args)
        => await InternalQueryOperationAsync<T>(DatabaseOperationTypesEnum.Query, sql, args, cancellationToken);
    
    /// <summary>
    /// Invoke the DB Query command in the Database Instance and cast it to a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>A value based of the object type</returns>
    public async Task<IEnumerable<T>> QueryAsync<T>(SqlBuilder sql) 
        => await QueryAsync<T>(sql, CancellationToken.None);
    
    /// <summary>
    /// Invoke the DB Query command in the Database Instance and cast it to a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the SQL object</param>
    /// /// <param name="cancellationToken">Contains a token to cancel the current operation</param>
    /// <returns>A value based of the object type</returns>
    public async Task<IEnumerable<T>> QueryAsync<T>(SqlBuilder sql, CancellationToken cancellationToken) 
        => await InternalQueryOperationAsync<T>(DatabaseOperationTypesEnum.Query, sql.Query, sql.Params, cancellationToken);

    /// <summary>
    /// Invoke the DB Query command in the Database Instance after the execution of a stored procedure
    /// and cast it to a specific object type similar to the Query command
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="name">Contains the unique name of the procedure</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public async Task<IEnumerable<T>> StoredProcedureAsync<T>(string name, params object[] args)
        => await StoredProcedureAsync<T>(name, "dbo", CancellationToken.None, args);
    
    /// <summary>
    /// Invoke the DB Query command in the Database Instance after the execution of a stored procedure
    /// and cast it to a specific object type similar to the Query command
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="name">Contains the unique name of the procedure</param>
    /// <param name="cancellationToken">Contains a token to cancel the current operation</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public async Task<IEnumerable<T>> StoredProcedureAsync<T>(string name, CancellationToken cancellationToken, params object[] args)
        => await StoredProcedureAsync<T>(name, "dbo", cancellationToken, args);
    
    /// <summary>
    /// Invoke the DB Query command in the Database Instance after the execution of a stored procedure
    /// and cast it to a specific object type similar to the Query command
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="name">Contains the unique name of the procedure</param>
    /// <param name="schema">Contains a specific schema associated to the procedure</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public async Task<IEnumerable<T>> StoredProcedureAsync<T>(string name, string schema, params object[] args)
        => await StoredProcedureAsync<T>(name, schema, CancellationToken.None, args);
    
    /// <summary>
    /// Invoke the DB Query command in the Database Instance after the execution of a stored procedure
    /// and cast it to a specific object type similar to the Query command
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="name">Contains the unique name of the procedure</param>
    /// <param name="schema">Contains a specific schema associated to the procedure</param>
    /// <param name="cancellationToken">Contains a token to cancel the current operation</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public async Task<IEnumerable<T>> StoredProcedureAsync<T>(string name, string schema, CancellationToken cancellationToken, params object[] args)
        => await InternalQueryOperationAsync<T>(DatabaseOperationTypesEnum.StoredProcedureAsync, $"{schema}.{name}", args, cancellationToken);
    
    /// <summary>
    /// Invoke the DB Query command in the Database Instance after the execution of a stored procedure
    /// and cast it to a specific object type similar to the Query command
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="procedure">Contains the Stored Procedure builder</param>
    /// <returns>A value based of the object type</returns>
    public async Task<IEnumerable<T>> StoredProcedureAsync<T>(StoredProcedureBuilder procedure)
        => await StoredProcedureAsync<T>(procedure, CancellationToken.None);
    
    /// <summary>
    /// Invoke the DB Query command in the Database Instance after the execution of a stored procedure
    /// and cast it to a specific object type similar to the Query command
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="procedure">Contains the Stored Procedure builder</param>
    /// <param name="cancellationToken">Contains a token to cancel the current operation</param>
    /// <returns>A value based of the object type</returns>
    public async Task<IEnumerable<T>> StoredProcedureAsync<T>(StoredProcedureBuilder procedure, CancellationToken cancellationToken)
        => await InternalQueryOperationAsync<T>(DatabaseOperationTypesEnum.StoredProcedure, procedure.Procedure.Item1, procedure.Procedure.Item2, cancellationToken);
    
    #endregion

    #region FindAll Command

    /// <summary>
    /// Select all the elements inside a table without specify the Sql query
    /// </summary>
    /// <returns>A value based of the object type</returns>
    public List<T>? FindAll<T>()
        => FindAll<T>(AutoMapper.AutoSelectClause<T>());
    
    /// <summary>
    /// Select all the elements inside a table with a specify Sql query
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>A value based of the object type</returns>
    public List<T>? FindAll<T>(SqlBuilder sql)
        => Query<T>(sql)?.ToList();
    
    /// <summary>
    /// Select all the elements inside a table with a specify Sql query
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="procedure">Contains the Stored Procedure builder</param>
    /// <returns>A value based of the object type</returns>
    public List<T>? FindAll<T>(StoredProcedureBuilder procedure)
        => StoredProcedure<T>(procedure)?.ToList();

    #endregion

    #region FindSingle Command

    /// <summary>
    /// Select a single specific element inside a table
    /// </summary>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>A value based of the object type</returns>
    public T? FindSingle<T>(SqlBuilder sql)
    {
        var result = Query<T>(sql);

        return (result is null)
            ? default
            : result.ToList().FirstElement();
    }

    /// <summary>
    /// Select a single specific element inside a table
    /// </summary>
    /// <param name="procedure">Contains the Stored Procedure builder</param>
    /// <returns>A value based of the object type</returns>
    public T? FindSingle<T>(StoredProcedureBuilder procedure)
    {
        var result = StoredProcedure<T>(procedure);

        return (result is null)
            ? default
            : result.ToList().FirstElement();
    }
    
    #endregion

    #region FetchPage

    /// <summary>
    /// Generate a Paging List from a PageObject
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="page">Contains the page object</param>
    /// <returns>A value based of the object type</returns>
    public List<T> FetchPage<T>(PageObject<T> page)
    {
        return FetchPage<T>(
            page,
            AutoMapper
                .AutoSelectClause<T>()
                .Where("WHERE 1=1")
        );
    }
    
    /// <summary>
    /// Generate a Paging List from a PageObject
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="page">Contains the page object</param>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>A value based of the object type</returns>
    public List<T> FetchPage<T>(PageObject<T> page, SqlBuilder sql)
    {
        var pageSql = _provider.BuildPageQuery<T>(page, sql);

        return (List<T>)(string.IsNullOrEmpty(pageSql.Query)
            ? Enumerable.Empty<T>()
            : Query<T>(pageSql).ToList()
        );
    
    }

    #endregion

    #region Insert, Update, Save, Delete, Exist Methods

    /// <summary>
    /// Insert the passed object inside a table of the database in the form of a row
    /// </summary>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <typeparam name="T">Contains the type of the objects to be insert inside the database</typeparam>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Insert<T>(object obj)
        => Execute(AutoMapper.AutoInsertClause<T>(obj));

    /// <summary>
    /// Insert the passed list of objects inside a table of the database in the form of a row
    /// </summary>
    /// <param name="objs">Contains the list of the objects with the db table's mapping</param>
    /// <typeparam name="T">Contains the type of the objects to be insert inside the database</typeparam>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Insert<T>(List<T> objs)
    {
        var affectedRows = 0;
        objs.ForEach(x =>
        {
            if (x is not null)
                affectedRows += (int)Insert<T>(x);
        });
        return affectedRows;
    }

    /// <summary>
    /// Update a number of element inside a table of the database
    /// </summary>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <typeparam name="T">Contains the type of the objects to be insert inside the database</typeparam>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Update<T>(object obj)
        => Execute(AutoMapper.AutoUpdateClause<T>(obj));

    /// <summary>
    /// Update the passed list of elements inside a table of the database
    /// </summary>
    /// <param name="objs">Contains the list of the objects with the db table's mapping</param>
    /// <typeparam name="T">Contains the type of the objects to be updated inside the database</typeparam>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Update<T>(List<T> objs)
    {
        var affectedRows = 0;
        objs.ForEach(x =>
        {
            if (x is not null)
                affectedRows += (int)Update<T>(x);
        });
        return affectedRows;
    }

    /// <summary>
    /// Delete a number of element inside a table of the database
    /// </summary>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Delete<T>(object obj)
        => Execute(AutoMapper.AutoDeleteClause<T>(obj));

    /// <summary>
    /// Delete the passed list of elements inside a table of the database
    /// </summary>
    /// <param name="objs">Contains the list of the objects with the db table's mapping</param>
    /// <typeparam name="T">Contains the type of the objects to be deleted from the database</typeparam>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Delete<T>(List<T> objs)
    {
        var affectedRows = 0;
        objs.ForEach(x =>
        {
            if (x is not null)
                affectedRows += (int)Update<T>(x);
        });
        return affectedRows;
    }

    /// <summary>
    /// If an object already exist inside the database will update it, otherwise will create it
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Save<T>(object obj)
        => Exist<T>(obj) ? Update<T>(obj) : Insert<T>(obj);

    /// <summary>
    /// Search inside the database's if a record exist
    /// </summary>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns></returns>
    public bool Exist(SqlBuilder sql)
        => Convert.ToBoolean(Scalar<int>(sql));

    /// <summary>
    /// Search inside the database's if a record exist
    /// </summary>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns></returns>
    public bool Exist(string sql, params object[] args)
        => Exist(new SqlBuilder(sql, args));

    /// <summary>
    /// Search inside the database's if a record exist
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <returns></returns>
    public bool Exist<T>(object obj)
        => Exist(AutoMapper.AutoExistClause<T>(obj));

    #endregion
}