// Custom Class
using LothiumDB.Attributes;

// Namespace
namespace LothiumDB.Data.Models;

[TableName("AuditEvents")]
internal sealed class AuditModel
{
    [RequiredColumn]
    [PrimaryKey("AuditID", true)]
    [ColumnName("AuditID")]
    public int Id { get; set; }

    [RequiredColumn]
    [ColumnName("AuditLevel")]
    public string? Level { get; set; }

    [RequiredColumn]
    [ColumnName("AuditUser")]
    public string? User { get; set; }

    [RequiredColumn]
    [ColumnName("ExecutedOn")]
    public DateTime ExecutedOnDate { get; set; }

    [RequiredColumn]
    [ColumnName("DbCommandType")]
    public string? DatabaseCommandType { get; set; }

    [RequiredColumn]
    [ColumnName("SqlCommandType")]
    public string? SqlCommandType { get; set; }

    [ColumnName("SqlCommandOnly")] public string? SqlCommandWithoutParams { get; set; }

    [ColumnName("SqlCommandComplete")] public string? SqlCommandWithParams { get; set; }

    [ColumnName("ErrorMessage")] public string? ErrorMsg { get; set; }
}