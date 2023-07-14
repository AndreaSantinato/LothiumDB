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
using LothiumDB.DatabaseExceptions;
using LothiumDB.Exceptions;

namespace LothiumDB
{
    /// <summary>
    /// Default Database Class of the library, provides basic operations to the connected istance
    /// </summary>
    public class Database : IDatabase, IDisposable
    {
        // Properties (Public & Private)

        #region Connection Properties

        /// <summary>
        /// Private Property For The Database Current DB's Connection
        /// </summary>
        protected IDbConnection? _dbConn = null;

        /// <summary>
        /// Contains the database connection's state
        /// </summary>
        public IDbConnection? Connection => _dbConn;

        #endregion
        #region Transaction Properties

        /// <summary>
        /// Private Property For The Database Current DB's Connection's Transaction
        /// </summary>
        protected IDbTransaction? _dbTran = null;

        /// <summary>
        /// Contains the database's provider
        /// </summary>
        public IDbProvider? DbProvider => _dbProv;

        #endregion
        #region Provider Property

        /// <summary>
        /// Private Property For The Database Current DB's Connection's Provider
        /// </summary>
        protected IDbProvider? _dbProv = null;

        #endregion
        #region LastSql Properties

        /// <summary>
        /// Contains the last complete executed Sql Query
        /// </summary>
        private String? _lastSql = String.Empty;

        /// <summary>
        /// Contains the complete last executed query with all parameters replace
        /// </summary>
        public String? LastSql => _lastSql;

        #endregion
        #region LastCommand Properties

        /// <summary>
        /// Contains the last command executed (Sql Query without parameters replacing)
        /// </summary>
        private String? _lastCommand = String.Empty;

        /// <summary>
        /// Contains the last executed query without parameters replace
        /// </summary>
        public String? LastCommand => _lastCommand;

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
            _dbConn = DatabaseUtility.GenerateConnection(provider.ProviderType(), connectionString);
            _dbConn.ConnectionString = connectionString;
            _dbProv = provider;
            _dbTran = null;
        }

        /// <summary cref="Database">
        /// By Default this method will inizialize a new Microsoft SQL Server database communication's istance
        /// </summary>
        /// <param name="connectionString">Contains the connection string</param>
        public Database(string connectionString) : this(new MSSqlServerDbProvider(), connectionString) { }

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
        /// Open a new Database Connection for the selected Database's Provider
        /// </summary>
        protected void OpenConnection()
        {
            bool success = DatabaseExtensions.ManageConnection(InternalOperationType.OpenConnection, ref _dbConn);
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
        protected void CloseConnection()
        {
            bool success = DatabaseExtensions.ManageConnection(InternalOperationType.CloseConnection, ref _dbConn);
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
            bool success = DatabaseExtensions.ManageTransaction(InternalOperationType.BeginTransaction, ref _dbConn, ref _dbTran);
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
            bool success = DatabaseExtensions.ManageTransaction(InternalOperationType.CommitTransaction, ref _dbConn, ref _dbTran);
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
            bool success = DatabaseExtensions.ManageTransaction(InternalOperationType.RollBackTransaction, ref _dbConn, ref _dbTran);
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

        /// <summary>
        /// Verify if the current database's connection is open
        /// </summary>
        /// <returns>Return True If The Connection's Status Is Open</returns>
        public bool IsConnectionOpen() => DatabaseExtensions.ManageConnectionState(_dbConn);

        /// <summary>
        /// Verify if the current database's connection is closed
        /// </summary>
        /// <returns>Return True If The Connection's Status Is Closed</returns>
        public bool IsConnectionClosed() => DatabaseExtensions.ManageConnectionState(_dbConn);

        #endregion

        // Base Methods

        #region Scalar, NonQuery, Query Commands

        /// <summary>
        /// Invoke the DB Scalar command in the Database Istance and return a single value of a specific object type
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the SQL object</param>
        /// <returns>A value based of the object type</returns>
        public T Scalar<T>(SqlBuilder sql)
        {
            T? objResult = default;
            IDbCommand cmd = DatabaseExtensions.CreateCommand(_dbProv, _dbConn, CommandType.Text, sql.Sql, sql.Params);
            LothiumDataInfo? dataInfo = new LothiumDataInfo(typeof(T));
            
            // Audit Parameters
            AuditLevels auditLevel = AuditLevels.Info;
            String? auditErrorMsg = String.Empty;

            if (_dbProv == null || _dbConn == null || cmd == null || sql == null || String.IsNullOrEmpty(sql.Sql)) return objResult;

            _lastCommand = sql.Sql;
            _lastSql = DatabaseUtility.FormatSQLCommandQuery(_dbProv.VariablePrefix(), sql.Sql, sql.Params);

            try
            {
                if (!DatabaseExtensions.ManageConnectionState(_dbConn))
                    DatabaseExtensions.ManageConnection(InternalOperationType.OpenConnection, ref _dbConn);

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
                // Add to info of the last executed command after an error
                ex.ErrorMSG = String.Concat("[Scalar Command Error]: ", ex.Message);
                auditErrorMsg = ex.Message;
                auditLevel = AuditLevels.Error;
            }
            finally
            {
                if (DatabaseExtensions.ManageConnectionState(_dbConn))
                    DatabaseExtensions.ManageConnection(InternalOperationType.CloseConnection, ref _dbConn);
            }

            // Add a new audit event inside the database table if the mode is enable
            if (!_auditExec) NewAuditEvent(auditLevel, DBCommandType.Text, DatabaseUtility.DefineSQLCommandType(sql.Sql), auditErrorMsg);

            return objResult;
        }

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
        public int Execute(SqlBuilder sql)
        {
            int affectedRowOnCommand = 0;
            IDbCommand cmd = DatabaseExtensions.CreateCommand(_dbProv, _dbConn, CommandType.Text, sql.Sql, sql.Params);

            // Audit Parameters
            AuditLevels auditLevel = AuditLevels.Info;
            String? auditErrorMsg = String.Empty;

            if (_dbProv == null || _dbConn == null || cmd == null || sql == null || String.IsNullOrEmpty(sql.Sql)) return 0;

            _lastCommand = sql.Sql;
            _lastSql = DatabaseUtility.FormatSQLCommandQuery(_dbProv.VariablePrefix(), sql.Sql, sql.Params);

            try
            {
                if (!DatabaseExtensions.ManageConnectionState(_dbConn))
                    DatabaseExtensions.ManageConnection(InternalOperationType.OpenConnection, ref _dbConn);

                if (_dbConn == null) throw new Exception("The database object is not initialize!");

                using (cmd)
                {
                    if (_dbTran != null) cmd.Transaction = _dbTran;
                    affectedRowOnCommand = cmd.ExecuteNonQuery();
                }
            }
            catch (DatabaseException ex)
            {
                // Add to info of the last executed command after an error
                ex.ErrorMSG = String.Concat("[Execute Command Error]: ", ex.Message);
                auditErrorMsg = ex.ErrorMSG;
                auditLevel = AuditLevels.Error;
            }
            finally
            {
                if (DatabaseExtensions.ManageConnectionState(_dbConn))
                    DatabaseExtensions.ManageConnection(InternalOperationType.CloseConnection, ref _dbConn);
            }

            // Add a new audit event inside the database table if the mode is enable
            if (!_auditExec) NewAuditEvent(auditLevel, DBCommandType.Text, DatabaseUtility.DefineSQLCommandType(sql.Sql), auditErrorMsg);

            return affectedRowOnCommand;
        }

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
        public List<T> Query<T>(SqlBuilder sql)
        {
            LothiumDataInfo? dataInfo = new LothiumDataInfo(typeof(T));
            Type type = typeof(T);

            // Audit Parameters
            AuditLevels auditLevel = AuditLevels.Info;
            String? auditErrorMsg = String.Empty;

            var cmd = DatabaseExtensions.CreateCommand(_dbProv, _dbConn, CommandType.Text, sql.Sql, sql.Params);
            var list = new List<T>();

            if (_dbProv == null || _dbConn == null || cmd == null || sql == null || String.IsNullOrEmpty(sql.Sql)) return null;

            _lastCommand = sql.Sql;
            _lastSql = DatabaseUtility.FormatSQLCommandQuery(_dbProv.VariablePrefix(), sql.Sql, sql.Params);

            try
            {
                if (!DatabaseExtensions.ManageConnectionState(_dbConn))
                    DatabaseExtensions.ManageConnection(InternalOperationType.OpenConnection, ref _dbConn);

                if (_dbConn == null) throw new Exception("The database object is not initialize!");

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
                                if (dataInfo != null) pName = dataInfo.TableColumns[property.Name].ToString();

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
                // Add to info of the last executed command after an error
                ex.ErrorMSG = String.Concat("[Query Command Error]: ", ex.Message);
                auditErrorMsg = ex.ErrorMSG;
                auditLevel = AuditLevels.Error;
            }
            finally
            {
                if (DatabaseExtensions.ManageConnectionState(_dbConn))
                    DatabaseExtensions.ManageConnection(InternalOperationType.CloseConnection, ref _dbConn);
            }

            // Add a new audit info event inside the database table if the mode is enable
            if (!_auditExec) NewAuditEvent(auditLevel, DBCommandType.Text, DatabaseUtility.DefineSQLCommandType(sql.Sql), auditErrorMsg);

            return list;
        }

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
            => DatabaseExtensions.ManageAuditMode(InternalOperationType.EnableAuditMode, userForAudit, ref _auditMode, ref _auditUser);

        /// <summary>
        /// Methods that disable the audit mode for every single database operation
        /// </summary>
        public void DisableAuditMode()
            => DatabaseExtensions.ManageAuditMode(InternalOperationType.DisableAuditMode, null, ref _auditMode, ref _auditUser);

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
        public List<T> FetchAll<T>() => Query<T>(DatabaseUtility.GenerateAutoSelectClause(string.Empty, new LothiumDataInfo(typeof(T))).Sql);

        /// <summary>
        /// Select all the elements inside a table with a specify Sql query
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the query command to be executed</param>
        /// <param name="args">Contains all the extra arguments of the query</param>
        /// <returns>A value based of the object type</returns>
        public List<T> FetchAll<T>(string sql, params object[] args)
        {
            if (!sql.Contains("SELECT") && !sql.Contains("FROM"))
            {
                return Query<T>(DatabaseUtility.GenerateAutoSelectClause(sql, new LothiumDataInfo(typeof(T)), args));
            }

            var query = new SqlBuilder(sql, args);
            return Query<T>(query);
        }

        /// <summary>
        /// Select all the elements inside a table with a specify Sql query
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the SQL object</param>
        /// <returns>A value based of the object type</returns>
        public List<T> FetchAll<T>(SqlBuilder sql) => FetchAll<T>(sql.Sql, sql.Params);

        /// <summary>
        /// Select all the elements inside a table with a specify Sql query
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the query command to be executed</param>
        /// <param name="offset">Contains the number of element to skip from the select</param>
        /// <param name="element">Contains the number of the element to select</param>
        /// <param name="args">Contains all the extra arguments of the query</param>
        /// <returns>A value based of the object type</returns>
        public List<T> FetchAll<T>(string sql, long offset, long element, params object[] args)
        {
            var dInfo = new LothiumDataInfo(typeof(T));
            var autoSql = DatabaseUtility.GenerateAutoSelectClause(new SqlBuilder().OrderBy(string.Join(", ", (from x in dInfo.PrimaryKeys select x.PrimaryKey).ToArray())).Sql, dInfo);
            return Query<T>(new SqlBuilder(DbProvider.BuildPageQuery(autoSql.Sql, offset, element)));
        }

        /// <summary>
        /// Select all the elements inside a table with a specify Sql query
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the SQL object</param>
        /// <param name="offset">Contains the number of element to skip from the select</param>
        /// <param name="element">Contains the number of the element to select</param>
        /// <returns>A value based of the object type</returns>
        public List<T> FetchAll<T>(SqlBuilder sql, long offset, long element) => FetchAll<T>(sql.Sql, offset, element, sql.Params);

        /// <summary>
        /// Select all the elements inside a table with a specify Sql query
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="offset">Contains the number of element to skip from the select</param>
        /// <param name="element">Contains the number of the element to select</param>
        /// <returns>A value based of the object type</returns>
        public List<T> FetchAll<T>(long offset, long element) => FetchAll<T>(new SqlBuilder(), offset, element).ToList();

        #endregion

        #region SingleFetch Methods

        /// <summary>
        /// Select a single specific element inside a table
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the query command to be executed</param>
        /// <param name="args">Contains all the extra arguments of the query</param>
        /// <returns>A value based of the object type</returns>
        /// <returns></returns>
        public T SingleFetch<T>(string sql, params object[] args)
        {
            SqlBuilder? sqlQuery = null;

            if (!sql.Contains("SELECT") && !sql.Contains("FROM"))
                sqlQuery = DatabaseUtility.GenerateAutoSelectClause(sql, new LothiumDataInfo(typeof(T)), 1, args);
            else
                sqlQuery = new SqlBuilder(sql, args);

            return Query<T>(sqlQuery)[0];
        }

        /// <summary>
        /// Select a single specific element inside a table
        /// </summary>
        /// <param name="sql">Contains the SQL object</param>
        /// <returns>A value based of the object type</returns>
        public T SingleFetch<T>(SqlBuilder sql) => SingleFetch<T>(sql.Sql, sql.Params);

        #endregion

        // Object Base Methods

        #region Insert Methods

        /// <summary>
        /// Insert the passed object inside a table of the database in the form of a row
        /// </summary>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Insert(object obj)
            => Execute(AutoQueryGenerator.GenerateInsertClause(_dbProv, obj));

        /// <summary>
        /// Insert the passed object inside a table of the database in the form of a row
        /// </summary>
        /// <param name="table">Contains the name of the table</param>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Insert(string table, object obj)
            => Execute(AutoQueryGenerator.GenerateInsertClause(_dbProv, obj, table));

        #endregion

        #region Update Methods

        /// <summary>
        /// Update a number of element inside a table of the database
        /// </summary>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Update(object obj)
            => Execute(AutoQueryGenerator.GenerateUpdateClause(_dbProv, obj));

        /// <summary>
        /// Update a number of element inside a table of the database
        /// </summary>
        /// <param name="table">Contains the name of the table</param>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Update(string table, object obj)
            => Execute(AutoQueryGenerator.GenerateUpdateClause(_dbProv, obj));

        #endregion

        #region Delete Methods

        /// <summary>
        /// Delete a number of element inside a table of the database
        /// </summary>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Delete(object obj)
            => Execute(AutoQueryGenerator.GenerateDeleteClause(_dbProv, obj));

        /// <summary>
        /// Delete a number of element inside a table of the database
        /// </summary>
        /// <param name="table">Contains the name of the table</param>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Delete(string table, object obj)
            => Execute(AutoQueryGenerator.GenerateDeleteClause(_dbProv, obj, table));

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

