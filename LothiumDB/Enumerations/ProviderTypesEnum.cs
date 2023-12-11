// System Class
using System;

namespace LothiumDB.Enumerations
{
    /// <summary>
    /// Indicates the Type of a Database's Provider
    /// </summary>
    public enum ProviderTypesEnum
    {
        None = 0,
        MicrosoftSqlServer = 1,
        MySql = 2,
        MariaDb = 3,
        PostgreSql = 4,
        Oracle = 5,
        Firebird = 6
    }
}
