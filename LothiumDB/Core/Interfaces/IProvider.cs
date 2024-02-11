using System.Data;
using LothiumDB.Core.Enumerations;
using LothiumDB.Tools;

namespace LothiumDB.Core.Interfaces;

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