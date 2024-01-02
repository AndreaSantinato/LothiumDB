// System Class
using System.Data;
using System.Text.RegularExpressions;
// Custom Class
using LothiumDB.Enumerations;
using LothiumDB.Extensions;
using LothiumDB.Interfaces;

// Namespace
namespace LothiumDB.Core.Helpers;

/// <summary>
/// Helper Class that contains methods used by other class
/// for complex or specific actions
/// </summary>
internal static class DatabaseHelper
{
    /// <summary>
    /// Retrieve all the parameter's variables inside a sql query
    /// </summary>
    /// <param name="provider">Contains the chosen database provider</param>
    /// <param name="sql">Contains the actual sql query</param>
    /// <returns></returns>
    private static MatchCollection? ExtractParametersVariableFromQuery(IDatabaseProvider provider, SqlBuilder sql)
    {
        var regex = new Regex(
            $@"(?<!{provider.DbVariablePrefix}){provider.DbVariablePrefix}\w+",
            RegexOptions.Compiled
        );
        return string.IsNullOrEmpty(sql.Query) ? null : regex.Matches(sql.Query);
    }

    /// <summary>
    /// Define what is the current database command's type
    /// </summary>
    /// <param name="cmd">Contains the database command</param>
    /// <returns>The database command's type</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static DBCommandTypeEnum DefineDatabaseCommandType(string cmd)
    {
        return cmd.ToUpper() switch
        {
            "TEXT" => DBCommandTypeEnum.Text,
            "TABLEDIRECTR" => DBCommandTypeEnum.TableDirect,
            "STOREDPROCEDURES" => DBCommandTypeEnum.StoredProcedure,
            _ => DBCommandTypeEnum.None
        };
    }

    /// <summary>
    /// Define what is the current sql query's type
    /// </summary>
    /// <param name="sql">Contains the sql query</param>
    /// <returns>The sql query's type</returns>
    public static SqlCommandTypeEnum DefineSqlCommandType(string sql)
    {
        SqlCommandTypeEnum type;

        if (sql.ToUpper().StartsWith("SELECT"))
            type = SqlCommandTypeEnum.Select;
        else if (sql.ToUpper().StartsWith("UPDATE"))
            type = SqlCommandTypeEnum.Update;
        else if (sql.ToUpper().StartsWith("DELETE"))
            type = SqlCommandTypeEnum.Delete;
        else if (sql.ToUpper().StartsWith("INSERT"))
            type = SqlCommandTypeEnum.Insert;
        else
            type = SqlCommandTypeEnum.None;

        return type;
    }

    /// <summary>
    /// Create a formatted Sql Command with all the parameters and their values
    /// </summary>
    /// <param name="provider">Contains the chosen database provider</param>
    /// <param name="sql">Contains the sql builder object</param>
    /// <returns></returns>
    public static string FormatSqlCommandQuery(IDatabaseProvider provider, SqlBuilder sql)
    {
        try
        {
            // Check if the minimum values are valid
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(sql);

            if (string.IsNullOrEmpty(sql.Query))
                throw new ArgumentNullException(nameof(sql.Query));

            // If there aren't any parameters, format the full sql with a generic message
            if (!sql.Params.Any()) return $"{sql}\n\n/// No Params ///";

            // Check if inside the query there are parameters
            var queryParams = DatabaseHelper.ExtractParametersVariableFromQuery(provider, sql);
            if (queryParams != null && !queryParams.Any()) return $"{sql}\n\n/// No Params ///";

            // Format the parameters for the output result
            var formattedParameters = string.Empty;
            var parIndex = 0;
            queryParams?.ToList().ForEach(par =>
            {
                var parName = par.ToString();
                var parValue = sql.Params[parIndex];
                formattedParameters += $"\n{parIndex}) {parName} = {parValue}";
                parIndex++;
            });

            // Return the final formatted result
            return $"{sql}\n\n/// Query Params ///\n{formattedParameters}";
        }
        catch (Exception ex)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Add all the variables inside the query to the final database command to be executed by the library
    /// This method will add a parameters for each variables with their respected values
    /// </summary>
    /// <param name="provider">Contains the loaded database provider</param>
    /// <param name="command">Contains the database command to add the parameters</param>
    /// <param name="sql">Contains the sql query</param>
    public static void AddParamsToDatabaseCommand(IDatabaseProvider provider, ref IDbCommand command, SqlBuilder sql)
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
        if (command.CommandText.Contains($"{provider.DbVariablePrefix}{provider.DbVariablePrefix}"))
        {
            command.CommandText = command.CommandText.Replace(
                $"{provider.DbVariablePrefix}{provider.DbVariablePrefix}",
                provider.DbVariablePrefix
            );
        }
    }
}