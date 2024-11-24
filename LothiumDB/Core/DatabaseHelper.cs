using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using LothiumDB.Exceptions;
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
            DatabaseHelper.AddParametersToDatabaseCommand(
                ref command,
                provider,
                sql,
                args
            );
        }

        // If the query contains a double variable prefix it will be formatted to be a normal sql variable
        if (command.CommandText.Contains($"{provider.GetVariablePrefix()}{provider.GetVariablePrefix()}"))
        {
            command.CommandText = command.CommandText.Replace(
                $"{provider.GetVariablePrefix()}{provider.GetVariablePrefix()}",
                provider.GetVariablePrefix()
            );
        }
        
        DatabaseException.ThrowIfNull(command);

        return command;
    }

    /// <summary>
    /// Add all the variables inside the query to the final database command to be executed by the library
    /// This method will add a parameters for each variable with their respected values
    /// </summary>
    /// <param name="provider">Contains the loaded database provider</param>
    /// <param name="command">Contains the created database command</param>
    /// <param name="sql">Contains the sql query</param>
    /// <param name="args">Contains the sql parameters</param>
    private static void AddParametersToDatabaseCommand(
        ref IDbCommand command,
        IDatabaseProvider provider,
        string sql,
        object[] args
    )
    {
        foreach (var param in ExtractVariablesFromQuery(sql, args))
        {
            var dbParameter = command.CreateParameter();
            
            dbParameter.ParameterName = param.Key;
            dbParameter.Value = param.Value;
            
            dbParameter.DbType = param.Value switch
            {
                null => DbType.Object,
                bool => DbType.Boolean,
                byte => DbType.Byte,
                string => DbType.String,
                short or ushort => DbType.Int16,
                int or uint => DbType.Int32, 
                long or ulong => DbType.Int64,
                double => DbType.Double,
                decimal => DbType.Decimal,
                Guid => DbType.Guid,
                DateOnly => DbType.Date,
                DateTime => DbType.DateTime,
                _ => DbType.Object
            };

            // Add the created parameter to the final database command
            command.Parameters.Add(dbParameter);
        }
    }
    
    /// <summary>
    /// Extract all the variables declared inside the provided sql
    /// and create a dictionary with variables names and values
    /// </summary>
    /// <param name="sql">Contains the SQL that include all the declared variables</param>
    /// <param name="args">Contains all the provided values for every single variables</param>
    /// <returns>A dictionary with all the variables and their unique values</returns>
    /// <exception cref="DatabaseException">
    /// Generate an exception if the SQL is non provided or if the values doesn't match the variables
    /// </exception>
    private static Dictionary<string, object> ExtractVariablesFromQuery(string sql, object[] args)
    {
        if (string.IsNullOrEmpty(sql))
            throw new DatabaseException("No valid SQL provided!");

        if (args.Length == 0)
            return [];
        
        var matches = new Regex(@"@(\w+)").Matches(sql);
        if (matches.Count > args.Length)
            throw new DatabaseException("There are not enough values for all the retrieved variables in the SQL!");

        var variables = new Dictionary<string, object>();
        
        for (var i = 0; i < matches.Count; i++)
        {
            var variable = matches[i].Value;
            
            variables[variable] = args[i];
        }

        return variables;
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
    
    /// <summary>
    /// Perform a database reader and auto map the results inside a provided object's type
    /// </summary>
    /// <param name="reader">Contains the reader to use to retrieve all the data</param>
    /// <typeparam name="T">Contains the type of the object to map the retrieved data</typeparam>
    /// <returns>A collection of object based on the provided type auto mapped dynamically</returns>
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
                
                VerifyDbNullValue(colInfo, ref value);

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
    /// Verify the nullable state of the column and perform the appropriate checks
    /// </summary>
    /// <param name="value"></param>
    /// <param name="columnData"></param>
    private static void VerifyDbNullValue(PocoColumnData columnData, ref object? value)
    {
        if (value != DBNull.Value)
            return;

        if (columnData.Nullable)
        {
            value = null;
            return;
        }
        
        var columnName = string.IsNullOrEmpty(columnData.Name)
            ? columnData.PocoObjectPropertyName
            : columnData.Name;

        throw new DatabaseException($"The column '{columnName}' does not allow nullable values!");
    }
}