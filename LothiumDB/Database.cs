// System Class
using System.Data;
// Custom Class
using LothiumDB.Core;
using LothiumDB.Core.Helpers;
using LothiumDB.Enumerations;
using LothiumDB.Models;
using LothiumDB.Providers;
using LothiumDB.Extensions;

namespace LothiumDB
{
    /// <summary>
    /// Default Database Class of the library, provides basic operations to the connected instance
    /// </summary>
    public class Database : IDatabase, IDisposable
    {
        private DatabaseConnectionObject _dbConnection;
        private DatabaseTransactionObject? _dbTransaction;
        private Exception? _lastError;
        private int _cmdTimeOut;
        private string? _lastSql;
        private string? _lastCommand;
        private bool _auditMode = false;
        private string _auditUser = "LothiumDB";
        private bool _auditExec = false;

        // Public Properties //
        
        #region Public Properties

        /// <summary>
        /// Contains the database's connection
        /// </summary>
        public IDbConnection? Connection
            => _dbConnection?.DatabaseConnection;

        /// <summary>
        /// Contains the database's provider
        /// </summary>
        public IDatabaseProvider? Provider
            => _dbConnection?.DatabaseProvider;

        /// <summary>
        /// Contains the complete last executed query with all parameters replace
        /// </summary>
        public string? LastSql
        {
            get => _lastSql;
            private set => _lastSql = value;
        }

        /// <summary>
        /// Contains the last executed query without parameters replace
        /// </summary>
        public string? LastCommand
        {
            get => _lastCommand;
            private set => _lastCommand = value;
        }

        /// <summary>
        /// Contains the last database's generated error
        /// </summary>
        public Exception? LastError
        {
            get => _lastError;
            private set => _lastError = value;
        }

        /// <summary>
        /// Contains the database's command timeout value
        /// </summary>
        public int CommandTimeOut
        {
            get => _cmdTimeOut;
            set => _cmdTimeOut = value;
        }

        #endregion

        // Class Core & Base Methods //
        
        #region Class Constructor & Destructor Methods

        public Database(IDatabaseProvider provider, string connectionString)
        {
            // Set the chosen connection string and database's provider
            _dbConnection = new DatabaseConnectionObject(provider, connectionString);

            // Set the remaining properties
            _lastSql = string.Empty;
            _lastCommand = string.Empty;
            _lastError = null;
        }

        public Database(IDatabaseProvider provider, params object[] args)
        {
            // Set the chosen connection string and database's provider
            _dbConnection = new DatabaseConnectionObject(provider, args);

            // Set the remaining properties
            _lastSql = string.Empty;
            _lastCommand = string.Empty;
            _lastError = null;
        }
        
        /// <summary>
        /// Dispose the Database Instance Previously Created
        /// </summary>
        public void Dispose() => GC.SuppressFinalize(this);

        #endregion
        
        #region Database Core Methods

        /// <summary>
        /// Open a new Database Connection for the selected Database's Provider
        /// </summary>
        private void OpenConnection()
        {
            try
            {
                if (_dbTransaction is null) _dbConnection?.OpenDatabaseConnection();
            }
            catch (Exception ex)
            {
                if (!_auditExec)
                {
                    NewAuditEvent
                    (
                        AuditLevelsEnum.Fatal,
                        DBCommandTypeEnum.Text,
                        SqlCommandTypeEnum.None,
                        "[Open Connection Fatal Error]: impossible to complete the operation!!"
                    );
                }
            }
        }

        /// <summary>
        /// Close the active Database Connection for the selected Database's Provider
        /// </summary>
        private void CloseConnection()
        {
            try
            {
                if (_dbTransaction is null) _dbConnection?.CloseDatabaseConnection();
            }
            catch (Exception ex)
            {
                if (!_auditExec)
                {
                    NewAuditEvent
                    (
                        AuditLevelsEnum.Fatal,
                        DBCommandTypeEnum.Text,
                        SqlCommandTypeEnum.None,
                        "[Close Connection Fatal Error]: impossible to complete the operation!!"
                    );
                }
            }
        }

        /// <summary>
        /// Start a new Database Transaction for the Open Connection for the selected Database's Provider
        /// </summary>
        public void BeginTransaction()
        {
            try
            {
                if (_dbConnection is null) throw new ArgumentNullException(nameof(_dbConnection));
                _dbTransaction = new DatabaseTransactionObject(_dbConnection);
                _dbTransaction.BeginDatabaseTransaction();
            }
            catch (Exception ex)
            {
                if (!_auditExec)
                {
                    NewAuditEvent
                    (
                        AuditLevelsEnum.Fatal,
                        DBCommandTypeEnum.Text,
                        SqlCommandTypeEnum.None,
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
                _dbTransaction?.CommitDatabaseTransaction();
            }
            catch (Exception ex)
            {
                if (!_auditExec)
                {
                    NewAuditEvent
                    (
                        AuditLevelsEnum.Fatal,
                        DBCommandTypeEnum.Text,
                        SqlCommandTypeEnum.None,
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
                _dbTransaction?.RollbackDatabaseTransaction();
            }
            catch (Exception ex)
            {
                if (!_auditExec)
                {
                    NewAuditEvent
                    (
                        AuditLevelsEnum.Fatal,
                        DBCommandTypeEnum.Text,
                        SqlCommandTypeEnum.None,
                        "[Rollback Transaction Fatal Error]: impossible to complete the operation!!"
                    );
                }
            }
        }
        
        /// <summary>
        /// Generate a new Database Command based on the actual Database's Provider
        /// </summary>
        /// <param name="commandType">Contains the database command's type</param>
        /// <param name="sql">Contains the actual sql query and all the parameters values</param>
        /// <returns>A Database's Command</returns>
        private IDbCommand? CreateCommand(CommandType commandType, SqlBuilder sql)
        {
            IDbCommand? command = null;

            if (_dbConnection == null) throw new ArgumentNullException(nameof(_dbConnection));
            if (_dbConnection.DatabaseProvider == null) throw new ArgumentNullException(nameof(_dbConnection.DatabaseProvider));
            if (sql == null) throw new ArgumentNullException(nameof(sql));
            if (string.IsNullOrEmpty(sql.SqlQuery)) throw new ArgumentNullException(nameof(sql.SqlQuery));

            try
            {
                command = _dbTransaction is null ? _dbConnection.DatabaseConnection?.CreateCommand() : _dbTransaction.Connection.CreateCommand();
                if (command is null) throw new Exception(nameof(command));
                
                command.Transaction = _dbTransaction?.Transaction;
                command.CommandText = sql.SqlQuery;
                command.CommandType = commandType;

                if (sql.SqlParams.Any())
                {
                    switch (commandType)
                    {
                        case CommandType.Text:
                            DatabaseHelper.AddParamsToDatabaseCommand(
                                _dbConnection.DatabaseProvider,
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
                if (!_auditExec)
                {
                    NewAuditEvent
                    (
                        AuditLevelsEnum.Fatal,
                        DBCommandTypeEnum.Text,
                        SqlCommandTypeEnum.None,
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
        private T ExecuteScalar<T>(SqlBuilder sql)
        {
            T? result = default;

            // Audit Parameters
            var auditLevel = AuditLevelsEnum.Info;
            var auditErrorMsg = string.Empty;

            _lastCommand = sql.SqlQuery;
            _lastSql = DatabaseHelper.FormatSqlCommandQuery(_dbConnection.DatabaseProvider, sql);

            try
            {
                OpenConnection();

                var cmd = CreateCommand(CommandType.Text, sql);
                if (cmd is null) throw new Exception(nameof(cmd));
                
                using (cmd)
                {
                    result = (T)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                // Add to info of the last executed command after an error
                auditErrorMsg = $"[Scalar Command Error]: {ex.Message}";
                auditLevel = AuditLevelsEnum.Error;
                result = default;
            }
            finally
            {
                CloseConnection();
            }

            // Add a new audit event inside the database table if the mode is enable
            if (!_auditExec) NewAuditEvent(auditLevel, DBCommandTypeEnum.Text, DatabaseHelper.DefineSqlCommandType(sql.SqlQuery), auditErrorMsg);

            return result;
        }

        /// <summary>
        /// Invoke the DB NonQuery command in the Database Instance and return the number of completed operations
        /// </summary>
        /// <param name="sql">Contains the SQL object</param>
        /// <returns>An int value that count all the affected table rows</returns>
        private int ExecuteNonQuery(SqlBuilder sql)
        {
            var affectedRowOnCommand = 0;

            // Audit Parameters
            var auditLevel = AuditLevelsEnum.Info;
            var auditErrorMsg = String.Empty;

            _lastCommand = sql.SqlQuery;
            _lastSql = DatabaseHelper.FormatSqlCommandQuery(_dbConnection.DatabaseProvider, sql);

            // Main Operations
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
                auditErrorMsg = $"[Execute Command Error]: {ex.Message}";
                auditLevel = AuditLevelsEnum.Error;
                affectedRowOnCommand = -1;
            }
            finally
            {
                CloseConnection();
            }

            // Add a new audit event inside the database table if the mode is enable
            if (!_auditExec) NewAuditEvent(auditLevel, DBCommandTypeEnum.Text, DatabaseHelper.DefineSqlCommandType(sql.SqlQuery), auditErrorMsg);

            return affectedRowOnCommand;
        }

        /// <summary>
        /// Invoke the DB Query command in the Database Istance and cast it to a specific object type
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the SQL object</param>
        /// <returns>A value based of the object type</returns>
        /// <exception cref="Exception"></exception>
        private IEnumerable<T> ExecuteQuery<T>(SqlBuilder sql)
        {
            var type = typeof(T);

            // Audit Parameters
            var auditLevel = AuditLevelsEnum.Info;
            var auditErrorMsg = string.Empty;

            var list = new List<T>();

            _lastCommand = sql.SqlQuery;
            _lastSql = DatabaseHelper.FormatSqlCommandQuery(_dbConnection.DatabaseProvider, sql);

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
                                foreach (var info in pocoObject.ColumnDataInfo.Where(c => c.PocoObjectProperty == prop.Name))
                                {
                                    pName = info.ColumnName;
                                    break;
                                }
                            }
                            var value = string.IsNullOrEmpty(pName) ? null : cmdReader[pName];
                            if (value == DBNull.Value) value = null;
                            if (value != null) prop.SetValue(obj, value, null);
                        }

                        list.Add(obj);
                    }
                }
            }
            catch (Exception ex)
            {
                // Add to info of the last executed command after an error
                auditErrorMsg = $"[Query Command Error]: {ex.Message}";
                auditLevel = AuditLevelsEnum.Error;
                list = null;
            }
            finally
            {
                CloseConnection();
            }

            // Add a new audit info event inside the database table if the mode is enable
            if (!_auditExec) NewAuditEvent(auditLevel, DBCommandTypeEnum.Text, DatabaseHelper.DefineSqlCommandType(sql.SqlQuery), auditErrorMsg);

            return list;
        }

        #endregion
        
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
        public IEnumerable<T> Query<T>(SqlBuilder sql)
            => ExecuteQuery<T>(sql);

        /// <summary>
        /// Invoke the DB Query command in the Database Istance and cast it to a specific object type
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the query command to be executed</param>
        /// <param name="args">Contains all the extra arguments of the query</param>
        /// <returns>A value based of the object type</returns>
        public IEnumerable<T> Query<T>(string sql, params object[] args)
            => Query<T>(new SqlBuilder(sql, args));

        #endregion

         // Audit Methods //
        
        #region Audit Methods
        
        /// <summary>
        /// Methods that enable the audit mode for every single database operation
        /// </summary>
        /// <param name="userForAudit">Contains a specific user, if null will use the default one</param>
        public void EnableAuditMode(string? userForAudit = null)
        {
            _auditMode = true;
            if (!string.IsNullOrEmpty(userForAudit)) _auditUser = userForAudit;
        }


        /// <summary>
        /// Methods that disable the audit mode for every single database operation
        /// </summary>
        public void DisableAuditMode()
        {
            _auditMode = false;
            _auditUser = string.Empty;
        }

        /// <summary>
        /// Methods that add a new Audit Event inside the database dedicated table
        /// </summary>
        /// <param name="level">Contains the level of the current action</param>
        /// <param name="cmdType">Contains the type of the current action's command</param>
        /// <param name="sqlType">Contains the type of the current action's query</param>
        /// <param name="errorMsg">Contains an optional error message</param>
        private void NewAuditEvent(AuditLevelsEnum level, DBCommandTypeEnum cmdType, SqlCommandTypeEnum sqlType, string? errorMsg = null)
        {
            if (!_auditMode) return;
            if (string.IsNullOrEmpty(LastCommand) && string.IsNullOrEmpty(LastSql)) return;

            // Set a true the audit action flag
            _auditExec = true;

            // Verify if the audit table already exists, if not will create it
            if (Scalar<int>(AuditModel.GenerateQueryForCheckTableExist()) != 1) Execute(AuditModel.GenerateQueryForTableCreation());

            // Insert the new event inside the audit table
            Insert<AuditModel>(new AuditModel()
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

        #endregion

        // Extended Methods //
        
        #region FindAll Methods

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

        #endregion
        
        #region FetchSingle Methods

        /// <summary>
        /// Select a single specific element inside a table
        /// </summary>
        /// <param name="sql">Contains the SQL object</param>
        /// <returns>A value based of the object type</returns>
        public T FindSingle<T>(SqlBuilder sql)
            => Query<T>(sql).ToList().FirstElement();

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
            var sqlAutoSelect = DatabaseAutoQueryHelper.AutoSelectClause<T>().Where("WHERE 1=1");
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
            => Query<T>(_dbConnection.DatabaseProvider.BuildPageQuery<T>(page, sql)).ToList();

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
        
        #region Exist Methods

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
         => Exist(DatabaseAutoQueryHelper.AutoExistClause<T>(new PocoObject<T>(obj)));

        #endregion

        // Object Base Methods //
        
        #region Insert Methods

        /// <summary>
        /// Insert the passed object inside a table of the database in the form of a row
        /// </summary>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object? Insert<T>(object obj)
        {
            if (obj == null) return null;
            return Execute(DatabaseAutoQueryHelper.AutoInsertClause<T>(new PocoObject<T>(obj)));
        }

        #endregion
        
        #region Update Methods

        /// <summary>
        /// Update a number of element inside a table of the database
        /// </summary>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Update<T>(object obj)
        {
            if (obj == null) return null;
            return Execute(DatabaseAutoQueryHelper.AutoUpdateClause(new PocoObject<T>(obj)));
        }

        #endregion
        
        #region Delete Methods

        /// <summary>
        /// Delete a number of element inside a table of the database
        /// </summary>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Delete<T>(object obj)
        {
            if (obj == null) return null;
            return Execute(DatabaseAutoQueryHelper.AutoDeleteClause(new PocoObject<T>(obj)));
        }

        #endregion
        
        #region Save Methods

        /// <summary>
        /// If an object already exist inside the database will update it, otherwise will cretae it
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Save<T>(object obj) 
            => Exist<T>(obj) ? Update<T>(obj) : Insert<T>(obj);

        #endregion
    }
}

