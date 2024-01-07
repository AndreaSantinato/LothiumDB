using System.Data;
using LothiumDB.Enumerations;
using LothiumDB.Extensions;

// Interfaces Namespace
namespace LothiumDB.Interfaces;

public interface IProvider
{
    ProviderTypesEnum GetProviderType();

    string? GetConnectionString();

    string? GetVariablePrefix();
    
    abstract IDbConnection CreateConnection();

    abstract SqlBuilder BuildPageQuery<T>(PageObject<T> pageObj, SqlBuilder sql);

    abstract SqlBuilder CheckIfAuditTableExists();
    
    abstract SqlBuilder CreateAuditTable();
}