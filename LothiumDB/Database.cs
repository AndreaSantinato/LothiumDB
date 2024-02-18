using System;
using System.Linq;
using System.Data;
using LothiumDB.Core;
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
    private IProvider _dbProvider;
    private IDbConnection _dbConnection;
    private DatabaseTransaction? _dbTransaction;
    private int _dbQueryTimeOut = 30;
    private bool _dbAuditMode = false;
    private bool _auditExec = false;
    private bool _auditTableChecked = false;
    private bool _disposed = false;
    private const int QUERY_TIMEOUT_VALUE = 30;
    private const bool AUDIT_DEFAULT_VALUE = false;

    #region Properties

    /// <summary>
    /// Indicates if the current database instance connection is open
    /// </summary>
    public bool IsConnectionOpen
    {
        get
        {
            if (_dbConnection is null) return false;

            return _dbConnection.State switch
            {
                ConnectionState.Open => true,
                ConnectionState.Connecting => true,
                ConnectionState.Fetching => true,
                ConnectionState.Executing => true,
                ConnectionState.Broken => false,
                ConnectionState.Closed => false,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    /// <summary>
    /// Contains the complete last executed query with all parameters replace
    /// </summary>
    public string? LastSql { get; private set; }

    /// <summary>
    /// Contains the last database's generated error
    /// </summary>
    public Exception? LastError { get; private set; }

    /// <summary>
    /// Contains the database's command timeout value
    /// </summary>
    public int CommandTimeOut
    {
        get => _dbQueryTimeOut;
        set => _dbQueryTimeOut = value;
    }

    #endregion

    #region Constructors & Destructors

    /// <summary>
    /// Create a new Database instance object from a specific Provider configuration
    /// </summary>
    /// <param name="provider">Contains the database instance's provider</param>
    /// <param name="audit">Indicates if the audit mode is enable</param>
    public Database(IProvider provider, bool audit = false) : this(provider, QUERY_TIMEOUT_VALUE, audit) { }

    /// <summary>
    /// Create a new Database instance object from a specific Provider configuration
    /// </summary>
    /// <param name="provider">Contains the database instance's provider</param>
    /// <param name="operationTimeOut">Indicates the query timeout values</param>
    /// <param name="audit">Indicates if the audit mode is enable</param>
    public Database(IProvider provider, int operationTimeOut = QUERY_TIMEOUT_VALUE, bool audit = AUDIT_DEFAULT_VALUE)
    {
        // Set the configuration object
        _dbProvider = provider;
        _dbQueryTimeOut = operationTimeOut;
        _dbAuditMode = audit;

        // Generate the new connection and transaction objects
        _dbConnection = _dbProvider.CreateConnection();
        _dbTransaction = new DatabaseTransaction(_dbProvider, _dbConnection);

        // Set the history property to their default values
        LastError = null;
        LastSql = string.Empty;
    }

    /// <summary>
    /// Dispose the Database Instance Previously Created
    /// </summary>
    public void Dispose()
        => DisposeInstance(true);

    #endregion Constructors & Destructors

    #region Context Initialization

    protected virtual void OnErrorOccured(Exception exception)
    {
        //
        // ToDo: Define the base actions
        //
    }

    /// <summary>
    /// This overridable method is executed after a core operation's error
    /// If the audit mode is enable it will track the error inside the dedicated database's table
    /// if you override this method you can write your own audit methods
    /// </summary>
    protected virtual void PostCommandExecution()
    {
        // Check if the audit is not enabled, if not write inside the dedicated table the event
        if (!_auditExec) NewAuditEvent(DateTime.Now, LastSql!, LastError);
    }

    private void DisposeInstance(bool dispose)
    {
        if (_disposed) return;

        if (dispose)
        {
            //
            // ToDo: Defines the resource to deallocate from the memory
            //
        }

        GC.SuppressFinalize(this);
    }

    #endregion Context Initialization

    #region Connection & Transaction

    /// <summary>
    /// Open a new database's connection for the selected provider
    /// If the configuration is not set or one of the property is not correct it will generate an argument null exception
    /// </summary>
    private void OpenInternalConnection()
    {
        if (_dbTransaction is null || !_dbTransaction.Usable)
            DatabaseHelper.OpenSafeConnection(_dbProvider, _dbConnection);
    }


    /// <summary>
    /// Close the active database's connection for the selected provider
    /// </summary>
    private void CloseInternalConnection()
    {
        if (_dbTransaction is null || !_dbTransaction.Usable)
            DatabaseHelper.CloseSafeConnection(_dbProvider, _dbConnection);
    }

    /// <summary>
    /// Start a new database's transaction for an open connection for the selected provider
    /// if the connection is not set or open will return an argument null exception
    /// </summary>
    public void BeginTransaction()
    {
        _dbTransaction ??= new DatabaseTransaction(_dbProvider, _dbConnection);

        if (_dbTransaction.Usable)
            throw new DatabaseException("There is an existing open transaction!!");

        _dbTransaction.BeginTransaction();
    }

    /// <summary>
    /// Revert all the operations executed during the active database's transaction for the open connection for the selected provider
    /// If there is any open transaction it will simply exit the method
    /// </summary>
    public void RollbackTransaction()
    {
        if (_dbTransaction is null || !_dbTransaction.Usable)
            throw new DatabaseException("There is no openend transaction to perform a rollback operation!");

        _dbTransaction.RollbackTransaction();
    }

    /// <summary>
    /// Close the active database's transaction for the open connection for the selected provider
    /// If there is any open transaction it will simply exit the method
    /// </summary>
    public void CommitTransaction()
    {
        if (_dbTransaction is null || !_dbTransaction.Usable)
            throw new DatabaseException("There is no openend transaction to perform a commit operation!");

        _dbTransaction.CommitTransaction();
    }

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
    {
        object? result = null;

        try
        {
            SqlBuilderException.ThrowIfSqlNullOrEmpty(sql, args);

            LastSql = new SqlBuilder(sql, args).ToFormatQuery();

            OpenInternalConnection();

            using (var cmd = DatabaseHelper.CreateSafeCommand(sql, args, CommandType.Text, _dbProvider, _dbConnection, _dbTransaction))
            {
                result = cmd.ExecuteScalar();
            }

            CloseInternalConnection();
        }
        catch (Exception ex)
        {
            OnErrorOccured(ex);
            LastError = ex;
            result = default;
        }

        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution();

        return result;
    }

    /// <summary>
    /// Invoke the DB Scalar command in the Database Instance and return a single value of a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>A value based of the object type</returns>
    public object? Scalar<T>(SqlBuilder sql) => Scalar<T>(sql.Query, sql.Params);

    #endregion

    #region Execute Command

    /// <summary>
    /// Invoke the DB NonQuery command in the Database Instance and return the number of completed operations
    /// </summary>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>An int value that count all the affected table rows</returns>
    public int Execute(string sql, params object[] args)
    {
        int affectedRowOnCommand = 0;

        try
        {
            SqlBuilderException.ThrowIfSqlNullOrEmpty(sql, args);

            LastSql = new SqlBuilder(sql, args).ToFormatQuery();

            OpenInternalConnection();

            using (var cmd = DatabaseHelper.CreateSafeCommand(sql, args, CommandType.Text, _dbProvider, _dbConnection, _dbTransaction))
            {
                affectedRowOnCommand = cmd.ExecuteNonQuery();
            }

            CloseInternalConnection();
        }
        catch (Exception ex)
        {
            OnErrorOccured(ex);
            LastError = ex;
            affectedRowOnCommand = -1;
        }

        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution();

        return affectedRowOnCommand;
    }

    /// <summary>
    /// Invoke the DB NonQuery command in the Database Instance and return the number of completed operations
    /// </summary>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>An int value that count all the affected table rows</returns>
    public int Execute(SqlBuilder sql) => Execute(sql.Query, sql.Params);

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
    {
        var result = new List<T>();

        try
        {
            SqlBuilderException.ThrowIfSqlNullOrEmpty(sql, args);

            LastSql = new SqlBuilder(sql, args).ToFormatQuery();

            // Check if exist a lothium object, if not will instance a new one
            var type = typeof(T);
            var mapper = new AutoMapper(type);
            var props = AutoMapper.GetMappedProperties<T>();

            OpenInternalConnection();

            using (var cmd = DatabaseHelper.CreateSafeCommand(sql, args, CommandType.Text, _dbProvider, _dbConnection, _dbTransaction))
            {
                var cmdReader = cmd.ExecuteReader();

                while (cmdReader.Read())
                {
                    if (cmdReader.FieldCount <= 0) continue;

                    var item = Activator.CreateInstance(type);

                    ArgumentNullException.ThrowIfNull(mapper.TableData, nameof(mapper.TableData));
                    ArgumentNullException.ThrowIfNull(mapper.ColumnsData, nameof(mapper.ColumnsData));

                    foreach (var prop in props)
                    {
                        var colInfo = Array.Find(mapper.ColumnsData.ToArray(), col => col.PocoObjectPropertyName == prop.Name);
                        ArgumentNullException.ThrowIfNull(colInfo, nameof(colInfo));

                        var value = (string.IsNullOrEmpty(colInfo.Name))
                            ? cmdReader[colInfo.PocoObjectPropertyName]
                            : cmdReader[colInfo.Name];

                        value = DatabaseHelper.VerifyDBNullValue(colInfo, value);

                        prop.SetValue(item, value, null);
                        continue;
                    }

                    if (item is not null) result.Add((T)item);
                }
            }

            CloseInternalConnection();
        }
        catch (Exception ex)
        {
            OnErrorOccured(ex);
            LastError = ex;
            result = null;
        }

        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution();

        return result;
    }

    /// <summary>
    /// Invoke the DB Query command in the Database Instance and cast it to a specific object type
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>A value based of the object type</returns>
    public IEnumerable<T>? Query<T>(SqlBuilder sql) => Query<T>(sql.Query, sql.Params);

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
    {
        var result = Query<T>(sql);
        return (result is null)
            ? null
            : result.ToList();
    }

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
        if (_dbProvider is null)
            return (List<T>)Enumerable.Empty<T>();

        var pageSql = _dbProvider.BuildPageQuery<T>(page, sql);

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

    #region Audit Methods

    /// <summary>
    /// Methods that add a new Audit Event inside the database dedicated table
    /// </summary>
    /// <param name="executedDateTime">Indicates when the event was performed</param>
    /// <param name="sqlQuery">Contains the actual performed query</param>
    /// <param name="dbError">Contains an occured error</param>
    private void NewAuditEvent(DateTime executedDateTime, string? sqlQuery, Exception? dbError)
    {
        SqlBuilder? sql;

        // Check the if the required variables are correctly sets
        if (!_dbAuditMode) return;

        // Set a true the audit action flag
        _auditExec = true;

        // If the audit table not exists it will create it
        if (!_auditTableChecked && _dbAuditMode)
        {
            sql = _dbProvider!.CheckIfAuditTableExists();
            if (Execute(sql) == 0)
            {
                sql = _dbProvider!.CreateAuditTable();
                Execute(sql);
            }
            _auditTableChecked = true;
        }

        // Insert the new event inside the audit table
        sql = new SqlBuilder().InsertIntoTable(
            "AuditEvents",
            new object[]
            {
                "ExecutedOn",
                "SqlQuery",
                "IsError",
                "ErrorMessage"
            },
            new object[]
            {
                executedDateTime,
                sqlQuery ?? string.Empty,
                dbError is null ? 0 : 1,
                dbError is null ? string.Empty : dbError.Message
            }
        );
        Execute(sql);

        // Set a false the audit's execution flag
        _auditExec = false;
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