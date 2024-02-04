namespace LothiumDB.Core.Interfaces;

internal interface IDatabaseCore : IDisposable
{
    object? Scalar<T>(string sql, object[] args);
    
    int Execute(string sql, object[] args);
    
    IEnumerable<T>? Query<T>(string sql, object[] args);
}