// System Class
using System.Data;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
// Custom Class
using LothiumDB.Enumerations;
using LothiumDB.Helpers;
using LothiumDB.Interfaces;
using LothiumDB.Providers;
using LothiumDB.Models;
using LothiumDB.Exceptions;
using LothiumDB.Extensions;

namespace LothiumDB
{
    /// <summary>
    /// Default Database Class of the library, provides basic operations to the connected istance
    /// </summary>
    public class Database : IDatabase, IDisposable
    {
        // Properties (Public & Private)

        #region Connection, Transaction, Provider Properties

        protected IDbConnection? _dbConn = null;
        protected IDbTransaction? _dbTran = null;
        protected IDbProvider? _dbProv = null;

        /// <summary>
        /// Contains the database's connection
        /// </summary>
        public IDbConnection? Connection { get { return _dbConn; } private set { _dbConn = value; } }

        /// <summary>
        /// Contains the database's transaction
        /// </summary>
        public IDbTransaction? Transaction { get { return _dbTran; } private set { _dbTran = value; } }

        /// <summary>
        /// Contains the database's provider
        /// </summary>
        public IDbProvider? Provider { get { return _dbProv; } private set { _dbProv = value; } }

        /// <summary>
        /// Return the current state of the connection
        /// True = Connection Open; False = Connection Close
        /// </summary>
        public bool IsConnectionOpen { get { return ManageConnectionState(); } }

        #endregion

        #region LastSql, LastCommand Properties

        private String? _lastSql = String.Empty;
        private String? _lastCommand = String.Empty;

        /// <summary>
        /// Contains the complete last executed query with all parameters replace
        /// </summary>
        public String? LastSql { get { return _lastSql; } private set { _lastSql = value; } }

        /// <summary>
        /// Contains the last executed query without parameters replace
        /// </summary>
        public String? LastCommand { get { return _lastCommand; } private set { _lastCommand = value; } }

        #endregion

        #region Database Exception Property

        private DatabaseException? _lastExcep = null;

        /// <summary>
        /// Contains the last database's generated error
        /// </summary>
        public DatabaseException? LastError { get { return _lastExcep; } private set { _lastExcep = value; } }

        #endregion

        #region Command TimeOut Properties

        /// <summary>
        /// Private Property For The Database's Command TimeOut
        /// </summary>
        protected int _cmdTimeOut;

        /// <summary>
        /// Contains the database's command timeout value
        /// </summary>
        public int CommandTimeOut { get { return _cmdTimeOut; } set { _cmdTimeOut = value; } }

        #endregion

        #region Audit Properties

        /// <summary>
        /// Indicate if the audit table will be populated or not
        /// </summary>
        private bool _auditMode = false;

        /// <summary>
        /// Indicate the default user that will add in the audit table for each operation
        /// </summary>
        private string _auditUser = "LothiumDB";

        /// <summary>
        /// Indicate if the audit is currently executing an operation
        /// </summary>
        private bool _auditExec = false;

        #endregion
        
        #region SafeHandle Properties

        /// <summary>
        /// Used to detect redundant calls
        /// </summary>
        private bool _disposedValue;

        /// <summary>
        /// Instantiate a SafeHandle instance.
        /// </summary>
        private SafeHandle _safeHandle = new SafeFileHandle(IntPtr.Zero, true);

        #endregion

        // Core Methods

        #region Class Constructor & Destructor Methods

        /// <summary>
        /// Inizialize a new Database Communication object
        /// </summary>
        /// <param name="provider">Contains the db's provider</param>
        /// <param name="connectionString">Contains the connection string</param>
        public Database(IDbProvider provider, string connectionString)
        {
            // Set the choosen provider
            _dbProv = provider;

            // Set the connection and the transaction
            _dbConn = DatabaseUtility.GenerateConnection(provider.ProviderType(), connectionString);
            _dbConn.ConnectionString = connectionString;
            _dbTran = null;

            // Set the remaining properties
            _lastSql = String.Empty;
            _lastCommand = String.Empty;
            _lastExcep = null;
        }

        /// <summary cref="Database">
        /// By Default this method will inizialize a new Microsoft SQL Server database communication's istance
        /// </summary>
        /// <param name="connectionString">Contains the connection string</param>
        public Database(string connectionString) : this(new MSSqlServerProvider(), connectionString) { }

        /// <summary cref="Database"></summary>
        /// <param name="provider">Contains the db's provider</param>
        /// <param name="args">Contains all the arguments needed for creating the connection string</param>
        public Database(IDbProvider provider, params object[] args) : this(provider, provider.GenerateConnectionString(args)) { }

        /// <summary>
        /// Protected Method Of The Dispose Pattern
        /// </summary>
        /// <param name="disposing">Indicate if the obgect's dispose is required</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing) _safeHandle.Dispose();
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Dispose the Database Istance Previusly Created
        /// </summary>
        public void Dispose() => Dispose(true);

        #endregion

        #region Database Core Methods

        /// <summary>
        /// Method used to manage all the connection's states
        /// </summary>
        /// <param name="operationType"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private bool ManageConnection(InternalOperationType operationType)
        {
            try
            {
                if (_dbConn == null) throw new Exception("The database connection is not setted, impossible to establish a proper connection!");
                switch (operationType)
                {
                    case InternalOperationType.OpenConnection:
                        if (_dbConn.State == ConnectionState.Open) throw new Exception("The connection is already open!");
                        if (_dbConn.State == ConnectionState.Executing) throw new Exception("The connection is currently executing operations!");
                        if (_dbConn.State == ConnectionState.Fetching) throw new Exception("The connection is fetching data!");
                        if (_dbConn.State == ConnectionState.Connecting) throw new Exception("The connection is tring to connecting to the database istance!");
                        _dbConn.Open();
                        break;
                    case InternalOperationType.CloseConnection:
                        if (_dbConn.State == ConnectionState.Closed) throw new Exception("The connection is already closed!");
                        if (_dbConn.State == ConnectionState.Executing) throw new Exception("The connection is currently executing operations!");
                        if (_dbConn.State == ConnectionState.Fetching) throw new Exception("The connection is currently fetching data!");
                        _dbConn.Close();
                        break;
                }
                return true;
            }
            catch (DatabaseException ex)
            {
                //
                // ToDo: Write the error into the system events
                //
                return false;
            }
        }

        /// <summary>
        /// Method used to check the current connection's state
        /// </summary>
        /// <returns></returns>
        private bool ManageConnectionState()
        {
            bool connectionStatus = false;
            switch (_dbConn.State)
            {
                case ConnectionState.Open:
                    connectionStatus = true;
                    break;
                case ConnectionState.Connecting:
                    connectionStatus = false;
                    break;
                case ConnectionState.Executing:
                    connectionStatus = true;
                    break;
                case ConnectionState.Fetching:
                    connectionStatus = true;
                    break;
                case ConnectionState.Broken:
                    connectionStatus = false;
                    break;
                case ConnectionState.Closed:
                    connectionStatus = false;
                    break;
            }
            return connectionStatus;
        }

        /// <summary>
        /// Method used to manage all the transaction's states 
        /// </summary>
        /// <param name="operationType"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private bool ManageTransaction(InternalOperationType operationType)
        {
            try
            {
                if (_dbConn == null) throw new Exception("The database connection is not setted, impossible to establish a proper connection!");
                switch (operationType)
                {
                    case InternalOperationType.BeginTransaction:
                        _dbTran = _dbConn.BeginTransaction();
                        break;
                    case InternalOperationType.RollBackTransaction:
                        if (_dbTran == null) throw new Exception("The database's transaction is not setted, impossible to rollback all the operations!");
                        _dbTran.Rollback();
                        _dbTran = null;
                        break;
                    case InternalOperationType.CommitTransaction:
                        if (_dbTran == null) throw new Exception("The database's transaction is not setted, impossible to rollback all the operations!");
                        _dbTran.Commit();
                        _dbTran = null;
                        break;
                }
                return true;
            }
            catch (DatabaseException ex)
            {
                //
                // ToDo: Write the error into the system events
                //
                return false;
            }
        }

        /// <summary>
        /// Method used to manage the activation of the Audit Mode
        /// </summary>
        /// <param name="operationType"></param>
        /// <param name="auditUser"></param>
        /// <returns></returns>
        private bool ManageAuditMode(InternalOperationType operationType, string? userForAuditMode)
        {
            try
            {
                switch (operationType)
                {
                    case InternalOperationType.EnableAuditMode:
                        _auditMode = true;
                        if (!string.IsNullOrEmpty(userForAuditMode)) _auditUser = userForAuditMode;
                        break;
                    case InternalOperationType.DisableAuditMode:
                        _auditMode = false;
                        break;
                }
                return true;
            }
            catch (DatabaseException ex)
            {
                //
                // ToDo: Write the error into the system events
                //
                return false;
            }
        }

        /// <summary>
        /// Generate a new Database Command based on the actual Database's Provider
        /// </summary>
        /// <param name="connection">Contains the active database connection</param>
        /// <param name="commandType">Contains the database command's type</param>
        /// <param name="sqlQuery">Contains the actual sql query</param>
        /// <param name="args">Contains all the sql query's parameters values</param>
        /// <returns></returns>
        private IDbCommand CreateCommand(CommandType commandType, string sqlQuery, params object[] args)
        {
            IDbCommand command = _dbConn.CreateCommand();
            try
            {
                command.CommandType = commandType;
                command.CommandText = sqlQuery;
                switch (commandType)
                {
                    case CommandType.Text:
                        //
                        // TODO: 
                        //
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
                }
                DatabaseUtility.AddParameters(_dbProv, ref command, args);
            }
            catch (DatabaseException ex)
            {
                //
                // ToDo: Export this error outside this methods and track it
                //
            }
            return command;
        }

        /// <summary>
        /// Invoke the DB Scalar command in the Database Istance and return a single value of a specific object type
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the SQL object</param>
        /// <returns>A value based of the object type</returns>
        /// <exception cref="Exception"></exception>
        internal T ExecuteScalar<T>(SqlBuilder sql)
        {
            T? objResult = default;
            IDbCommand cmd = CreateCommand(CommandType.Text, sql.Sql, sql.Params);

            // Audit Parameters
            AuditLevels auditLevel = AuditLevels.Info;
            String? auditErrorMsg = String.Empty;

            if (_dbProv == null || _dbConn == null || cmd == null || sql == null || String.IsNullOrEmpty(sql.Sql)) return objResult;

            _lastCommand = sql.Sql;
            _lastSql = DatabaseUtility.FormatSQLCommandQuery(_dbProv.VariablePrefix(), sql.Sql, sql.Params);

            try
            {
                OpenConnection();

                try
                {
                    // Verify if the provided connection is open, otherwise generate a new exception
                    if (_dbConn == null) throw new Exception("The database object is not initialize!");

                    // Execute the provided command
                    using (cmd)
                    {
                        if (_dbTran != null) cmd.Transaction = _dbTran;
                        objResult = (T)cmd.ExecuteScalar();
                    }
                }
                catch (DatabaseException ex)
                {
                    throw new Exception(ex.Message, ex);
                }
            }
            catch (DatabaseException ex)
            {
                // Add to info of the last executed command after an error
                ex.ErrorMSG = String.Concat("[Scalar Command Error]: ", ex.Message);
                auditErrorMsg = ex.Message;
                auditLevel = AuditLevels.Error;
                objResult = default;
            }
            finally
            {
                CloseConnection();
            }

            // Add a new audit event inside the database table if the mode is enable
            if (!_auditExec) NewAuditEvent(auditLevel, DBCommandType.Text, DatabaseUtility.DefineSQLCommandType(sql.Sql), auditErrorMsg);

            return objResult;
        }

        /// <summary>
        /// Invoke the DB NonQuery command in the Database Istance and return the number of completed operations
        /// </summary>
        /// <param name="sql">Contains the SQL object</param>
        /// <returns>An int value that count all the affected table rows</returns>
        /// <exception cref="Exception"></exception>
        internal int ExecuteNonQuery(SqlBuilder sql)
        {
            int affectedRowOnCommand = 0;
            IDbCommand cmd = CreateCommand(CommandType.Text, sql.Sql, sql.Params);

            // Audit Parameters
            AuditLevels auditLevel = AuditLevels.Info;
            String? auditErrorMsg = String.Empty;

            if (_dbProv == null || _dbConn == null || cmd == null || sql == null || String.IsNullOrEmpty(sql.Sql)) return 0;

            _lastCommand = sql.Sql;
            _lastSql = DatabaseUtility.FormatSQLCommandQuery(_dbProv.VariablePrefix(), sql.Sql, sql.Params);

            try
            {
                OpenConnection();

                try
                {
                    if (_dbConn == null) throw new Exception("The database object is not initialize!");

                    using (cmd)
                    {
                        if (_dbTran != null) cmd.Transaction = _dbTran;
                        affectedRowOnCommand = cmd.ExecuteNonQuery();
                    }
                }
                catch (DatabaseException ex)
                {
                    throw new Exception(ex.Message, ex);
                }
            }
            catch (DatabaseException ex)
            {
                // Add to info of the last executed command after an error
                ex.ErrorMSG = String.Concat("[Execute Command Error]: ", ex.Message);
                auditErrorMsg = ex.ErrorMSG;
                auditLevel = AuditLevels.Error;
                affectedRowOnCommand = -1;
            }
            finally
            {
                CloseConnection();
            }

            // Add a new audit event inside the database table if the mode is enable
            if (!_auditExec) NewAuditEvent(auditLevel, DBCommandType.Text, DatabaseUtility.DefineSQLCommandType(sql.Sql), auditErrorMsg);

            return affectedRowOnCommand;
        }

        /// <summary>
        /// Invoke the DB Query command in the Database Istance and cast it to a specific object type
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the SQL object</param>
        /// <returns>A value based of the object type</returns>
        /// <exception cref="Exception"></exception>
        internal List<T> ExecuteQuery<T>(SqlBuilder sql, LothiumObject lothiumObject = null)
        {
            Type type = typeof(T);

            // Audit Parameters
            AuditLevels auditLevel = AuditLevels.Info;
            String? auditErrorMsg = String.Empty;

            var cmd = CreateCommand(CommandType.Text, sql.Sql, sql.Params);
            var list = new List<T>();

            if (_dbProv == null || _dbConn == null || cmd == null || sql == null || String.IsNullOrEmpty(sql.Sql)) return null;

            _lastCommand = sql.Sql;
            _lastSql = DatabaseUtility.FormatSQLCommandQuery(_dbProv.VariablePrefix(), sql.Sql, sql.Params);

            try
            {
                OpenConnection();

                try
                {
                    if (_dbConn == null) throw new Exception("The database object is not initialize!");

                    // Check if exist a lothium object, if not will istance a new one
                    if (lothiumObject == null) lothiumObject = new LothiumObject(typeof(T));

                    using (cmd)
                    {
                        if (_dbTran != null) cmd.Transaction = _dbTran;

                        IDataReader cmdReader = cmd.ExecuteReader();
                        while (cmdReader.Read())
                        {
                            if (cmdReader.FieldCount > 0)
                            {
                                T? obj = (T)Activator.CreateInstance(typeof(T));

                                PropertyInfo[] propertyInfo = LothiumDataInfo.GetProperties(type);
                                foreach (PropertyInfo property in propertyInfo)
                                {
                                    string pName = property.Name;
                                    if (lothiumObject != null) pName = lothiumObject.columnInfo[property.Name].ColumnName;

                                    var value = cmdReader[pName];
                                    if (value == DBNull.Value) value = null;
                                    if (value != null) property.SetValue(obj, cmdReader[pName], null);
                                }

                                list.Add(obj);
                            }
                        }
                    }
                }
                catch (DatabaseException ex)
                {
                    throw new Exception(ex.Message, ex);
                }    
            }
            catch (DatabaseException ex)
            {
                // Add to info of the last executed command after an error
                ex.ErrorMSG = String.Concat("[Query Command Error]: ", ex.Message);
                auditErrorMsg = ex.ErrorMSG;
                auditLevel = AuditLevels.Error;
                list = null;
            }
            finally
            {
                CloseConnection();
            }

            // Add a new audit info event inside the database table if the mode is enable
            if (!_auditExec) NewAuditEvent(auditLevel, DBCommandType.Text, DatabaseUtility.DefineSQLCommandType(sql.Sql), auditErrorMsg);

            return list;
        }

        #endregion

        // Connection & Transaction Methods

        #region Connection & Transaction Managing Methods

        /// <summary>
        /// Open a new Database Connection for the selected Database's Provider
        /// </summary>
        public void OpenConnection()
        {
            bool success = ManageConnection(InternalOperationType.OpenConnection);
            if (!success)
            {
                if (!_auditExec) NewAuditEvent(
                    AuditLevels.Fatal,
                    DBCommandType.Text,
                    SqlCommandType.None,
                    "[Open Connection Fatal Error]: impossible to complete the desire operation"
                );

                //
                // ToDo: Writing the error inside the system events
                //
            }
        }

        /// <summary>
        /// Close the active Database Connection for the selected Database's Provider
        /// </summary>
        public void CloseConnection()
        {
            bool success = ManageConnection(InternalOperationType.CloseConnection);
            if (!success)
            {
                if (!_auditExec) NewAuditEvent(
                    AuditLevels.Fatal,
                    DBCommandType.Text,
                    SqlCommandType.None,
                    "[Close Connection Fatal Error]: impossible to complete the desire operation"
                );

                //
                // ToDo: Writing the error inside the system events
                //
            }
        }

        /// <summary>
        /// Start a new Database Transaction for the Open Connection for the selected Database's Provider
        /// </summary>
        public void BeginTransaction()
        {
            bool success = ManageTransaction(InternalOperationType.BeginTransaction);
            if (!success)
            {
                if (!_auditExec) NewAuditEvent(
                    AuditLevels.Fatal,
                    DBCommandType.Text,
                    SqlCommandType.None,
                    "[Begin Transaction Fatal Error]: impossible to complete the desire operation"
                );

                //
                // ToDo: Writing the error inside the system events
                //
            }
        }

        /// <summary>
        /// Close the active Database Transaction for the Open Connection for the selected Database's Provider
        /// </summary>
        public void CommitTransaction()
        {
            bool success = ManageTransaction(InternalOperationType.CommitTransaction);
            if (!success)
            {
                if (!_auditExec) NewAuditEvent(
                    AuditLevels.Fatal,
                    DBCommandType.Text,
                    SqlCommandType.None,
                    "[Commit Transaction Fatal Error]: impossible to complete the desire operation"
                );

                //
                // ToDo: Writing the error inside the system events
                //
            }
        }

        /// <summary>
        /// Revert all the operations executed during the active Database Transaction for the Open Connection for the selected Database's Provider
        /// </summary>
        public void RollbackTransaction()
        {
            bool success = ManageTransaction(InternalOperationType.RollBackTransaction);
            if (!success)
            {
                if (!_auditExec) NewAuditEvent(
                    AuditLevels.Fatal,
                    DBCommandType.Text,
                    SqlCommandType.None,
                    "[Rollback Transaction Fatal Error]: impossible to complete the desire operation"
                );

                //
                // ToDo: Writing the error inside the system events
                //
            }
        }

        #endregion

        // Base Methods

        #region Scalar, Execute, Query Commands

        /// <summary>
        /// Invoke the DB Scalar command in the Database Istance and return a single value of a specific object type
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the SQL object</param>
        /// <returns>A value based of the object type</returns>
        public T Scalar<T>(SqlBuilder sql) => ExecuteScalar<T>(sql);

        /// <summary>
        /// Invoke the DB Scalar command in the Database Istance and return a single value of a specific object type
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the query command to be executed</param>
        /// <param name="args">Contains all the extra arguments of the query</param>
        /// <returns>A value based of the object type</returns>
        public T Scalar<T>(string sql, params object[] args) => Scalar<T>(new SqlBuilder(sql, args));

        /// <summary>
        /// Invoke the DB NonQuery command in the Database Istance and return the number of completed operations
        /// </summary>
        /// <param name="sql">Contains the SQL object</param>
        /// <returns>An int value that count all the affected table rows</returns>
        public int Execute(SqlBuilder sql) => ExecuteNonQuery(sql);

        /// <summary>
        /// Invoke the DB NonQuery command in the Database Istance and return the number of completed operations
        /// </summary>
        /// <param name="sql">Contains the query command to be executed</param>
        /// <param name="args">Contains all the extra arguments of the query</param>
        /// <returns>An int value that count all the affected table rows</returns>
        public int Execute(string sql, params object[] args) => Execute(new SqlBuilder(sql, args));

        /// <summary>
        /// Invoke the DB Query command in the Database Istance and cast it to a specific object type
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the SQL object</param>
        /// <returns>A value based of the object type</returns>
        public List<T> Query<T>(SqlBuilder sql) => ExecuteQuery<T>(sql);

        /// <summary>
        /// Invoke the DB Query command in the Database Istance and cast it to a specific object type
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the query command to be executed</param>
        /// <param name="args">Contains all the extra arguments of the query</param>
        /// <returns>A value based of the object type</returns>
        public List<T> Query<T>(string sql, params object[] args) => Query<T>(new SqlBuilder(sql, args));

        #endregion

        // Audit Methods

        #region Audit Methods

        /// <summary>
        /// Methods that enable the audit mode for every single database operation
        /// </summary>
        /// <param name="userForAudit">Contains a specific user, if null will use the default one</param>
        public void EnableAuditMode(string? userForAudit = null)
            => ManageAuditMode(InternalOperationType.EnableAuditMode, userForAudit);

        /// <summary>
        /// Methods that disable the audit mode for every single database operation
        /// </summary>
        public void DisableAuditMode()
            => ManageAuditMode(InternalOperationType.DisableAuditMode, null);

        /// <summary>
        /// Methods that add a new Audit Event inside the database dedicated table
        /// </summary>
        /// <param name="level">Contains the level of the current action</param>
        /// <param name="cmdType">Contains the type of the current action's command</param>
        /// <param name="sqlType">Contains the type of the current action's query</param>
        internal void NewAuditEvent(AuditLevels level, DBCommandType cmdType, SqlCommandType sqlType, string? errorMsg = null)
        {
            if (_auditMode)
            {
                if (String.IsNullOrEmpty(LastCommand) && String.IsNullOrEmpty(LastSql)) return;

                // Set a true the audit action flag
                _auditExec = true;

                // Verify if the audit table already exists, if not will create it
                if (Scalar<int>(AuditModel.GenerateQueryForCheckTableExist()) != 1) Execute(AuditModel.GenerateQueryForTableCreation());

                // Insert the new event inside the audit table
                Insert(new AuditModel()
                {
                    Level = Enum.GetName(level.GetType(), level),
                    User = _auditUser,
                    ExecutedOnDate = DateTime.Now,
                    DatabaseCommandType = Enum.GetName(cmdType.GetType(), cmdType),
                    SqlCommandType = Enum.GetName(sqlType.GetType(), sqlType),
                    SqlCommandWithoutParams = LastCommand,
                    SqlCommandWithParams = LastSql,
                    ErrorMsg = errorMsg
                });

                // Set a false the audit action flag
                _auditExec = false;
            }
        }

        #endregion

        // Extended Methods

        #region FetchAll Methods

        /// <summary>
        /// Select all the elements inside a table without specify the Sql query
        /// </summary>
        /// <returns>A value based of the object type</returns>
        public List<T> FetchAll<T>()
            => Query<T>(AutoQueryGenerator.GenerateAutoSelectClauseFromPocoObject(new SqlBuilder(string.Empty), typeof(T)).Sql);

        /// <summary>
        /// Select all the elements inside a table with a specify Sql query
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the SQL object</param>
        /// <returns>A value based of the object type</returns>
        public List<T> FetchAll<T>(SqlBuilder sql)
        {
            if (sql == null || String.IsNullOrEmpty(sql.Sql)) return FetchAll<T>();
            return Query<T>(AutoQueryGenerator.GenerateAutoSelectClauseFromPocoObject(sql, typeof(T)));
        }

        /// <summary>
        /// Select all the elements inside a table with a specify Sql query
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the query command to be executed</param>
        /// <param name="args">Contains all the extra arguments of the query</param>
        /// <returns>A value based of the object type</returns>
        public List<T> FetchAll<T>(string sql, params object[] args)
            => FetchAll<T>(new SqlBuilder(sql, args));

        #endregion

        #region FetchSingle Methods

        /// <summary>
        /// Select a single specific element inside a table
        /// </summary>
        /// <param name="sql">Contains the SQL object</param>
        /// <returns>A value based of the object type</returns>
        public T FetchSingle<T>(SqlBuilder sql)
        {
            if (sql == null || String.IsNullOrEmpty(sql.Sql)) return default(T);
            return FetchAll<T>(AutoQueryGenerator.GenerateAutoSelectClauseFromPocoObject(sql, typeof(T), 1)).FirstElement();
        }

        /// <summary>
        /// Select a single specific element inside a table
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the query command to be executed</param>
        /// <param name="args">Contains all the extra arguments of the query</param>
        /// <returns>A value based of the object type</returns>
        /// <returns></returns>
        public T FetchSingle<T>(string sql, params object[] args)
            => FetchSingle<T>(new SqlBuilder(sql, args));

        #endregion

        #region FetchPage Methods

        /// <summary>
        /// Generate a Paging List from a PageObject
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="page">Contains the page object</param>
        /// <returns>A value based of the object type</returns>
        public List<T> FetchPage<T>(PageObject<T> page)
        {
            var lothiumObj = new LothiumObject(typeof(T));
            var sqlAutoSelect = AutoQueryGenerator.GenerateAutoSelectClauseFromPocoObject(new SqlBuilder("WHERE 1=1"), typeof(T));
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
            => Query<T>(_dbProv.BuildPageQuery<T>(page, sql));

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

        // Object Base Methods

        #region Insert Methods

        /// <summary>
        /// Insert the passed object inside a table of the database in the form of a row
        /// </summary>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Insert(object obj) => Execute(AutoQueryGenerator.GenerateInsertClauseFromPocoObject(_dbProv, obj));

        /// <summary>
        /// Insert the passed object inside a table of the database in the form of a row
        /// </summary>
        /// <param name="table">Contains the name of the table</param>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Insert(string table, object obj) => Execute(AutoQueryGenerator.GenerateInsertClauseFromPocoObject(_dbProv, obj, table));

        #endregion

        #region Update Methods

        /// <summary>
        /// Update a number of element inside a table of the database
        /// </summary>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Update(object obj) => Execute(AutoQueryGenerator.GenerateUpdateClauseFromPocoObject(_dbProv, obj));

        /// <summary>
        /// Update a number of element inside a table of the database
        /// </summary>
        /// <param name="table">Contains the name of the table</param>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Update(string table, object obj) => Execute(AutoQueryGenerator.GenerateUpdateClauseFromPocoObject(_dbProv, obj));

        #endregion

        #region Delete Methods

        /// <summary>
        /// Delete a number of element inside a table of the database
        /// </summary>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Delete(object obj) => Execute(AutoQueryGenerator.GenerateDeleteClauseFromPocoObject(_dbProv, obj));

        /// <summary>
        /// Delete a number of element inside a table of the database
        /// </summary>
        /// <param name="table">Contains the name of the table</param>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Delete(string table, object obj) => Execute(AutoQueryGenerator.GenerateDeleteClauseFromPocoObject(_dbProv, obj, table));

        #endregion
    }

    /// <summary>
    /// Extended Database Class
    /// </summary>
    /// <typeparam name="TDbProvider"></typeparam>
    public class Database<TDbProvider> : Database where TDbProvider : IDbProvider
    {
        public Database(string connectionString = "") : base(DatabaseUtility.InizializeProviderByName(typeof(TDbProvider).Name), connectionString) { }

        public Database(params object[] args) : base(DatabaseUtility.InizializeProviderByName(typeof(TDbProvider).Name), args) { }
    }
}

