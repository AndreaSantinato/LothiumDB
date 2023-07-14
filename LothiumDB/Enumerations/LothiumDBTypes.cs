using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LothiumDB.Enumerations
{
    /// <summary>
    /// Indicates the Type of a Database's Provider
    /// </summary>
    public enum ProviderTypes
    {
        None = 0,
        MSSql = 1,
        MySql = 2,
        Oracle = 3,
        Firebird = 4
    }

    /// <summary>
    /// Indicates the Type of a Database's Command
    /// </summary>
    public enum DBCommandType
    {
        None = 0,
        Text = System.Data.CommandType.Text,
        StoredProcedure = System.Data.CommandType.StoredProcedure,
        TableDirect = System.Data.CommandType.TableDirect
    }

    /// <summary>
    /// Indicates the Type of a Sql Command
    /// </summary>
    public enum SqlCommandType
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

    /// <summary>
    /// Defines the Type of operation to be managed
    /// </summary>
    public enum InternalOperationType
    {
        OpenConnection = 0,
        CloseConnection = 1,
        BeginTransaction = 2,
        RollBackTransaction = 3,
        CommitTransaction = 4,
        EnableAuditMode = 5,
        DisableAuditMode = 6,
    }

    /// <summary>
    /// Defines the Type of the Audit event's levels
    /// </summary>
    public enum AuditLevels
    {
        Info = 0,
        Warning = 1,
        Error = 3,
        Fatal = 4,
    }
}
