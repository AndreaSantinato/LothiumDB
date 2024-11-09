namespace LothiumDB.Core.Interfaces;

public interface IDatabaseConnection
{
    void OpenConnection();
    
    void CloseConnection();

#if ASYNC
    Task OpenConnectionAsync();

    Task OpenConnectionAsync(CancellationToken cancellationToken);

    Task CloseConnectionAsync();
#endif
}