using LothiumDB.Enumerations;
using LothiumDB.Tools;

namespace LothiumDB;

public interface IDatabaseProvider
{
    DatabaseProviderTypesEnum GetProviderType();

    string GetVariablePrefix();

    SqlBuilder BuildPageQuery<T>(PageObject<T> pageObj, SqlBuilder sql);
}