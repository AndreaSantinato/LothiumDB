// System Class
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Custom Class
using LothiumDB.Enumerations;
using LothiumDB.Exceptions;
using LothiumDB.Helpers;
using LothiumDB.Interfaces;

namespace LothiumDB.DatabaseExceptions
{
    /// <summary>
    /// Extension Class Used By The Database Class
    /// This Include all the centralized methods for managing the connection, the transaction and the commands
    /// </summary>
    internal static class DatabaseExtensions
    {
        /// <summary>
        /// Method used to manage all the connection's states
        /// </summary>
        /// <param name="operationType"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        internal static bool ManageConnection(InternalOperationType operationType, ref IDbConnection connection)
        {
            try
            {
                if (connection == null) throw new Exception("The database connection is not setted, impossible to establish a proper connection!");
                switch (operationType)
                {
                    case InternalOperationType.OpenConnection:
                        if (connection.State == ConnectionState.Open) throw new Exception("The connection is already open!");
                        if (connection.State == ConnectionState.Executing) throw new Exception("The connection is currently executing operations!");
                        if (connection.State == ConnectionState.Fetching) throw new Exception("The connection is fetching data!");
                        if (connection.State == ConnectionState.Connecting) throw new Exception("The connection is tring to connecting to the database istance!");
                        connection.Open();
                        break;
                    case InternalOperationType.CloseConnection:
                        if (connection.State == ConnectionState.Closed) throw new Exception("The connection is already closed!");
                        if (connection.State == ConnectionState.Executing) throw new Exception("The connection is currently executing operations!");
                        if (connection.State == ConnectionState.Fetching) throw new Exception("The connection is currently fetching data!");
                        connection.Close();
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
        /// <param name="connection"></param>
        /// <returns></returns>
        internal static bool ManageConnectionState(IDbConnection connection)
        {
            bool connectionStatus = false;
            switch (connection.State)
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
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        internal static bool ManageTransaction(InternalOperationType operationType, ref IDbConnection? connection, ref IDbTransaction? transaction)
        {
            try
            {
                if (connection == null) throw new Exception("The database connection is not setted, impossible to establish a proper connection!");
                switch (operationType)
                {
                    case InternalOperationType.BeginTransaction:
                        transaction = connection.BeginTransaction();
                        break;
                    case InternalOperationType.RollBackTransaction:
                        if (transaction == null) throw new Exception("The database's transaction is not setted, impossible to rollback all the operations!");
                        transaction.Rollback();
                        transaction = null;
                        break;
                    case InternalOperationType.CommitTransaction:
                        if (transaction == null) throw new Exception("The database's transaction is not setted, impossible to rollback all the operations!");
                        transaction.Commit();
                        transaction = null;
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
        internal static IDbCommand CreateCommand(IDbProvider provider, IDbConnection connection, CommandType commandType, string sqlQuery, params object[] args)
        {
            IDbCommand command = connection.CreateCommand();
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
                DatabaseUtility.AddParameters(provider, ref command, args);
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
        /// Method used to manage the activation of the Audit Mode
        /// </summary>
        /// <param name="operationType"></param>
        /// <param name="userForAuditMode"></param>
        /// <param name="auditModeStatus"></param>
        /// <param name="auditUser"></param>
        /// <returns></returns>
        internal static bool ManageAuditMode(InternalOperationType operationType, string? userForAuditMode, ref bool auditModeStatus, ref string? auditUser)
        {
            try
            {
                switch (operationType)
                {
                    case InternalOperationType.EnableAuditMode:
                        auditModeStatus = true;
                        if (!string.IsNullOrEmpty(userForAuditMode)) auditUser = userForAuditMode;
                        break;
                    case InternalOperationType.DisableAuditMode:
                        auditModeStatus = false;
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
    }
}
