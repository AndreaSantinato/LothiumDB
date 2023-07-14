using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LothiumDB.Enumerations
{
    public enum SqlTypes
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
    }
}
