// Custom Class
using LothiumDB;
using LothiumDB.Configurations;
using LothiumDB.Data.Providers;
using LothiumDB.Extensions;
using LothiumDB.Tester.TestModels;

Console.WriteLine("Start Testing Console Project");

// Generate a new db configuration and build it //
var config = new DatabaseContextConfiguration
{
    Provider = new MsSqlServerProvider(
        dataSource: "192.168.1.124",
        userId: "SA",
        password: "SntnAndr28021998",
        initialCatalog: "LothiumDB_Dev",
        currentLanguage: "Italian",
        encrypt: false,
        trustServerCertificate: false
    ),
    QueryTimeOut = 30,
    AuditMode = false
};
var db = new DatabaseContext(config);
var sql = new SqlBuilder();

// Execute test examples //

// Remove all the element inside the table
sql.Clear();
sql.DeleteTable("TabellaDiTest");
db.Execute(sql);

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

// Retrieve the elements in the form of an IEnumerable collections
sql.Clear();
sql.Select().From("TabellaDiTest");
var res1 = db.Query<TabellaDiTest>(sql);
var res2 = db.FindAll<TabellaDiTest>(sql);

// Retrieve the elements in the form of a list
sql.Where("Nome = @0", "Prop4");
var res3 = db.FindSingle<TabellaDiTest>(sql);

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
