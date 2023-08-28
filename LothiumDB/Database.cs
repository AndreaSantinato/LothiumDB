// System Class
using System.Data;
using System.Data.Common;
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
using LothiumDB.Core;

namespace LothiumDB
{
    /// <summary>
    /// Default Database Class of the library, provides basic operations to the connected istance
    /// </summary>
    public class Database : IDatabase, IDisposable
    {
        // Last Sql And Command Properties

        #region LastSql, LastCommand Properties

        private String? _lastSql = String.Empty;
        private String? _lastCommand = String.Empty;

        /// <summary>
        /// Contains the complete last executed query with all parameters replace
        /// </summary>
        public String? LastSql
        {
            get => _lastSql;
            private set => _lastSql = value;
        }

        /// <summary>
        /// Contains the last executed query without parameters replace
        /// </summary>
        public String? LastCommand
        {
            get => _lastCommand;
            private set => _lastCommand = value;
        }

        #endregion

        // Last Generated Exception Properties

        #region Database Exception Property

        private DatabaseException? _lastExcep = null;

        /// <summary>
        /// Contains the last database's generated error
        /// </summary>
        public DatabaseException? LastError { get { return _lastExcep; } private set { _lastExcep = value; } }

        #endregion

        // Query TimeOut Properties

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

        // Dispose Handle Properties

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
            // Set the choosen connectionstring and database's provider
            _dbProv = provider;
            _dbConnection = new DatabaseConnectionObjet(provider, connectionString);

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
        public Database(IDbProvider provider, params object[] args) : this(provider, provider.CreateConnectionString(args)) { }

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
        /// Generate a new Database Command based on the actual Database's Provider
        /// </summary>
        /// <param name="commandType">Contains the database command's type</param>
        /// <param name="sql">Contains the actual sql query and all the parameters values</param>
        /// <returns>A Database's Command</returns>
        private IDbCommand CreateCommand(CommandType commandType, SqlBuilder sql)
        {
            IDbCommand? command = null;

            if (_dbProv == null) throw new ArgumentNullException(nameof(_dbProv));
            if (_dbConnection == null) throw new ArgumentNullException(nameof(_dbConnection));
            if (sql == null) throw new ArgumentNullException(nameof(sql));
            if (String.IsNullOrEmpty(sql.Sql)) throw new ArgumentNullException(nameof(sql.Sql));

            try
            {
                IDbConnection? conn = null;
                IDbTransaction? trann = null;

                if (_dbTransaction != null) 
                {
                    conn = _dbTransaction.Connection;
                    trann = _dbTransaction.Transaction;       
                }
                else
                {
                    conn = _dbConnection.DatabaseConnection;
                    trann = null;
                }

                command = conn.CreateCommand();
                command.Transaction = trann;
                command.CommandText = sql.Sql;
                command.CommandType = commandType;
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
                DatabaseUtility.AddParameters(_dbProv, ref command, sql.Params);
            }
            catch (DatabaseException ex)
            {
                if (!_auditExec)
                {
                    NewAuditEvent
                    (
                        AuditLevels.Fatal,
                        DBCommandType.Text,
                        SqlCommandType.None,
                        "[Create Database's Command Fatal Error]: impossible to complete the operation!!"
                    );
                }
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

            // Audit Parameters
            AuditLevels auditLevel = AuditLevels.Info;
            String? auditErrorMsg = String.Empty;

            _lastCommand = sql.Sql;
            _lastSql = DatabaseUtility.FormatSQLCommandQuery(_dbProv.VariablePrefix(), sql.Sql, sql.Params);

            try
            {
                OpenConnection();

                using (IDbCommand cmd = CreateCommand(CommandType.Text, sql))
                {
                    objResult = (T)cmd.ExecuteScalar();
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
        internal int ExecuteNonQuery(SqlBuilder sql)
        {
            int affectedRowOnCommand = 0;

            // Audit Parameters
            AuditLevels auditLevel = AuditLevels.Info;
            String? auditErrorMsg = String.Empty;

            _lastCommand = sql.Sql;
            _lastSql = DatabaseUtility.FormatSQLCommandQuery(_dbProv.VariablePrefix(), sql.Sql, sql.Params);

            // Main Operations
            try
            {
                OpenConnection();

                using (IDbCommand cmd = CreateCommand(CommandType.Text, sql))
                {
                    affectedRowOnCommand = cmd.ExecuteNonQuery();
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

            var list = new List<T>();

            _lastCommand = sql.Sql;
            _lastSql = DatabaseUtility.FormatSQLCommandQuery(_dbProv.VariablePrefix(), sql.Sql, sql.Params);

            try
            {
                OpenConnection();

                // Check if exist a lothium object, if not will istance a new one
                if (lothiumObject == null) lothiumObject = new LothiumObject(typeof(T));

                using (IDbCommand cmd = CreateCommand(CommandType.Text, sql))
                {
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

        // Connection, Transaction, Provider -> Properties & Methods

        #region Connection Properties & Methods

        internal DatabaseConnectionObjet? _dbConnection = null;

        /// <summary>
        /// Contains the database's connection
        /// </summary>
        public IDbConnection? Connection
        {
            get => _dbConnection?.DatabaseConnection;
        }

        /// <summary>
        /// Open a new Database Connection for the selected Database's Provider
        /// </summary>
        internal void OpenConnection()
        {
            try
            {
                if (_dbTransaction == null) 
                {
                    _dbConnection?.OpenDatabaseConnection();
                }
            }
            catch (DatabaseException ex)
            {
                if (!_auditExec)
                {
                    NewAuditEvent
                    (
                        AuditLevels.Fatal,
                        DBCommandType.Text,
                        SqlCommandType.None,
                        "[Open Connection Fatal Error]: impossible to complete the operation!!"
                    );
                }
            }
        }

        /// <summary>
        /// Close the active Database Connection for the selected Database's Provider
        /// </summary>
        internal void CloseConnection()
        {
            try
            {
                if (_dbTransaction == null)
                {
                    _dbConnection?.CloseDatabaseConnection();
                }
            }
            catch (DatabaseException ex)
            {
                if (!_auditExec)
                {
                    NewAuditEvent
                    (
                        AuditLevels.Fatal,
                        DBCommandType.Text,
                        SqlCommandType.None,
                        "[Close Connection Fatal Error]: impossible to complete the operation!!"
                    );
                }
            }
        }

        #endregion

        #region Transaction Properties & Methods

        internal DatabaseTransactionObjet? _dbTransaction = null;

        /// <summary>
        /// Start a new Database Transaction for the Open Connection for the selected Database's Provider
        /// </summary>
        public void BeginTransaction()
        {
            try
            {
                _dbTransaction = new DatabaseTransactionObjet(_dbConnection);
                _dbTransaction.BeginDatabaseTransaction();
            }
            catch (Exception ex)
            {
                if (!_auditExec)
                {
                    NewAuditEvent
                    (
                        AuditLevels.Fatal,
                        DBCommandType.Text,
                        SqlCommandType.None,
                        "[Begin Transaction Fatal Error]: impossible to complete the operation!!"
                    );
                }
            }
        }

        /// <summary>
        /// Close the active Database Transaction for the Open Connection for the selected Database's Provider
        /// </summary>
        public void CommitTransaction()
        {
            try
            {
                if ( _dbTransaction != null )
                {
                    _dbTransaction.CommitDatabaseTransaction();
                }
            }
            catch (Exception ex)
            {
                if (!_auditExec)
                {
                    NewAuditEvent
                    (
                        AuditLevels.Fatal,
                        DBCommandType.Text,
                        SqlCommandType.None,
                        "[Commit Transaction Fatal Error]: impossible to complete the operation!!"
                    );
                }
            }
        }

        /// <summary>
        /// Revert all the operations executed during the active Database Transaction for the Open Connection for the selected Database's Provider
        /// </summary>
        public void RollbackTransaction()
        {
            try
            {
                if (_dbTransaction != null)
                {
                    _dbTransaction.RollbackDatabaseTransaction();
                }
            }
            catch (Exception ex)
            {
                if (!_auditExec)
                {
                    NewAuditEvent
                    (
                        AuditLevels.Fatal,
                        DBCommandType.Text,
                        SqlCommandType.None,
                        "[Rollback Transaction Fatal Error]: impossible to complete the operation!!"
                    );
                }
            }
        }

        #endregion

        #region Provider Properties

        protected IDbProvider? _dbProv = null;

        /// <summary>
        /// Contains the database's provider
        /// </summary>
        public IDbProvider? Provider
        {
            get => _dbProv;
            private set => _dbProv = value;
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
        public T Scalar<T>(SqlBuilder sql)
            => ExecuteScalar<T>(sql);

        /// <summary>
        /// Invoke the DB Scalar command in the Database Istance and return a single value of a specific object type
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the query command to be executed</param>
        /// <param name="args">Contains all the extra arguments of the query</param>
        /// <returns>A value based of the object type</returns>
        public T Scalar<T>(string sql, params object[] args)
            => Scalar<T>(new SqlBuilder(sql, args));

        /// <summary>
        /// Invoke the DB NonQuery command in the Database Istance and return the number of completed operations
        /// </summary>
        /// <param name="sql">Contains the SQL object</param>
        /// <returns>An int value that count all the affected table rows</returns>
        public int Execute(SqlBuilder sql)
            => ExecuteNonQuery(sql);

        /// <summary>
        /// Invoke the DB NonQuery command in the Database Istance and return the number of completed operations
        /// </summary>
        /// <param name="sql">Contains the query command to be executed</param>
        /// <param name="args">Contains all the extra arguments of the query</param>
        /// <returns>An int value that count all the affected table rows</returns>
        public int Execute(string sql, params object[] args)
            => Execute(new SqlBuilder(sql, args));

        /// <summary>
        /// Invoke the DB Query command in the Database Istance and cast it to a specific object type
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the SQL object</param>
        /// <returns>A value based of the object type</returns>
        public List<T> Query<T>(SqlBuilder sql)
            => ExecuteQuery<T>(sql);

        /// <summary>
        /// Invoke the DB Query command in the Database Istance and cast it to a specific object type
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the query command to be executed</param>
        /// <param name="args">Contains all the extra arguments of the query</param>
        /// <returns>A value based of the object type</returns>
        public List<T> Query<T>(string sql, params object[] args)
            => Query<T>(new SqlBuilder(sql, args));

        #endregion

        // Audit -> Properties & Methods

        #region Audit Properties & Methods

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

        /// <summary>
        /// Methods that enable the audit mode for every single database operation
        /// </summary>
        /// <param name="userForAudit">Contains a specific user, if null will use the default one</param>
        public void EnableAuditMode(string? userForAudit = null)
        {
            _auditMode = true;
            if (!string.IsNullOrEmpty(userForAudit))
            {
                _auditUser = userForAudit;
            }
        }


        /// <summary>
        /// Methods that disable the audit mode for every single database operation
        /// </summary>
        public void DisableAuditMode()
        {
            _auditMode = false;
            _auditUser = String.Empty;
        }

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
            => FetchAll<T>(AutoQueryGenerator.GenerateAutoSelectClauseFromPocoObject(new SqlBuilder(string.Empty), typeof(T)).Sql);

        /// <summary>
        /// Select all the elements inside a table with a specify Sql query
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the SQL object</param>
        /// <returns>A value based of the object type</returns>
        public List<T> FetchAll<T>(SqlBuilder sql)
        {
            if (sql == null) return null;
            if (String.IsNullOrEmpty(sql.Sql)) return null;
            return Query<T>(sql);
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
            if (sql == null) return default(T);
            if (String.IsNullOrEmpty(sql.Sql)) return default(T);
            return FetchAll<T>(sql).FirstElement();
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

