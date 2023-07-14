using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LothiumDB.Enumerations
{
    public enum DBCommandType
    {
        None = 0,
        Text = System.Data.CommandType.Text,
        StoredProcedure = System.Data.CommandType.StoredProcedure,
        TableDirect = System.Data.CommandType.TableDirect
    }

    public enum SqlCommandType
    {
        None = 0,
        Select = 1,
        Insert = 2,
        Update = 3,
        Delete = 4,
    }
}
