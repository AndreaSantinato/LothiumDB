// System Class
using System.Data;
// Custom Class
using LothiumDB.Core;
using LothiumDB.Core.Helpers;
using LothiumDB.Enumerations;
using LothiumDB.Extensions;
using LothiumDB.Configurations;
using LothiumDB.Data.Models;
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

    private readonly DatabaseContextConfiguration _configuration;
    private IDbConnection? _dbConnection;
    private DatabaseTransactionObject? _dbTransaction;
    private bool _auditExec = false;

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
    /// Contains the database's provider
    /// </summary>
    public IDatabaseProvider? Provider => _configuration.Provider;

    /// <summary>
    /// Contains the complete last executed query with all parameters replace
    /// </summary>
    public string? LastExecutedSql { get; private set; }

    /// <summary>
    /// Contains the last executed query without parameters replace
    /// </summary>
    public string? LastExecuteCommandType { get; private set; }

    /// <summary>
    /// Contains the last database's generated error
    /// </summary>
    public Exception? LastGeneratedError { get; private set; }

    /// <summary>
    /// Contains the database's command timeout value
    /// </summary>
    public int CommandTimeOut
    {
        get => _configuration.QueryTimeOut;
        set => _configuration.QueryTimeOut = value;
    }

    #endregion

    #region Context Initialization

    /// <summary>
    ///     <para>
    ///         This overridable method contains all the configuration for this current database context
    ///         You will need to override it and set all the property with a not null value
    ///     </para>
    /// </summary>
    ///     <example>
    ///         dbConfiguration.Provider = new MsSqlServerProvider();
    ///         dbConfiguration.ConnectionString = "[Put here the connection string]"
    ///         dbConfiguration.AuditMode = false;
    ///         dbConfiguration.AuditUser = "[Put here the audit user]";
    ///     </example>
    /// <param name="dbConfiguration"></param>
    protected virtual void SetConfiguration(DatabaseContextConfiguration dbConfiguration) { }

    /// <summary>
    ///     <para>
    ///         this overridable method is executed after a core operation's error
    ///         If the audit mode is enable it will track the error inside the dedicated database's table
    ///         if you override this method you can write your own audit methods
    ///     </para>
    /// </summary>
    /// <param name="message">Contains the event message</param>
    protected virtual void PostCommandExecution(string message)
    {
        // Check if the audit is enabled
        if (_auditExec) return;
        
        // Define the different values to write inside the new audit row
        var auditLevel = LastGeneratedError is null 
            ? AuditLevelsEnum.Info 
            : AuditLevelsEnum.Error;
        var cmdType = string.IsNullOrEmpty(LastExecuteCommandType)
            ? DBCommandTypeEnum.None
            : DatabaseHelper.DefineDatabaseCommandType(LastExecuteCommandType);
        var sqlType = string.IsNullOrEmpty(LastExecutedSql)
            ? SqlCommandTypeEnum.None
            : DatabaseHelper.DefineSqlCommandType(LastExecutedSql);
        var errMsg = LastGeneratedError is null
            ? string.Empty
            : LastGeneratedError.Message;
        
        // Generate the new audit event's row
        NewAuditEvent(auditLevel, cmdType, sqlType, errMsg);
    }
    
    /// <summary>
    ///     <para>
    ///         Internal private method that load the new database context using the assigned configuration
    ///     </para>
    /// </summary>
    /// <exception cref="ArgumentNullException">Will be generated if one of the configuration's property is not correct</exception>
    private void LoadDatabaseContext()
    {
        // Set the configuration object
        SetConfiguration(_configuration);
        
        // Validate the current loaded configuration
        DatabaseExceptionHelper.ValidateDatabaseContextConfiguration(_configuration);

        // Set the connection with the configuration's values
        var connString = _configuration.Provider!.DbConnectionString;
        _dbConnection = _configuration.Provider!.CreateConnection(connString);
        _dbConnection.ConnectionString = connString;
        _dbTransaction = null;
        
        // Set the history property to their default values
        LastGeneratedError = null;
        LastExecutedSql = string.Empty;
        LastExecuteCommandType = string.Empty;
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
            LastGeneratedError = ex;
            LastExecutedSql = string.Empty;
            LastExecuteCommandType = string.Empty;
        }
        
        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution(System.Reflection.MethodBase.GetCurrentMethod()?.Name ?? string.Empty);
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
            LastGeneratedError = ex;
            LastExecutedSql = string.Empty;
            LastExecuteCommandType = string.Empty;
        }
        
        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution(System.Reflection.MethodBase.GetCurrentMethod()?.Name ?? string.Empty);
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
            DatabaseExceptionHelper.ValidateDatabaseContextConfiguration(_configuration);

            // Create a new transaction and start it
            _dbTransaction = new DatabaseTransactionObject(_dbConnection);
            _dbTransaction.BeginDatabaseTransaction();
        }
        catch (Exception ex)
        {
            LastGeneratedError = ex;
            LastExecutedSql = string.Empty;
            LastExecuteCommandType = string.Empty;
        }
        
        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution(System.Reflection.MethodBase.GetCurrentMethod()?.Name ?? string.Empty);
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
            LastGeneratedError = ex;
            LastExecutedSql = string.Empty;
            LastExecuteCommandType = string.Empty;
        }
        
        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution(System.Reflection.MethodBase.GetCurrentMethod()?.Name ?? string.Empty);
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
            LastGeneratedError = ex;
            LastExecutedSql = string.Empty;
            LastExecuteCommandType = string.Empty;
        }
        
        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution(System.Reflection.MethodBase.GetCurrentMethod()?.Name ?? string.Empty);
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
        ArgumentNullException.ThrowIfNull(_configuration.Provider);
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
                        DatabaseHelper.AddParamsToDatabaseCommand(
                            _configuration.Provider,
                            ref command,
                            sql
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
                        throw new ArgumentOutOfRangeException(nameof(commandType), commandType, null);
                }
            }
        }
        catch (Exception ex)
        {
            LastGeneratedError = ex;
            LastExecutedSql = string.Empty;
            LastExecuteCommandType = string.Empty;
        }
        
        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution(System.Reflection.MethodBase.GetCurrentMethod()?.Name ?? string.Empty);
        
        return command;
    }
    
    /// <summary>
    /// Create a new Database Instance
    /// </summary>
    protected DatabaseContext()
    {
        _configuration = new DatabaseContextConfiguration();
        LoadDatabaseContext();
    }

    /// <summary>
    /// Dispose the Database Instance Previously Created
    /// </summary>
    public void Dispose() => GC.SuppressFinalize(this);
    
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
        
        LastExecutedSql = sql.ToFormatQuery();

        // Execute the query
        try
        {
            OpenConnection();

            var cmd = CreateCommand(CommandType.Text, sql);
            if (cmd is null) throw new Exception(nameof(cmd));

            LastExecuteCommandType = Enum.GetName(typeof(CommandType), cmd.CommandType);
            
            using (cmd)
            {
                result = (T)cmd.ExecuteScalar()! ?? default;
            }
        }
        catch (Exception ex)
        {
            LastGeneratedError = ex;
            result = default;
        }
        finally
        {
            CloseConnection();
        }

        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution(System.Reflection.MethodBase.GetCurrentMethod()?.Name ?? string.Empty);
        
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
        
        LastExecutedSql = sql.ToFormatQuery();

        // Execute the query
        try
        {
            OpenConnection();

            var cmd = CreateCommand(CommandType.Text, sql);
            if (cmd is null) throw new Exception(nameof(cmd));

            LastExecuteCommandType = Enum.GetName(typeof(CommandType), cmd.CommandType);
            
            using (cmd)
            {
                affectedRowOnCommand = cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            // Add to info of the last executed command after an error
            LastGeneratedError = ex;
            affectedRowOnCommand = -1;
        }
        finally
        {
            CloseConnection();
        }

        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution(System.Reflection.MethodBase.GetCurrentMethod()?.Name ?? string.Empty);

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
        
        LastExecutedSql = sql.ToFormatQuery();

        // Execute the query
        try
        {
            OpenConnection();

            // Check if exist a lothium object, if not will instance a new one
            var pocoObject = new PocoObject<T>();

            var cmd = CreateCommand(CommandType.Text, sql);
            if (cmd is null) throw new Exception(nameof(cmd));

            LastExecuteCommandType = Enum.GetName(typeof(CommandType), cmd.CommandType);
            
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
            LastGeneratedError = ex;
            list = null;
        }
        finally
        {
            CloseConnection();
        }

        // Add a new audit event inside the database table if the mode is enable
        PostCommandExecution(System.Reflection.MethodBase.GetCurrentMethod()?.Name ?? string.Empty);

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
    /// <param name="level">Contains the level of the current action</param>
    /// <param name="cmdType">Contains the type of the current action's command</param>
    /// <param name="sqlType">Contains the type of the current action's query</param>
    /// <param name="errorMsg">Contains an optional error message</param>
    private void NewAuditEvent(
        AuditLevelsEnum level, 
        DBCommandTypeEnum cmdType, 
        SqlCommandTypeEnum sqlType,
        string? errorMsg = null
    )
    {
        // Check the if the required variables are correctly sets
        if (!_configuration.AuditMode) return;
        if (string.IsNullOrEmpty(LastExecuteCommandType)) return;
        if (string.IsNullOrEmpty(LastExecutedSql)) return;

        // Set a true the audit action flag
        _auditExec = true;

        // Verify if the audit table already exists, if not will create it
        if (Scalar<int>(
            new SqlBuilder(@"
                IF (
                    EXISTS 
                    (
                        SELECT  *
                        FROM    INFORMATION_SCHEMA.TABLES
                        WHERE   TABLE_SCHEMA = @0
                                AND TABLE_NAME = @1
                    )
                )
                BEGIN
                    SELECT 1
                END
                ELSE
                BEGIN
                	SELECT 0
                END
            ", "dbo", "AuditEvents")
        ) != 1) Execute(Provider!.CreateAuditTable());

        // Insert the new event inside the audit table
        Insert<AuditModel>(new AuditModel()
        {
            Level = Enum.GetName(level.GetType(), level),
            User = _configuration.AuditUser,
            ExecutedOnDate = DateTime.Now,
            DatabaseCommandType = Enum.GetName(cmdType.GetType(), cmdType),
            SqlCommandType = Enum.GetName(sqlType.GetType(), sqlType),
            SqlCommandWithoutParams = LastExecuteCommandType,
            SqlCommandWithParams = LastExecutedSql,
            ErrorMsg = errorMsg
        });

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
        if (_configuration.Provider is null) return (List<T>)Enumerable.Empty<T>();
        var pageSql = _configuration.Provider.BuildPageQuery<T>(page, sql);
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