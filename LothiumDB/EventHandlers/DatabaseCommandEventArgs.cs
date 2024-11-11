using System.Data.Common;
using LothiumDB.Enumerations;

namespace LothiumDB.EventHandlers;

/// <summary>
/// Events Args correlated to a database command
/// </summary>
/// <param name="operation">Contains the operation type of the command</param>
/// <param name="query">Contains the actual command</param>
/// <param name="parameters">Contains a collection of parameter to use alongside the command</param>
public class DatabaseCommandEventArgs(DatabaseOperationTypesEnum operation, string query, object[] parameters) : EventArgs
{
    public DatabaseOperationTypesEnum Operation = operation;
    public string Query = query;
    public object[] Parameters = parameters;
}