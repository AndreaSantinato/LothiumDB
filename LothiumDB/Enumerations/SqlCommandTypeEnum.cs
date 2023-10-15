// System Class
using System;

namespace LothiumDB.Enumerations
{
    /// <summary>
    /// Indicates the Type of a Sql Command
    /// </summary>
    public enum SqlCommandTypeEnum
    {
        None = 0,
        Select = 1,
        From = 2,
        Where = 3,
        OrderBy = 4,
        GroupBy = 5,
        InnerJoin = 6,
        LeftJoin = 7,
        RightJoin = 8,
        OuterJoin = 9,
        LeftOuterJoin = 10,
        RightOuterJoin = 11,
        FullOuterJoin = 12,
        Insert = 13,
        Update = 14,
        Delete = 15,
    }
}
