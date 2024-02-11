using LothiumDB.DataAnnotations;

namespace LothiumDB.Tests.TestModels;

[Table("TestTable", "dbo")]
public class TestTable
{
    [PrimaryKey]
    [Position(1)]
    [Column("PropertyName", "VARCHAR")]
    public string Name { get; set; } = string.Empty;
    
    [Nullable]
    [Default("")]
    [Position(2)]
    [Column("PropertyDescription", "VARCHAR")]
    public string? Description { get; set; }

    [Nullable]
    [Default("")]
    [Position(3)]
    [Column("PropertyStringValue", "VARCHAR")]
    public string? Value1 { get; set; }

    [Nullable]
    [Default(0)]
    [Position(4)]
    [Column("PropertyIntValue", "INT")]
    public int? Value2 { get; set; }

    [Nullable]
    [Default("")]
    [Position(5)]
    [Column("PropertyDateTimeValue", "DATETIME")]
    public DateTime? Value3 { get; set; }

    [Ignore]
    public string CombinedValues 
        => string.Concat(Value1, Value2, Value3);
}