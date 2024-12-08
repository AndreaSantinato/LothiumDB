namespace LothiumDB;

public interface IDatabaseProvider
{
    /// <summary>
    /// Return the chosen provider's type
    /// </summary>
    /// <returns>The value of the chosen Database's Provider</returns>
    DatabaseProviderTypesEnum GetProviderType();

    /// <summary>
    /// Return the prefix that will be used to generate new parameter's variable
    /// during query generations
    /// </summary>
    /// <returns>The specific Database's Provider Variable Character</returns>
    string GetVariablePrefix();

    SqlBuilder BuildPageQuery<T>(PageObject<T> pageObj, SqlBuilder sql);
}