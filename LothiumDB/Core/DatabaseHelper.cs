using System.Configuration.Provider;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using LothiumDB.Core.Interfaces;
using LothiumDB.Core.PocoDataInfo;
using LothiumDB.Exceptions;
using LothiumDB.Tools;

namespace LothiumDB.Core;

/// <summary>
/// Helper Class that contains methods used by other class
/// for complex or specific actions
/// </summary>
internal static class DatabaseHelper
{
    /// <summary>
    /// Open a new db's connection in safe
    /// </summary>
    /// <param name="connection">Contains the connection object to be opended</param>
    /// <param name="configuration">Contains the configuration of the db instance</param>
    public static void OpenSafeConnection(IProvider provider, IDbConnection? connection)
    {
        connection ??= provider.CreateConnection();

        if (ConnectionState.Closed == connection.State)
            connection.Open();
    }

    /// <summary>
    /// Close an existing db's connection in safe if the provider doesn't want it to be keeped open
    /// </summary>
    /// <param name="connection">Contains the connection object to be opended</param>
    /// <param name="configuration">Contains the configuration of the db instance</param>
    /// <param name="keepOpen">Indicate if the connection need to be keeped open</param>
    public static void CloseSafeConnection(IProvider provider, IDbConnection? connection, bool keepOpen = false)
    {
        if (connection is null) return;

        if (keepOpen)
            OpenSafeConnection(provider, connection);

        if ((ConnectionState.Open == connection.State) && !keepOpen)
            connection.Close();
    }

    /// <summary>
    /// Create a new db's command in safe
    /// If the sql have some parameters they will be automatically added to the command
    /// If the command need a transaction it will be automatically added to the command
    /// </summary>
    /// <param name="sql">Contains the query or stored procedure to be executed</param>
    /// <param name="args">Contains all the variable/parameters required by the query or stored procedure</param>
    /// <param name="cmdType">Indicates what type of command must be created</param>
    /// <param name="provider">Contains the db's configuration</param>
    /// <param name="connection">Contains the actual db's connection</param>
    /// <param name="transaction">Contains an optional db's transaction</param>
    /// <returns>An object of type DbCommand based on the configuration's provider</returns>
    public static IDbCommand CreateSafeCommand(
        string sql,
        object[] args,
        CommandType cmdType,
        IProvider provider,
        IDbConnection connection,
        DatabaseTransaction transaction
    )
    {
        // Check if the minimum required variables are correctly sets
        DatabaseException.ThrowIfNull(provider, "Database Provider");
        DatabaseException.ThrowIfNull(connection, "Database Connection");
        DatabaseException.ThrowIfNull(transaction, "Database Transaction");
        DatabaseException.ThrowIfNullOrEmpty(sql);

        // Create the new command
        // Create the new command
        var command = provider.CreateCommand(
            sql,
            (transaction.Transaction is null)
                ? connection
                : transaction.Connection,
            (transaction.Transaction is null)
                ? null
                : transaction.Transaction
        );
        command.CommandType = cmdType;

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
    /// Retrieve all the parameter's variables inside a sql query
    /// </summary>
    /// <param name="provider">Contains the chosen database provider</param>
    /// <param name="sql">Contains the actual sql query</param>
    /// <returns></returns>
    private static MatchCollection? ExtractParametersVariableFromQuery(IProvider provider, SqlBuilder sql)
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
    public static object? VerifyDBNullValue(PocoColumnData columnData, object? value)
    {
        if (value != DBNull.Value) return value;

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
    public static void AddParamsToDatabaseCommand(IProvider provider, ref IDbCommand command, SqlBuilder sql)
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