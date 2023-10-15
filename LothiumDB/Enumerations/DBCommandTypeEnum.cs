// System Class
using System;

namespace LothiumDB.Enumerations
{
    /// <summary>
    /// Indicates the Type of a Database's Command
    /// </summary>
    public enum DBCommandTypeEnum
    {
        None = 0,
        Text = System.Data.CommandType.Text,
        StoredProcedure = System.Data.CommandType.StoredProcedure,
        TableDirect = System.Data.CommandType.TableDirect
    }
}
