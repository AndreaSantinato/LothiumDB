using System.Data;
using LothiumDB.Core.Enumerations;
using LothiumDB.Tools;

namespace LothiumDB;

public class DatabaseProvider(ProviderTypesEnum type, string variablePrefix) : IDatabaseProvider
{
    private readonly ProviderTypesEnum _type = type;
    private readonly string _variablePrefix = variablePrefix;

    /// <summary>
    /// Indicates the type of the provider
    /// </summary>
    public ProviderTypesEnum GetProviderType() => _type;
    
    /// <summary>
    /// Indicates prefix that will be used to generate new parameter's variable
    /// during query generations
    /// </summary>
    public string GetVariablePrefix() => _variablePrefix;

    public SqlBuilder BuildPageQuery<T>(PageObject<T> pageObj, SqlBuilder sql)
    {
        throw new NotImplementedException();
    }
}