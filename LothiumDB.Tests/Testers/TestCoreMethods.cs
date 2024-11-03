using LothiumDB.Tests.TestModels;
using LothiumDB.Tools;

namespace LothiumDB.Tests.Testers;

public static class TestCoreMethods
{
    public static void ExecuteTests(Database database)
    {
        var rows = database.Execute(
            new SqlBuilder()
                .Append("DELETE FROM TestTable")
        );
        ;
        rows = database.Execute(
            new SqlBuilder().Append("""
                                    
                                                    INSERT INTO [dbo].[TestTable]
                                                    (
                                                        [PropertyName]
                                                        , [PropertyDescription]
                                                        , [PropertyStringValue]
                                                        , [PropertyIntValue]
                                                        , [PropertyDateTimeValue]
                                                    )
                                                    VALUES
                                                        ('Prop1', 'Test Property 1', 'Value 1', 1, GETDATE()),
                                                        ('Prop2', 'Test Property 2', 'Value 2', 2, GETDATE()),
                                                        ('Prop3', 'Test Property 3', 'Value 3', 3, GETDATE()),
                                                        ('Prop4', 'Test Property 4', 'Value 4', 4, GETDATE()),
                                                        ('Prop5', 'Test Property 5', 'Value 5', 5, GETDATE()),
                                                        ('Prop6', 'Test Property 6', 'Value 6', 6, GETDATE())
                                                
                                    """)
        );

        var res1 = database.Query<TestTable>(
            new SqlBuilder()
                .Select()
                .From("TestTable")
        );
    }
}
