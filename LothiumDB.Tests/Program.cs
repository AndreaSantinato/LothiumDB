// System Class
using System.Data;
// Custom Class
using LothiumDB.Extensions;
using LothiumDB.Tester.TestModels;
using LothiumDB.Tests;

Console.WriteLine("Start Testing Console Project");

// Generate a new db configuration and build it //
var db = new TestDatabaseContext();

// Execute test examples //

// Remove all the element inside the table
db.Execute(new SqlBuilder().DeleteTable("TabellaDiTest"));

// Populate the table with six new elements
db.Insert<TabellaDiTest>(
    new List<TabellaDiTest>()
    {
        new TabellaDiTest() { PropertyNome = "Prop1", PropertyDescrizione = "Property 1" },
        new TabellaDiTest() { PropertyNome = "Prop2", PropertyDescrizione = "Property 2" },
        new TabellaDiTest() { PropertyNome = "Prop3", PropertyDescrizione = "Property 3" },
        new TabellaDiTest() { PropertyNome = "Prop4", PropertyDescrizione = "Property 4" },
        new TabellaDiTest() { PropertyNome = "Prop5", PropertyDescrizione = "Property 5" },
        new TabellaDiTest() { PropertyNome = "Prop6", PropertyDescrizione = "Property 6" }
    }
);

// Retrieve an IEnumerable result
var res1 = db.Query<TabellaDiTest>(new SqlBuilder().Select("TabellaDiTest"));
var res2 = db.FindAll<TabellaDiTest>(new SqlBuilder().Select("TabellaDiTest"));

// Retrieve an IList result
var res3 = db.FindSingle<TabellaDiTest>(new SqlBuilder().Select("TabellaDiTest").Where("Nome = @0", "Prop4"));

// Insert/Update/Delete
try
{
    db.BeginTransaction();

    // Create a new element in the database
    var prop7 = new TabellaDiTest()
    {
        PropertyNome = "Prop7",
        PropertyDescrizione = "Property 7"
    };
    db.Save<TabellaDiTest>(prop7);
    
    // Update the previously created element in the database
    prop7.PropertyDescrizione = "Updated Description Of Property 7";
    db.Save<TabellaDiTest>(prop7);
    
    // Delete the previously created and updated element in the database
    db.Delete<TabellaDiTest>(prop7);
    
    db.CommitTransaction();
}
catch (Exception e)
{
    db.RollbackTransaction();
    Console.WriteLine(e);
}
finally
{
    db.Dispose();
    db = null;
}

return;
