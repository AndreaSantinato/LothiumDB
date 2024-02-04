using LothiumDB.DataAnnotations;

namespace LothiumDB.Tests.TestModels;

[Table("TabellaDiTest", "dbo")]
public class TestTable
{
    [Nullable]
    [Default("")]
    [Position(1)]
    [PrimaryKey]
    [Column("Nome", "VARCHAR", 1)]
    public string? PropertyName { get; set; }
    
    [Nullable]
    [Default("")]
    [Position(2)]
    [Column("Descrizione", "VARCHAR", 2)]
    public string? PropertyDescription { get; set; }
}