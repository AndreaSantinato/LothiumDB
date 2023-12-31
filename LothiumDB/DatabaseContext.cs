﻿// System Classes
using System.Data;
// Custom Classes
using LothiumDB.Core;
using LothiumDB.Core.Helpers;
using LothiumDB.Extensions;
using LothiumDB.Configurations;
using LothiumDB.Interfaces;

// Namespace
namespace LothiumDB;

/// <summary>
///     <para>
///         Default Database Class of the library
///         Provides basic operations to the connected instance
///     </para>
/// </summary>
public class DatabaseContext : IDatabase, IDisposable
{
    #region Properties

    private readonly DatabaseContextConfiguration _dbConfiguration;
    private IDbConnection? _dbConnection;
    private DatabaseTransactionObject? _dbTransaction;
    private bool _auditExec = false;
    private bool _auditTableChecked = false;

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

    /// <summary>
    ///     <para>
    ///         this overridable method is executed after a core operation's error
    ///         If the audit mode is enable it will track the error inside the dedicated database's table
    ///         if you override this method you can write your own audit methods
    ///     </para>
    /// </summary>
    protected virtual void PostCommandExecution()
    {
        // Check if the audit is not enabled, if not write inside the dedicated table the event
        if (!_auditExec) NewAuditEvent(DateTime.Now, LastSql!, LastError);
    }
    
    /// <summary>
    ///     <para>
    ///         Internal private method that load the new database context using the assigned configuration
    ///     </para>
    /// </summary>
    /// <exception cref="ArgumentNullException">Will be generated if one of the configuration's property is not correct</exception>
    private void LoadDatabaseContext()
    {
        
    }

    /// <summary>
    ///     <para>
    ///         Open a new database's connection for the selected provider
    ///         If the configuration is not set or one of the property is not correct it will generate an argument null exception
    ///     </para>
    /// </summary>
    private void OpenConnection()
    {
        try
        {
            // If there is already an open transaction it will do nothing
            if (_dbTransaction is not null) return;
            
            // Validate the current connection object
            DatabaseExceptionHelper.ValidateDatabaseContextConnection(_dbConnection);
            
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
    ///     <para>
    ///         Close the active database's connection for the selected provider
    ///     </para>
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
    ///     <para>
    ///         Start a new database's transaction for an open connection for the selected provider
    ///         if the connection is not set or open will return an argument null exception
    ///     </para>
    /// </summary>
    public void BeginTransaction()
    {
        try
        {
            // Check if the connection and the configuration object is valid
            DatabaseExceptionHelper.ValidateDatabaseContextConnection(_dbConnection);
            DatabaseExceptionHelper.ValidateDatabaseContextConfiguration(_dbConfiguration);

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
    ///     <para>
    ///         Close the active database's transaction for the open connection for the selected provider
    ///         If there is any open transaction it will simply exit the method
    ///     </para>
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
    ///     <para>
    ///         Revert all the operations executed during the active database's transaction for the open connection for the selected provider
    ///         If there is any open transaction it will simply exit the method
    ///     </para>
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
    ///     <para>
    ///         Generate a new Database Command based on the actual Database's Provider
    ///     </para>
    /// </summary>
    /// <param name="commandType">Contains the database command's type</param>
    /// <param name="sql">Contains the actual sql query and all the parameters values</param>
    /// <returns>A Database's Command</returns>
    private IDbCommand? CreateCommand(CommandType commandType, SqlBuilder sql)
    {
        // Check if the minimum required variables are correctly sets
        ArgumentNullException.ThrowIfNull(_dbConnection);
        ArgumentNullException.ThrowIfNull(_dbConfiguration.Provider);
        ArgumentNullException.ThrowIfNull(sql);
        
        // Check if the query is empty
        if (string.IsNullOrEmpty(sql.Query)) 
            throw new ArgumentException(nameof(sql.Query));

        // Create the new command
        IDbCommand? command = null;
        try
        {
            command = _dbTransaction is null
                ? _dbConnection.CreateCommand()
                : _dbTransaction.Connection.CreateCommand();
            
            ArgumentNullException.ThrowIfNull(command);

            command.Transaction = _dbTransaction?.Transaction;
            command.CommandText = sql.Query;
            command.CommandType = commandType;

            if (sql.Params.Any())
            {
                switch (commandType)
                {
                    case CommandType.Text:
                        DatabaseHelper.AddParamsToDatabaseCommand(_dbConfiguration.Provider, ref command, sql);
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
                        throw new ArgumentOutOfRangeException(nameof(commandType), commandType, null);
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

    public DatabaseContext(DatabaseContextConfiguration configuration)
    {
        // Set the configuration object
        _dbConfiguration = configuration;
        
        // Validate the current loaded configuration
        DatabaseExceptionHelper.ValidateDatabaseContextConfiguration(_dbConfiguration);
        
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

    #region Scalar, Execute, Query Commands

    /// <summary>
    ///     <para>
    ///         Invoke the DB Scalar command in the Database Instance and return a single value of a specific object type
    ///     </para>
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>A value based of the object type</returns>
    public T Scalar<T>(SqlBuilder sql)
    {
        T? result = default;

        // If the query is empty it will return a default value
        if (string.IsNullOrEmpty(sql.Query)) return result!;
        
        LastSql = sql.ToFormatQuery();

        // Execute the query
        try
        {
            OpenConnection();

            var cmd = CreateCommand(CommandType.Text, sql);
            if (cmd is null) throw new Exception(nameof(cmd));
            
            using (cmd)
            {
                result = (T)cmd.ExecuteScalar()! ?? default;
            }
        }
        catch (Exception ex)
        {
            LastError = ex;
            result = default;
        }
        finally
        {
            CloseConnection();
        }

        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution();
        
        return result!;
    }

    /// <summary>
    ///     <para>
    ///         Invoke the DB Scalar command in the Database Instance and return a single value of a specific object type
    ///     </para>
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public T Scalar<T>(string sql, params object[] args)
        => Scalar<T>(new SqlBuilder(sql, args));

    /// <summary>
    ///     <para>
    ///         Invoke the DB NonQuery command in the Database Instance and return the number of completed operations
    ///     </para>
    /// </summary>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>An int value that count all the affected table rows</returns>
    public int Execute(SqlBuilder sql)
    {
        var affectedRowOnCommand = 0;

        // If the query is empty it will return a default value
        if (string.IsNullOrEmpty(sql.Query)) return affectedRowOnCommand;
        
        LastSql = sql.ToFormatQuery();

        // Execute the query
        try
        {
            OpenConnection();

            var cmd = CreateCommand(CommandType.Text, sql);
            if (cmd is null) throw new Exception(nameof(cmd));
            
            using (cmd)
            {
                affectedRowOnCommand = cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            // Add to info of the last executed command after an error
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
    ///     <para>
    ///         Invoke the DB NonQuery command in the Database Instance and return the number of completed operations
    ///     </para>
    /// </summary>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>An int value that count all the affected table rows</returns>
    public int Execute(string sql, params object[] args)
        => Execute(new SqlBuilder(sql, args));

    /// <summary>
    ///     <para>
    ///         Invoke the DB Query command in the Database Instance and cast it to a specific object type
    ///     </para>
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>A value based of the object type</returns>
    public IEnumerable<T> Query<T>(SqlBuilder sql)
    {
        var type = typeof(T);
        var list = new List<T>();

        // If the query is empty it will return an empty list of the passed type
        if (string.IsNullOrEmpty(sql.Query)) return Enumerable.Empty<T>();
        
        LastSql = sql.ToFormatQuery();

        // Execute the query
        try
        {
            OpenConnection();

            // Check if exist a lothium object, if not will instance a new one
            var pocoObject = new PocoObject<T>();

            var cmd = CreateCommand(CommandType.Text, sql);
            if (cmd is null) throw new Exception(nameof(cmd));
            
            using (cmd)
            {
                var cmdReader = cmd.ExecuteReader();
                while (cmdReader.Read())
                {
                    if (cmdReader.FieldCount <= 0) continue;
                    var obj = (T)Activator.CreateInstance(typeof(T))!;

                    foreach (var prop in pocoObject.GetProperties(type))
                    {
                        var pName = string.Empty;
                        if (pocoObject.ColumnDataInfo is not null)
                        {
                            foreach (var info in pocoObject
                                         .ColumnDataInfo
                                         .Where(c => c.PocoObjectProperty == prop.Name))
                            {
                                pName = info.ColumnName;
                                break;
                            }
                        }

                        var value = string.IsNullOrEmpty(pName) ? null : cmdReader[pName];
                        if (value == DBNull.Value) value = null;
                        if (value is not null) prop.SetValue(obj, value, null);
                    }

                    list.Add(obj);
                }
            }
        }
        catch (Exception ex)
        {
            LastError = ex;
            list = null;
        }
        finally
        {
            CloseConnection();
        }

        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution();

        return list!;
    }
    
    /// <summary>
    ///     <para>
    ///         Invoke the DB Query command in the Database Instance and cast it to a specific object type
    ///     </para>
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public IEnumerable<T> Query<T>(string sql, params object[] args)
        => Query<T>(new SqlBuilder(sql, args));

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
        // Check the if the required variables are correctly sets
        if (!_dbConfiguration.AuditMode) return;

        // Set a true the audit action flag
        _auditExec = true;
        
        // If the audit table not exists it will create it
        if (!_auditTableChecked && (_dbConfiguration.AuditMode && Execute(_dbConfiguration.Provider!.CheckIfAuditTableExists()) != 1))
        {
            if (Execute(_dbConfiguration.Provider!.CreateAuditTable()) > 0) 
                _auditExec = true;
        }
        
        // Insert the new event inside the audit table
        var sql = new SqlBuilder().InsertIntoTable(
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

    #region Find Methods (All, Single, Page)

    /// <summary>
    /// Select all the elements inside a table without specify the Sql query
    /// </summary>
    /// <returns>A value based of the object type</returns>
    public List<T> FindAll<T>()
        => FindAll<T>(DatabaseAutoQueryHelper.AutoSelectClause<T>());


    /// <summary>
    /// Select all the elements inside a table with a specify Sql query
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>A value based of the object type</returns>
    public List<T> FindAll<T>(SqlBuilder sql)
        => Query<T>(sql).ToList();

    /// <summary>
    /// Select all the elements inside a table with a specify Sql query
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    public List<T> FindAll<T>(string sql, params object[] args)
        => FindAll<T>(new SqlBuilder(sql, args));

    /// <summary>
    /// Select a single specific element inside a table
    /// </summary>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns>A value based of the object type</returns>
    public T FindSingle<T>(SqlBuilder sql)
        => Query<T>(sql).ToList().FirstElement()!;

    /// <summary>
    /// Select a single specific element inside a table
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <param name="args">Contains all the extra arguments of the query</param>
    /// <returns>A value based of the object type</returns>
    /// <returns></returns>
    public T FindSingle<T>(string sql, params object[] args)
        => FindSingle<T>(new SqlBuilder(sql, args));

    /// <summary>
    /// Generate a Paging List from a PageObject
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="page">Contains the page object</param>
    /// <returns>A value based of the object type</returns>
    public List<T> FetchPage<T>(PageObject<T> page)
    {
        var sqlAutoSelect = DatabaseAutoQueryHelper
            .AutoSelectClause<T>()
            .Where("WHERE 1=1");
        return FetchPage<T>(page, sqlAutoSelect);
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
        => Execute(DatabaseAutoQueryHelper.AutoInsertClause<T>(new PocoObject<T>(obj)));

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
        => Execute(DatabaseAutoQueryHelper.AutoUpdateClause(new PocoObject<T>(obj)));

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
        => Execute(DatabaseAutoQueryHelper.AutoDeleteClause(new PocoObject<T>(obj)));

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
        => Exist(DatabaseAutoQueryHelper.AutoExistClause<T>(new PocoObject<T>(obj)));

    #endregion
}