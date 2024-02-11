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
            database.BeginTransaction();

            // Create a new element in the database
            var prop7 = new TestTable()
            {
                Name = "Prop7",
                Description = "Property 7",
                Value1 = "Value 7",
                Value2 = 7,
                Value3 = DateTime.Now
            };
            database.Save<TestTable>(prop7);

            // Update the previously created element in the database
            prop7.Description = "Updated Description Of Property 7";
            database.Save<TestTable>(prop7);

            // Delete the previously created and updated element in the database
            database.Delete<TestTable>(prop7);

            database.CommitTransaction();
        }
        catch (Exception e)
        {
            database.RollbackTransaction();
            Console.WriteLine(e);
        }
    }
}
