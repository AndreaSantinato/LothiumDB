using LothiumDB.Attributes;

namespace LothiumDB.Tester.TestModels
{
    [Serializable]
    [TableName("TabellaDiTest")]
    
    public class TabellaDiTest
    {
        [RequiredColumn]
        [PrimaryKey("Nome")]
        [ColumnName("Nome")]
        public string? PropertyNome { get; set; }

        [ColumnName("Descrizione")]
        public string? PropertyDescrizione { get; set; }
    }
}
