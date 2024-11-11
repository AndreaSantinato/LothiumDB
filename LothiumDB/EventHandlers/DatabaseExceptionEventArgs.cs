using LothiumDB.Enumerations;

namespace LothiumDB.EventHandlers;

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