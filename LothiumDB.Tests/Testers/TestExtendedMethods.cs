using LothiumDB.Tools;
using LothiumDB.Tests.TestModels;

namespace LothiumDB.Tests.Testers;

public static class TestExtendedMethods
{
    public static void ExecuteTests(Database database)
    {
        var sql = new SqlBuilder();
        
        sql.Clear();
        sql.DeleteTable("TestTable");
        database.Execute(sql);

        // Populate the table with six new elements
        database.Insert<TestTable>(
            new List<TestTable>()
            {
                new TestTable()
        {
            Name = "Prop1",
            Description = "Property Test 1",
            Value1 = "Value 1",
            Value2 = 1,
            Value3 = DateTime.Now
        },
                new TestTable()
        {
            Name = "Prop2",
            Description = "Property Test 2",
            Value1 = "Value 2",
            Value2 = 2,
            Value3 = DateTime.Now
        },
                new TestTable()
        {
            Name = "Prop3",
            Description = "Property Test 3",
            Value1 = "Value 3",
            Value2 = 3,
            Value3 = DateTime.Now
        },
                new TestTable()
        {
            Name = "Prop4",
            Description = "Property Test 4",
            Value1 = "Value 4",
            Value2 = 4,
            Value3 = DateTime.Now
        },
                new TestTable()
        {
            Name = "Prop5",
            Description = "Property Test 5",
            Value1 = "Value 5",
            Value2 = 5,
            Value3 = DateTime.Now
        },
                new TestTable()
        {
            Name = "Prop6",
            Description = "Property Test 6",
            Value1 = "Value 6",
            Value2 = 6,
            Value3 = DateTime.Now
        },
            }
        );

        // Retrieve the elements in the form of an IEnumerable collections
        var res2 = database.FindAll<TestTable>();

        // Retrieve the elements in the form of a list
        sql.Clear();
        sql.Select()
           .From("TestTable")
           .Where("Nome = @0", "Prop4");
        var res3 = database.FindSingle<TestTable>(sql);

        sql.Dispose();
    }
}
