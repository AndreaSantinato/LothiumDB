using LothiumDB.Enumerations;

namespace LothiumDB;

public interface IDatabaseProvider
{
    DatabaseProviderTypesEnum GetProviderType();

    string GetVariablePrefix();

    SqlBuilder BuildPageQuery<T>(PageObject<T> pageObj, SqlBuilder sql);
}