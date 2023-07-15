using LothiumDB.Attributes;

namespace LothiumDB.Tester.TestModels
{
    [Serializable]
    [TableName("TabellaDiTest")]
    [PrimaryKey("Nome")]
    public class TabellaDiTest
    {
        [ColumnName("Nome")]
        public string? PropertyNome { get; set; }

        [ColumnName("Descrizione")]
        public string? PropertyDescrizione { get; set; }
    }
}
