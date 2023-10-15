// System Class
using System.Data;
// Custom Class
using LothiumDB;
using LothiumDB.Configurations;
using LothiumDB.Tester.TestModels;

Console.WriteLine("Start Testing Console Project");

// Generate a new db configuration and build it //

var config = new DatabaseConfiguration();
config.Provider
    .AddProvider(providerName: "MSSqlServer")
    .AddConnectionString("192.168.1.124", "SA", "SntnAndr28021998", "LothiumDB_Dev", "Italian", false, false);
config.Audit
    .AddAudit()
    .SetUser(auditUser: "DatabaseAdmin");
var db = config.BuildDatabase();

// Execute test examples //

// Remove all the element inside the table
db.Execute(new SqlBuilder().DeleteTable("TabellaDiTest"));

// Populate the table with six new elements
db.Insert<TabellaDiTest>(new TabellaDiTest() { PropertyNome = "Prop1", PropertyDescrizione = "Property 1" });
db.Insert<TabellaDiTest>(new TabellaDiTest() { PropertyNome = "Prop2", PropertyDescrizione = "Property 2" });
db.Insert<TabellaDiTest>(new TabellaDiTest() { PropertyNome = "Prop3", PropertyDescrizione = "Property 3" });
db.Insert<TabellaDiTest>(new TabellaDiTest() { PropertyNome = "Prop4", PropertyDescrizione = "Property 4" });
db.Insert<TabellaDiTest>(new TabellaDiTest() { PropertyNome = "Prop5", PropertyDescrizione = "Property 5" });
db.Insert<TabellaDiTest>(new TabellaDiTest() { PropertyNome = "Prop6", PropertyDescrizione = "Property 6" });

// Retrieve an IEnumerable result
var sql = new SqlBuilder().Select("*").From("TabellaDiTest");
var res1 = db.Query<TabellaDiTest>(sql);
var res2 = db.FindAll<TabellaDiTest>(sql);

// Retrieve an IList result
sql = new SqlBuilder().Select("*").From("TabellaDiTest").Where("Nome = @0", "Prop4");
var res3 = db.FindSingle<TabellaDiTest>(sql);

// Insert/Update/Delete
try
{
    db.BeginTransaction();

    // Create a new element in the database
    var prop7 = new TabellaDiTest() { PropertyNome = "Prop7", PropertyDescrizione = "Property 7" };
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
