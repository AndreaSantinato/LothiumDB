using System;
using System.Linq;
using System.Data;
using LothiumDB.Core;
using LothiumDB.Core.Enumerations;
using LothiumDB.Core.Interfaces;
using LothiumDB.Linq;
using LothiumDB.Tools;
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
    private readonly IDbConnection _connection;
    private IDbTransaction? _transaction;
    private readonly int _commandTimeout;
    
    #region Properties

    /// <summary>
    /// Indicates if the current database instance connection is open
    /// </summary>
    public bool IsConnectionOpen
        => DatabaseHelper.CheckConnectionStatus(_connection);
    
    /// <summary>
    /// Contains the complete last executed query with all parameters replace
    /// </summary>
    public string? LastSql { get; private set; }

    /// <summary>
    /// Contains the last database's generated error
    /// </summary>
    public Exception? LastError { get; private set; }

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
        
        _provider = new DatabaseProvider(configuration.Type, (string)configuration.VariablePrefix);
        _connection = configuration.Connection;
        _transaction = null;
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

    #region Object Management & Core Operations

    private void SafeOpenConnection()
    {
        if (_transaction is not null) return;
        if (DatabaseHelper.CheckConnectionStatus(_connection)) return;
        
        _connection.Open();
    }
    
    private void SafeCloseConnection()
    {
        if (_transaction is not null) return;
        if (!DatabaseHelper.CheckConnectionStatus(_connection)) return;
        
        _connection.Close();
    }

    private void SafeOpenTransaction()
    {
        if (_transaction is not null)
            throw new DatabaseException("There is an existing open transaction!!");
        
        _transaction = _connection.BeginTransaction();
    }

    private void SafeCloseTransaction(bool rollback)
    {
        if (_transaction is null)
            throw new DatabaseException("There is no open transaction to close!");

        if (rollback)
            _transaction.Rollback();
        else
            _transaction.Commit();

        _transaction.Dispose();
        _transaction = null;
    }
    
    private IDbCommand SafeCreateCommand(
        string sql,
        object[] args,
        CommandType commandType
    )
    {
        // Check if the minimum required variables are correctly sets
        DatabaseException.ThrowIfNullOrEmpty(sql);
        if (sql.Contains('@') && args.Length.Equals(0))
            throw new DatabaseException("The provided SQL contains variables but the actual parameters were not provided!");

        // Create the new command
        var command = _connection.CreateCommand();
        
        command.Transaction = _transaction;
        command.CommandText = sql;
        command.CommandType = commandType;

        if (args.Length != 0)
        {
            DatabaseHelper.AddParamsToDatabaseCommand(
                _provider,
                ref command,
                new SqlBuilder(sql, args)
            );
        }

        DatabaseException.ThrowIfNull(command);

        return command;
    }

    private object? SafeScalarOperation<T>(DatabaseOperationTypesEnum operationType, string sql, object[] args)
    {
        object? result = null;

        try
        {
            SafeOpenConnection();
            
            OnCommandExecution(operationType, sql, args);

            using var cmd = SafeCreateCommand(sql, args, CommandType.Text);
            
            result = cmd.ExecuteScalar();
        }
        catch (Exception ex)
        {
            OnErrorOccured(operationType, ex);
            
            result = default;
        }
        finally
        {
            OnCommandExecuted(operationType, sql, args);
            
            SafeCloseConnection();
        }

        return result;
    }
    
    private int SafeExecuteOperation(DatabaseOperationTypesEnum operationType, string sql, params object[] args)
    {
        var affectedRowOnCommand = 0;

        try
        {
            SafeOpenConnection();
            
            OnCommandExecution(operationType, sql, args);

            using var cmd = SafeCreateCommand(sql, args, CommandType.Text);
            
            affectedRowOnCommand = (int)cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            OnErrorOccured(operationType, ex);
            
            affectedRowOnCommand = -1;
        }
        finally
        {
            OnCommandExecuted(operationType, sql, args);
            
            SafeCloseConnection();
        }

        return affectedRowOnCommand;
    }
    
    private IEnumerable<T>? SafeQueryOperation<T>(DatabaseOperationTypesEnum operationType, string sql, params object[] args)
    {
        var result = new List<T>();

        try
        {
            SafeOpenConnection();
            
            OnCommandExecution(operationType, sql, args);

            // Check if exist a lothium object, if not will instance a new one
            var type = typeof(T);
            var mapper = new AutoMapper(type);
            var props = AutoMapper.GetMappedProperties<T>();

            using var cmd = SafeCreateCommand(sql, args, CommandType.Text);
            
            var cmdReader = cmd.ExecuteReader();

            while (cmdReader.Read())
            {
                if (cmdReader.FieldCount <= 0) continue;

                var item = Activator.CreateInstance(type);

                ArgumentNullException.ThrowIfNull(mapper.TableData, nameof(mapper.TableData));
                ArgumentNullException.ThrowIfNull(mapper.ColumnsData, nameof(mapper.ColumnsData));

                foreach (var prop in props)
                {
                    var colInfo = Array.Find(mapper.ColumnsData.ToArray(),
                        col => col.PocoObjectPropertyName == prop.Name);
                    ArgumentNullException.ThrowIfNull(colInfo, nameof(colInfo));

                    var value = (string.IsNullOrEmpty(colInfo.Name))
                        ? cmdReader[colInfo.PocoObjectPropertyName]
                        : cmdReader[colInfo.Name];

                    value = DatabaseHelper.VerifyDbNullValue(colInfo, value);

                    prop.SetValue(item, value, null);
                    continue;
                }

                if (item is not null)
                {
                    result.Add((T)item);
                }
            }
        }
        catch (Exception ex)
        {
            OnErrorOccured(operationType, ex);
            
            result = Enumerable
                .Empty<T>()
                .ToList();
        }
        finally
        {
            OnCommandExecuted(operationType, sql, args);
            
            SafeCloseConnection();
        }

        return result;
    }
    
    /// <summary>
    /// Perform a bunch of check to validate the passed sql
    /// </summary>
    /// <param name="operation">Contains the performed operation that raised the error</param>
    /// <param name="sql">Contains the sql that will be executed into the provided database's instance</param>
    /// <param name="parameters">Contains a set of parameters used by the sql</param>
    protected virtual void OnCommandExecution(DatabaseOperationTypesEnum operation, string sql, object[] parameters)
    {
        //SqlBuilderException.ThrowIfSqlNullOrEmpty(sql.Query, sql.Params);
        
        if (string.IsNullOrEmpty(sql))
            throw new ArgumentException("There is not a valid Sql query!");

        if (sql.Contains(_provider.GetVariablePrefix()) && parameters.Length <= 0)
            throw new ArgumentException("There are no provided parameters for the query!");
    }
    
    /// <summary>
    /// Contains the actual performed sql with all the associated parameters
    /// </summary>
    /// <param name="operation">Contains the performed operation that raised the error</param>
    /// <param name="sql">Contains the sql executed into the provided database's instance</param>
    /// <param name="parameters">Contains a set of parameters used by the sql</param>
    protected virtual void OnCommandExecuted(DatabaseOperationTypesEnum operation,string sql, object[] parameters)
    {
        LastSql = new SqlBuilder(sql, parameters)
            .ToFormatQuery();
    }
    
    /// <summary>
    /// Contains an actual error generated during the execution of a provided sql
    /// </summary>
    /// <param name="operation">Contains the performed operation that raised the error</param>
    /// <param name="exception">Contains the generated error by the database's instance</param>
    protected virtual void OnErrorOccured(DatabaseOperationTypesEnum operation, Exception exception)
    {
        LastError = exception;
    }
    
    #endregion

    #region Transaction Methods
    
    /// <summary>
    /// Start a new database's transaction for an open connection for the selected provider
    /// if the connection is not set or open will return an argument null exception
    /// </summary>
    public void BeginTransaction() 
        => SafeOpenTransaction();

    /// <summary>
    /// Start a new database's transaction for an open connection for the selected provider
    /// if the connection is not set or open will return an argument null exception
    /// </summary>
    public async Task BeginTransactionAsync() 
        => await Task.Run(SafeOpenTransaction);
    
    /// <summary>
    /// Revert all the operations executed during the active database's transaction for the open connection for the selected provider
    /// If there is any open transaction it will simply exit the method
    /// </summary>
    public void RollbackTransaction()
        => SafeCloseTransaction(true);

    /// <summary>
    /// Revert all the operations executed during the active database's transaction for the open connection for the selected provider
    /// If there is any open transaction it will simply exit the method
    /// </summary>
    public async Task RollbackTransactionAsync()
        => await Task.Run(() => SafeCloseTransaction(true));
    
    /// <summary>
    /// Close the active database's transaction for the open connection for the selected provider
    /// If there is any open transaction it will simply exit the method
    /// </summary>
    public void CommitTransaction()
        => SafeCloseTransaction(false);

    /// <summary>
    /// Close the active database's transaction for the open connection for the selected provider
    /// If there is any open transaction it will simply exit the method
    /// </summary>
    public async Task CommitTransactionAsync()
        => await Task.Run(() => SafeCloseTransaction(false));
    
    #endregion
    
    #region  Scalar Command

    /// <summary>
    /// Invoke the DB Scalar command in the Database Instance and return a single value of a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public object? Scalar<T>(string sql, object[] args)
        => SafeScalarOperation<T>(DatabaseOperationTypesEnum.Scalar, sql, args);

    /// <summary>
    /// Invoke the DB Scalar command in the Database Instance and return a single value of a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>A value based of the object type</returns>
    public object? Scalar<T>(SqlBuilder sql) 
        => SafeScalarOperation<T>(DatabaseOperationTypesEnum.Scalar, sql.Query, sql.Params);

    /// <summary>
    /// Invoke the DB Scalar command in the Database Instance and return a single value of a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public async Task<object?>? ScalarAsync<T>(string sql, object[] args)
        => await Task.Run(() => SafeScalarOperation<T>(DatabaseOperationTypesEnum.ScalarAsync, sql, args));

    /// <summary>
    /// Invoke the DB Scalar command in the Database Instance and return a single value of a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>A value based of the object type</returns>
    public async Task<object?>? ScalarAsync<T>(SqlBuilder sql) 
        => await Task.Run(() => SafeScalarOperation<T>(DatabaseOperationTypesEnum.ScalarAsync, sql.Query, sql.Params));
    
    #endregion

    #region Execute Command

    /// <summary>
    /// Invoke the DB NonQuery command in the Database Instance and return the number of completed operations
    /// </summary>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>An int value that count all the affected table rows</returns>
    public int Execute(string sql, params object[] args)
        => SafeExecuteOperation(DatabaseOperationTypesEnum.ExecuteQuery, sql, args);

    /// <summary>
    /// Invoke the DB NonQuery command in the Database Instance and return the number of completed operations
    /// </summary>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>An int value that count all the affected table rows</returns>
    public int Execute(SqlBuilder sql)
        => SafeExecuteOperation(DatabaseOperationTypesEnum.ExecuteQuery, sql.Query, sql.Params);

    /// <summary>
    /// Invoke the DB NonQuery command in the Database Instance and return the number of completed operations
    /// </summary>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>An int value that count all the affected table rows</returns>
    public async Task<int> ExecuteAsync(string sql, params object[] args)
        => await Task.Run(() => SafeExecuteOperation(DatabaseOperationTypesEnum.ExecuteQueryAsync, sql, args));

    /// <summary>
    /// Invoke the DB NonQuery command in the Database Instance and return the number of completed operations
    /// </summary>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>An int value that count all the affected table rows</returns>
    public async Task<int> ExecuteAsync(SqlBuilder sql)
        => await Task.Run(() => SafeExecuteOperation(DatabaseOperationTypesEnum.ExecuteQueryAsync, sql.Query, sql.Params));
    
    #endregion

    #region Query Command

    /// <summary>
    /// Invoke the DB Query command in the Database Instance and cast it to a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public IEnumerable<T>? Query<T>(string sql, params object[] args)
        => SafeQueryOperation<T>(DatabaseOperationTypesEnum.Query, sql, args);

    /// <summary>
    /// Invoke the DB Query command in the Database Instance and cast it to a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>A value based of the object type</returns>
    public IEnumerable<T>? Query<T>(SqlBuilder sql) 
        => SafeQueryOperation<T>(DatabaseOperationTypesEnum.Query, sql.Query, sql.Params);

    /// <summary>
    /// Invoke the DB Query command in the Database Instance and cast it to a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public async Task<IEnumerable<T>?>? QueryAsync<T>(string sql, params object[] args)
        => await Task.Run(() => SafeQueryOperation<T>(DatabaseOperationTypesEnum.Query, sql, args));

    /// <summary>
    /// Invoke the DB Query command in the Database Instance and cast it to a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>A value based of the object type</returns>
    public async Task<IEnumerable<T>?>? QueryAsync<T>(SqlBuilder sql) 
        => await Task.Run(() => SafeQueryOperation<T>(DatabaseOperationTypesEnum.Query, sql.Query, sql.Params));
    
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
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public List<T>? FindAll<T>(string sql, params object[] args)
        => FindAll<T>(new SqlBuilder(sql, args));

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
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    /// <returns></returns>
    public T? FindSingle<T>(string sql, params object[] args)
        => FindSingle<T>(new SqlBuilder(sql, args));

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

    /// <summary>
    /// Generate a Paging List from a PageObject
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="page">Contains the page object</param>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public List<T> FetchPage<T>(PageObject<T> page, string sql, params object[] args)
        => FetchPage<T>(page, new SqlBuilder(sql, args));

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