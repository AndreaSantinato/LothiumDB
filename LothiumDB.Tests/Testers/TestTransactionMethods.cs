using LothiumDB.Core;
using LothiumDB.Tools;
using LothiumDB.Tests.TestModels;

namespace LothiumDB.Tests.Testers;

public static class TestTransactionMethods
{
    public static void ExecuteTests(Database database)
    {
        try
        {
            var sql = new SqlBuilder();

            database.BeginTransaction();

            // Create a new element in the database
            sql.Clear();
            sql.Append(@"
                INSERT INTO [dbo].[TestTable]
                (
                    [PropertyName]
                    , [PropertyDescription]
                    , [PropertyStringValue]
                    , [PropertyIntValue]
                    , [PropertyDateTimeValue]
                )
                VALUES
                (
                    'Prop7'
                    , 'Test Property 7'
                    , 'Value 7'
                    , 7
                    , GETDATE()
                )
            ");
            var rows = database.Execute(sql);

            // Update the previously created element in the database
            sql.Clear();
            sql.Append(@"
                UPDATE  [dbo].[TestTable]
                SET     [PropertyDescription] = 'Updated Description Of Property 7'
                WHERE   [PropertyName] = 'Prop7'
            ");
            rows = database.Execute(sql);

            // Delete the previously created and updated element in the database
            sql.Clear();
            sql.Append(@"
                DELETE  FROM [dbo].[TestTable]
                WHERE   [PropertyName] = 'Prop7'
            ");
            rows = database.Execute(sql);

            database.CommitTransaction();
        }
        catch (Exception e)
        {
            database.RollbackTransaction();
            Console.WriteLine(e);
        }
    }
}
