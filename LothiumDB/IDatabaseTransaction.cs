using System.Data;

namespace LothiumDB;

public interface IDatabaseTransaction
{
    void BeginTransaction();

    public void BeginTransaction(IsolationLevel isolationLevel);
    
    void RollbackTransaction();
    
    void CommitTransaction();

#if ASYNC
    
    Task BeginTransactionAsync();
    
    Task BeginTransactionAsync(CancellationToken cancellationToken);
    
    Task BeginTransactionAsync(IsolationLevel isolationLevel);
    
    Task BeginTransactionAsync(CancellationToken cancellationToken, IsolationLevel isolationLevel);
    
    Task RollbackTransactionAsync();
    
    Task CommitTransactionAsync();

#endif
}