using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using LothiumDB.Tools;
using LothiumDB.Exceptions;
using LothiumDB.Core.Interfaces;
using LothiumDB.Core.PocoDataInfo;

namespace LothiumDB.Core;

/// <summary>
/// Helper Class that contains methods used by other class
/// for complex or specific actions
/// </summary>
internal static class DatabaseHelper
{
    /// <summary>
    /// Return a boolean result based on the current connection object status
    /// </summary>
    /// <param name="connection">Contains the connection's object to use</param>
    /// <returns>True = Connection Open; False = Connection Closed</returns>
    internal static bool CheckConnectionStatus(IDbConnection connection)
    {
        return connection.State switch
        {
            ConnectionState.Open => true,
            ConnectionState.Connecting => true,
            ConnectionState.Fetching => true,
            ConnectionState.Executing => true,
            ConnectionState.Broken => false,
            ConnectionState.Closed => false,
            _ => false
        };
    }

    /// <summary>
    /// Create a new database's command that will be used to perform operations inside
    /// the chosen database's instance
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="connection">Contains the database's connection object to use</param>
    /// <param name="transaction">Contains an optional created transaction</param>
    /// <param name="commandType">Contains the type of the command to create</param>
    /// <param name="commandTimeout">Contains the maximum time for the command to execute before raise an error</param>
    /// <param name="sql">Contains the actual query / stored procedure to execute</param>
    /// <param name="args">Contains a collection of parameters to use alongside the query / stored procedure to execute</param>
    /// <returns>A DbCommand object related to the type of the provided connection's object</returns>
    /// <exception cref="DatabaseException">Raise an error the provided sql contains variables but the parameter's collection is empty</exception>
    internal static IDbCommand CreateDatabaseCommand(
        IDatabaseProvider provider,
        IDbConnection connection,
        IDbTransaction? transaction,
        CommandType commandType,
        int commandTimeout,
        string sql,
        object[] args
    )
    {
        // Check if the minimum required variables are correctly sets
        DatabaseException.ThrowIfNullOrEmpty(sql);
        if (sql.Contains('@') && args.Length.Equals(0))
            throw new DatabaseException("The provided SQL contains variables but the actual parameters were not provided!");
        
        // Create the new command
        var command = connection.CreateCommand();
        
        command.Transaction = transaction;
        command.CommandText = sql;
        command.CommandType = commandType;
        command.CommandTimeout = commandTimeout;

        if (args.Length != 0)
        {
            DatabaseHelper.AddParamsToDatabaseCommand(
                provider,
                ref command,
                new SqlBuilder(sql, args)
            );
        }

        DatabaseException.ThrowIfNull(command);

        return command;
    }

    /// <summary>
    /// Perform a bunch of checks to the provided database's command
    /// </summary>
    /// <param name="command">Contains the command to check and validate</param>
    /// <exception cref="DatabaseException">Raise an error if the command don't pass one or more checks</exception>
    private static void CheckDatabaseCommand(IDbCommand command)
    {
        if (command == null)
            throw new DatabaseException("No database command provided!");

        if (string.IsNullOrWhiteSpace(command.CommandText))
            throw new DatabaseException("No SQL command provided!");

        if (command.CommandText.Contains('@') && command.Parameters.Count == 0)
            throw new DatabaseException("The SQL command contains variables but no parameters provided!");
    }
    
    private static IEnumerable<T> RetrieveAndMapData<T>(IDataReader reader)
    {
        var result = new List<T>();
        
        var type = typeof(T);
        var mapper = new AutoMapper(type);
        var props = AutoMapper.GetMappedProperties<T>();
        
        while (reader.Read())
        {
            if (reader.FieldCount <= 0) continue;

            var item = Activator.CreateInstance(type);

            ArgumentNullException.ThrowIfNull(mapper.TableData, nameof(mapper.TableData));
            ArgumentNullException.ThrowIfNull(mapper.ColumnsData, nameof(mapper.ColumnsData));

            foreach (var prop in props)
            {
                var colInfo = Array.Find(mapper.ColumnsData.ToArray(),
                    col => col.PocoObjectPropertyName == prop.Name);
                ArgumentNullException.ThrowIfNull(colInfo, nameof(colInfo));

                var value = (string.IsNullOrEmpty(colInfo.Name))
                    ? reader[colInfo.PocoObjectPropertyName]
                    : reader[colInfo.Name];

                value = DatabaseHelper.VerifyDbNullValue(colInfo, value);

                prop.SetValue(item, value, null);
                continue;
            }

            if (item is not null)
            {
                result.Add((T)item);
            }
        }

        return result;
    }
    
    /// <summary>
    /// Perform a scalar command and provided a set of checks to the final returned result
    /// </summary>
    /// <typeparam name="T">Contains the type of the returned object</typeparam>
    /// <param name="command">Contains the database command to perform</param>
    /// <returns>An object of the </returns>
    internal static object? PerformScalarCommand<T>(IDbCommand command)
    {
        CheckDatabaseCommand(command);
        
        var value = command.ExecuteScalar();

        DatabaseHelper.HandleScalarDbNullConversion<T>(
            value, 
            out var result
        );
        
        return result;
    }
    
    /// <summary>
    /// Perform an asynchronous scalar command and provided a set of checks to the final returned result
    /// </summary>
    /// <typeparam name="T">Contains the type of the returned object</typeparam>
    /// <param name="command">Contains the database command to perform</param>
    /// <param name="cancellationToken">Contains a token to cancel the current operation</param>
    /// <returns>An object of the </returns>
    internal static async Task<object?> PerformScalarCommandAsync<T>(IDbCommand command, CancellationToken cancellationToken)
    {
        CheckDatabaseCommand(command);

        var value = (command is DbCommand dbCommand)
            ? await dbCommand.ExecuteScalarAsync(cancellationToken)
            : command.ExecuteScalar();

        DatabaseHelper.HandleScalarDbNullConversion<T>(
            value, 
            out var result
        );
        
        return result;
    }
    
    /// <summary>
    /// Perform an execute command and return the number of affected rows
    /// </summary>
    /// <param name="command">Contains the database command to perform</param>
    /// <returns>The number of rows affected by the database's command</returns>
    internal static int PerformExecuteCommand(IDbCommand command)
    {
        CheckDatabaseCommand(command);
        
        return command.ExecuteNonQuery();
    }
    
    /// <summary>
    /// Perform an asynchronous execute command and return the number of affected rows
    /// </summary>
    /// <param name="command">Contains the database command to perform</param>
    /// <param name="cancellationToken">Contains a token to cancel the current operation</param>
    /// <returns>The number of rows affected by the database's command</returns>
    internal static async Task<int> PerformExecuteCommand(IDbCommand command, CancellationToken cancellationToken)
    {
        CheckDatabaseCommand(command);
        
        return (command is DbCommand dbCommand)
            ? await dbCommand.ExecuteNonQueryAsync(cancellationToken)
            : command.ExecuteNonQuery();
    }

    /// <summary>
    /// Perform a query command and return a collection of typed objects
    /// </summary>
    /// <typeparam name="T">Contains the type of the collection's object</typeparam>
    /// <param name="command">Contains the database command to perform</param>
    /// <returns>A collection of object automatically mapped based on a specified model</returns>
    internal static IEnumerable<T> PerformQueryCommand<T>(IDbCommand command)
    {
        CheckDatabaseCommand(command);
        
        return RetrieveAndMapData<T>(command.ExecuteReader());
    }
    
    /// <summary>
    /// Perform an asynchronous query command and return a collection of typed objects
    /// </summary>
    /// <typeparam name="T">Contains the type of the collection's object</typeparam>
    /// <param name="command">Contains the database command to perform</param>
    /// <param name="cancellationToken">Contains a token to cancel the current operation</param>
    /// <returns>A collection of object automatically mapped based on a specified model</returns>
    internal static async Task<IEnumerable<T>> PerformQueryCommandAsync<T>(IDbCommand command, CancellationToken cancellationToken)
    {
        CheckDatabaseCommand(command);
        
        return RetrieveAndMapData<T>(
            (command is DbCommand dbCommand)
                ? await dbCommand.ExecuteReaderAsync(cancellationToken)
                : command.ExecuteReader()
        );
    }
    
    /// <summary>
    /// Convert the result object into a valid return type  
    /// </summary>
    /// <typeparam name="T">Contains the type of the final returned object</typeparam>
    /// <param name="value">Contains the object to check and convert if necessary</param>
    /// <param name="result">Contains the final converted result object</param>
    private static void HandleScalarDbNullConversion<T>(object? value, out object? result)
    {
        var returnType = typeof(T);
        var underlyingType = Nullable.GetUnderlyingType(returnType);
                
        result = (underlyingType != null && (value == null || value == DBNull.Value))
            ? default(T)
            : Convert.ChangeType(value, underlyingType ?? returnType);
    }
    
    /// <summary>
    /// Retrieve all the parameter's variables inside a sql query
    /// </summary>
    /// <param name="provider">Contains the chosen database provider</param>
    /// <param name="sql">Contains the actual sql query</param>
    /// <returns></returns>
    private static MatchCollection? ExtractParametersVariableFromQuery(IDatabaseProvider provider, SqlBuilder sql)
    {
        var regex = new Regex(
            $@"(?<!{provider.GetVariablePrefix()}){provider.GetVariablePrefix()}\w+",
            RegexOptions.Compiled
        );
        return string.IsNullOrEmpty(sql.Query) ? null : regex.Matches(sql.Query);
    }

    /// <summary>
    /// Verify the nullable state of the column and perform the appropriete checks
    /// </summary>
    /// <param name="value"></param>
    /// <param name="columnData"></param>
    internal static object? VerifyDbNullValue(PocoColumnData columnData, object? value)
    {
        if (value != DBNull.Value) 
            return value;

        if (!columnData.Nullable)
        {
            ArgumentNullException.ThrowIfNull(columnData.DefaultValue, nameof(columnData.DefaultValue));

            var colName = (string.IsNullOrEmpty(columnData.Name))
                ? columnData.PocoObjectPropertyName
                : columnData.Name;

            throw new Exception($"The column {colName} don't allow nullable values");
        }

        return null;
    }

    /// <summary>
    /// Add all the variables inside the query to the final database command to be executed by the library
    /// This method will add a parameters for each variables with their respected values
    /// </summary>
    /// <param name="provider">Contains the loaded database provider</param>
    /// <param name="command">Contains the database command to add the parameters</param>
    /// <param name="sql">Contains the sql query</param>
    private static void AddParamsToDatabaseCommand(IDatabaseProvider provider, ref IDbCommand command, SqlBuilder sql)
    {
        var paramsList = new Dictionary<string, object>();

        // Gets all the variables inside the query
        var variables = DatabaseHelper.ExtractParametersVariableFromQuery(provider, sql);
        if (variables != null && !variables.Any()) return;

        // Add to the dictionary all the variables with their respected values
        var index = 0;
        if (variables != null)
        {
            foreach (var variable in variables)
            {
                if (variable is null) continue;

                var key = variable.ToString();
                var value = sql.Params.ElementAt(index);

                if (key != null) paramsList.Add(key, value);
                index++;
            }
        }

        // Add the parameters inside the database command (Name and Values)
        foreach (var elem in paramsList)
        {
            // Create a new database parameter
            var param = command.CreateParameter();

            // Set the name and value for the parameter
            param.ParameterName = elem.Key;
            param.Value = elem.Value;

            // Define the type of the parameter
            if (elem.Value.GetType() == typeof(object))
            {
                param.DbType = DbType.Object;
            }
            else
                switch (elem.Value)
                {
                    case bool:
                        param.DbType = DbType.Boolean;
                        break;
                    case byte:
                        param.DbType = DbType.Byte;
                        break;
                    case string:
                        param.DbType = DbType.String;
                        break;
                    case short:
                        param.DbType = DbType.Int16;
                        break;
                    case ushort:
                        param.DbType = DbType.Int16;
                        break;
                    case int:
                        param.DbType = DbType.Int32;
                        break;
                    case uint:
                        param.DbType = DbType.Int32;
                        break;
                    case long:
                        param.DbType = DbType.Int64;
                        break;
                    case ulong:
                        param.DbType = DbType.Int64;
                        break;
                    case double:
                        param.DbType = DbType.Double;
                        break;
                    case decimal:
                        param.DbType = DbType.Decimal;
                        break;
                    case Guid:
                        param.DbType = DbType.Guid;
                        break;
                    case DateOnly:
                        param.DbType = DbType.Date;
                        break;
                    case DateTime:
                        param.DbType = DbType.DateTime;
                        break;
                    default:
                        break;
                }

            // Add the created parameter to the final database command
            command.Parameters.Add(param);
        }

        // If the query contains a double variable prefix it will be formatted to be a normal sql variable
        if (command.CommandText.Contains($"{provider.GetVariablePrefix()}{provider.GetVariablePrefix()}"))
        {
            command.CommandText = command.CommandText.Replace(
                $"{provider.GetVariablePrefix()}{provider.GetVariablePrefix()}",
                provider.GetVariablePrefix()
            );
        }
    }
}