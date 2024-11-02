using LothiumDB.Core.Enumerations;
using LothiumDB.Tools;

namespace LothiumDB;

public interface IDatabaseProvider
{
    ProviderTypesEnum GetProviderType();

    string GetVariablePrefix();

    SqlBuilder BuildPageQuery<T>(PageObject<T> pageObj, SqlBuilder sql);
}