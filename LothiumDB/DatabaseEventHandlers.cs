namespace LothiumDB;

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

/// <summary>
/// Events Arguments correlated to a generated exception
/// </summary>
/// <param name="operation">Indicates the type of operation where the exception got generated</param>
/// <param name="exception">Contains the actual generated exception</param>
public class DatabaseExceptionEventArgs(DatabaseOperationTypesEnum operation, Exception exception) : EventArgs
{
    public DatabaseOperationTypesEnum Operation { get; set; } = operation;
    public Exception Exception { get; set; } = exception;
}