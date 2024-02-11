using System;
using System.Linq;
using System.Data;
using LothiumDB.Core;
using LothiumDB.Core.PocoDataInfo;
using LothiumDB.Core.Interfaces;
using LothiumDB.Linq;
using LothiumDB.Tools;
using LothiumDB.Exceptions;

// Namespace
namespace LothiumDB;

/// <summary>
/// Default Database Class of the library
/// Provides basic operations to the connected instance
/// </summary>
public class Database : IDatabase
{
    // Private ReadOnly Variables
    private readonly DatabaseConfiguration _dbConfiguration;
    private readonly IDbConnection? _dbConnection;

    // Private Variables
    private DatabaseTransactionObject? _dbTransaction;
    private bool _auditExec = false;
    private bool _auditTableChecked = false;

    // Private Constant Variables
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
        get => _dbConfiguration.QueryTimeOut;
        set => _dbConfiguration.QueryTimeOut = value;
    }

    #endregion

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

    /// <summary>
    /// Open a new database's connection for the selected provider
    /// If the configuration is not set or one of the property is not correct it will generate an argument null exception
    /// </summary>
    private void OpenConnection()
    {
        try
        {
            // If there is already an open transaction it will do nothing
            if (_dbTransaction is not null) return;

            // Validate the current connection object
            DatabaseException.ThrowIfConnectionIsNull(_dbConnection);

            // Open a new connection
            _dbConnection.Open();
        }
        catch (Exception ex)
        {
            LastError = ex;
            LastSql = string.Empty;
        }

        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution();
    }

    /// <summary>
    /// Close the active database's connection for the selected provider
    /// </summary>
    private void CloseConnection()
    {
        try
        {
            if (_dbTransaction is not null) return;
            if (_dbConnection is null) return;

            // Close a previously opened connection
            _dbConnection.Close();
        }
        catch (Exception ex)
        {
            LastError = ex;
            LastSql = string.Empty;
        }

        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution();
    }

    /// <summary>
    /// Start a new database's transaction for an open connection for the selected provider
    /// if the connection is not set or open will return an argument null exception
    /// </summary>
    public void BeginTransaction()
    {
        try
        {
            // Check if the connection and the configuration object is valid
            DatabaseException.ThrowIfConnectionIsNull(_dbConnection);
            DatabaseConfigurationException.ThrowIfConfigurationIsNotValid(_dbConfiguration);

            // Create a new transaction and start it
            _dbTransaction = new DatabaseTransactionObject(_dbConnection);
            _dbTransaction.BeginDatabaseTransaction();
        }
        catch (Exception ex)
        {
            LastError = ex;
            LastSql = string.Empty;
        }

        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution();
    }

    /// <summary>
    /// Close the active database's transaction for the open connection for the selected provider
    /// If there is any open transaction it will simply exit the method
    /// </summary>
    public void CommitTransaction()
    {
        try
        {
            if (_dbTransaction is null) return;
            _dbTransaction.CommitDatabaseTransaction();
        }
        catch (Exception ex)
        {
            LastError = ex;
            LastSql = string.Empty;
        }

        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution();
    }

    /// <summary>
    /// Revert all the operations executed during the active database's transaction for the open connection for the selected provider
    /// If there is any open transaction it will simply exit the method
    /// </summary>
    public void RollbackTransaction()
    {
        try
        {
            if (_dbTransaction is null) return;
            _dbTransaction.RollbackDatabaseTransaction();
        }
        catch (Exception ex)
        {
            LastError = ex;
            LastSql = string.Empty;
        }

        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution();
    }

    /// <summary>
    /// Generate a new Database Command based on the actual Database's Provider
    /// </summary>
    /// <param name="commandType">Contains the database command's type</param>
    /// <param name="sql">Contains the actual sql query</param>
    /// <param name="args">Contains the actual sql query's parameters values</param>
    /// <returns>A Database's Command</returns>
    private IDbCommand? CreateCommand(CommandType commandType, string sql, object[] args)
    {
        // Check if the minimum required variables are correctly sets
        ArgumentNullException.ThrowIfNull(_dbConnection);
        ArgumentNullException.ThrowIfNull(_dbConfiguration.Provider);

        // Check if the query is empty
        if (string.IsNullOrEmpty(sql)) throw new ArgumentException(null, nameof(sql));

        // Create the new command
        IDbCommand? command = null;

        try
        {
            command = (_dbTransaction is null)
                ? _dbConnection.CreateCommand()
                : _dbTransaction.Connection.CreateCommand();

            ArgumentNullException.ThrowIfNull(command);

            command.Transaction = _dbTransaction?.Transaction;
            command.CommandText = sql;
            command.CommandType = commandType;

            if (args.Any())
            {
                switch (commandType)
                {
                    case CommandType.Text:
                        DatabaseHelper.AddParamsToDatabaseCommand(
                            _dbConfiguration.Provider,
                            ref command,
                            new SqlBuilder(sql, args)
                        );
                        break;
                    case CommandType.TableDirect:
                        //
                        // TODO: 
                        //
                        break;
                    case CommandType.StoredProcedure:
                        //
                        // TODO: 
                        //
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(commandType),
                            commandType,
                            null
                        );
                }
            }
        }
        catch (Exception ex)
        {
            LastError = ex;
            LastSql = string.Empty;
        }

        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution();

        return command;
    }

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
        _dbConfiguration = new DatabaseConfiguration()
        {
            Provider = provider,
            QueryTimeOut = operationTimeOut,
            AuditMode = audit
        };

        // Validate the current loaded configuration
        DatabaseConfigurationException.ThrowIfConfigurationIsNotValid(_dbConfiguration);

        // Set the connection with the configuration's values
        _dbConnection = _dbConfiguration.Provider!.CreateConnection();
        _dbTransaction = null;

        // Set the history property to their default values
        LastError = null;
        LastSql = string.Empty;
    }

    /// <summary>
    /// Dispose the Database Instance Previously Created
    /// </summary>
    public void Dispose()
        => GC.SuppressFinalize(this);

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

            OpenConnection();

            var cmd = CreateCommand(CommandType.Text, sql, args);
            if (cmd is null) throw new Exception(nameof(cmd));

            using (cmd)
            {
                result = cmd.ExecuteScalar();
            }
        }
        catch (Exception ex)
        {
            OnErrorOccured(ex);
            LastError = ex;
            result = default;
        }
        finally
        {
            CloseConnection();
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

            OpenConnection();

            var cmd = CreateCommand(CommandType.Text, sql, args);
            if (cmd is null) throw new Exception(nameof(cmd));

            using (cmd)
            {
                affectedRowOnCommand = cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            OnErrorOccured(ex);
            LastError = ex;
            affectedRowOnCommand = -1;
        }
        finally
        {
            CloseConnection();
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

            OpenConnection();

            // Check if exist a lothium object, if not will instance a new one
            var type = typeof(T);
            var mapper = new AutoMapper(type);
            var props = AutoMapper.GetMappedProperties<T>();

            var cmd = CreateCommand(CommandType.Text, sql, args);
            if (cmd is null) throw new Exception(nameof(cmd));

            using (cmd)
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
        }
        catch (Exception ex)
        {
            OnErrorOccured(ex);
            LastError = ex;
            result = null;
        }
        finally
        {
            CloseConnection();
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
    public IEnumerable<T> Query<T>(SqlBuilder sql) => Query<T>(sql.Query, sql.Params);

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
        var result = Query<T>(sql).ToList();
        return (result is null)
            ? default(T) 
            : result.FirstElement();
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
        if (_dbConfiguration.Provider is null) return (List<T>)Enumerable.Empty<T>();
        var pageSql = _dbConfiguration.Provider.BuildPageQuery<T>(page, sql);
        return (List<T>)(string.IsNullOrEmpty(pageSql.Query) ? Enumerable.Empty<T>() : Query<T>(pageSql).ToList());
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
        if (!_dbConfiguration.AuditMode) return;

        // Set a true the audit action flag
        _auditExec = true;

        // If the audit table not exists it will create it
        if (!_auditTableChecked && _dbConfiguration.AuditMode)
        {
            sql = _dbConfiguration.Provider!.CheckIfAuditTableExists();
            if (Execute(sql) == 0)
            {
                sql = _dbConfiguration.Provider!.CreateAuditTable();
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
    ///     <para>
    ///         Insert the passed object inside a table of the database in the form of a row
    ///     </para>
    /// </summary>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <typeparam name="T">Contains the type of the objects to be insert inside the database</typeparam>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Insert<T>(object obj)
        => Execute(AutoMapper.AutoInsertClause<T>(obj));

    /// <summary>
    ///     <para>
    ///         Insert the passed list of objects inside a table of the database in the form of a row
    ///     </para>
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
    ///     <para>
    ///         Update a number of element inside a table of the database
    ///     </para>
    /// </summary>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <typeparam name="T">Contains the type of the objects to be insert inside the database</typeparam>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Update<T>(object obj)
        => Execute(AutoMapper.AutoUpdateClause<T>(obj));

    /// <summary>
    ///     <para>
    ///         Update the passed list of elements inside a table of the database
    ///     </para>
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
    ///     <para>
    ///         Delete a number of element inside a table of the database
    ///     </para>
    /// </summary>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Delete<T>(object obj)
        => Execute(AutoMapper.AutoDeleteClause<T>(obj));

    /// <summary>
    ///     <para>
    ///         Delete the passed list of elements inside a table of the database
    ///     </para>
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
    ///     <para>
    ///         If an object already exist inside the database will update it, otherwise will create it
    ///     </para>
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Save<T>(object obj)
        => Exist<T>(obj) ? Update<T>(obj) : Insert<T>(obj);

    /// <summary>
    ///     <para>
    ///         Search inside the database's if a record exist
    ///     </para>
    /// </summary>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns></returns>
    public bool Exist(SqlBuilder sql)
        => Convert.ToBoolean(Scalar<int>(sql));

    /// <summary>
    ///     <para>
    ///         Search inside the database's if a record exist
    ///     </para>
    /// </summary>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns></returns>
    public bool Exist(string sql, params object[] args)
        => Exist(new SqlBuilder(sql, args));

    /// <summary>
    ///     <para>
    ///         Search inside the database's if a record exist
    ///     </para>
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <returns></returns>
    public bool Exist<T>(object obj)
        => Exist(AutoMapper.AutoExistClause<T>(obj));

    #endregion
}