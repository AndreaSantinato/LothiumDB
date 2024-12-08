using System.Data;
using System.Data.Common;
using LothiumDB.Exceptions;

namespace LothiumDB.Helpers;

/// <summary>
/// Manager class used to store and centralize all the methods
/// required to perform operation from and to the database provided instance
/// </summary>
internal static class CommandManager
{
    /// <summary>
    /// Perform a scalar command and provided a set of checks to the final returned result
    /// </summary>
    /// <typeparam name="T">Contains the type of the returned object</typeparam>
    /// <param name="command">Contains the database command to perform</param>
    /// <returns>An object of the </returns>
    internal static object? PerformScalarCommand<T>(IDbCommand command)
        => PerformScalarCommandAsync<T>(command, CancellationToken.None).GetAwaiter().GetResult();
    
    /// <summary>
    /// Perform an asynchronous scalar command and provided a set of checks to the final returned result
    /// </summary>
    /// <typeparam name="T">Contains the type of the returned object</typeparam>
    /// <param name="command">Contains the database command to perform</param>
    /// <param name="cancellationToken">Contains a token to cancel the current operation</param>
    /// <returns>An object of the </returns>
    internal static async Task<object?> PerformScalarCommandAsync<T>(IDbCommand command, CancellationToken cancellationToken)
    {
        Utility.CheckDatabaseCommand(command);

        return Utility.HandleScalarDbNullConversion<T>(
                (command is DbCommand dbCommand)
                    ? await dbCommand.ExecuteScalarAsync(cancellationToken)
                    : command.ExecuteScalar()
        );
    }
    
    /// <summary>
    /// Perform an execute command and return the number of affected rows
    /// </summary>
    /// <param name="command">Contains the database command to perform</param>
    /// <returns>The number of rows affected by the database's command</returns>
    internal static int PerformExecuteCommand(IDbCommand command)
        => PerformExecuteCommandAsync(command, CancellationToken.None).GetAwaiter().GetResult();
    
    /// <summary>
    /// Perform an asynchronous execute command and return the number of affected rows
    /// </summary>
    /// <param name="command">Contains the database command to perform</param>
    /// <param name="cancellationToken">Contains a token to cancel the current operation</param>
    /// <returns>The number of rows affected by the database's command</returns>
    internal static async Task<int> PerformExecuteCommandAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        Utility.CheckDatabaseCommand(command);
        
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
        => PerformQueryCommandAsync<T>(command, CancellationToken.None).GetAwaiter().GetResult();
    
    /// <summary>
    /// Perform an asynchronous query command and return a collection of typed objects
    /// </summary>
    /// <typeparam name="T">Contains the type of the collection's object</typeparam>
    /// <param name="command">Contains the database command to perform</param>
    /// <param name="cancellationToken">Contains a token to cancel the current operation</param>
    /// <returns>A collection of object automatically mapped based on a specified model</returns>
    internal static async Task<IEnumerable<T>> PerformQueryCommandAsync<T>(IDbCommand command, CancellationToken cancellationToken)
    {
        Utility.CheckDatabaseCommand(command);

        using var readerManager = new ReaderManager(
            (command is DbCommand dbCommand)
                ? await dbCommand.ExecuteReaderAsync(cancellationToken)
                : command.ExecuteReader(),
            [typeof(T)]
        );

        try
        {
            var data = readerManager.RetrieveData();
            DatabaseException.ThrowIfNull(data, nameof(data));

            object?[] enumerable = data.ToArray();

            for (var i = 0; i < enumerable.Length; i++)
            {
                var objectType = typeof(T);
                var underlyingType = Nullable.GetUnderlyingType(objectType) ?? objectType;

                if (enumerable[i] != null)
                    enumerable[i] = Convert.ChangeType(enumerable[i], underlyingType);
            }

            return enumerable.Cast<T>().ToList();
        }
        catch 
        {
            return [];
        }
    }
}