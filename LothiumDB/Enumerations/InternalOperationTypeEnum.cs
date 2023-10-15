// System Class
using System;

namespace LothiumDB.Enumerations
{
    /// <summary>
    /// Defines the Type of operation to be managed
    /// </summary>
    public enum InternalOperationTypeEnum
    {
        OpenConnection = 0,
        CloseConnection = 1,
        BeginTransaction = 2,
        RollBackTransaction = 3,
        CommitTransaction = 4,
        EnableAuditMode = 5,
        DisableAuditMode = 6,
    }
}
