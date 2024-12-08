using System.Data;

namespace LothiumDB;

public class DatabaseProvider(DatabaseProviderTypesEnum type, string variablePrefix) : IDatabaseProvider
{
    private readonly DatabaseProviderTypesEnum _type = type;
    private readonly string _variablePrefix = variablePrefix;

    /// <summary>
    /// Return the chosen provider's type
    /// </summary>
    /// <returns>The value of the chosen Database's Provider</returns>
    public DatabaseProviderTypesEnum GetProviderType() => _type;
    
    /// <summary>
    /// Return the prefix that will be used to generate new parameter's variable
    /// during query generations
    /// </summary>
    /// <returns>The specific Database's Provider Variable Character</returns>
    public string GetVariablePrefix() => _variablePrefix;
    
    public SqlBuilder BuildPageQuery<T>(PageObject<T> pageObj, SqlBuilder sql)
    {
        throw new NotImplementedException();
    }
}