using LothiumDB;
using LothiumDB.Configurations;
using LothiumDB.DatabaseProviders;
using LothiumDB.Tests.TestModels;

Console.WriteLine("Start Testing Console Project");

// Generate a new db configuration and build it //
var config = new DatabaseConfiguration
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
var db = new Database(config);
var sql = new SqlBuilder();

// Execute test examples //

// Remove all the element inside the table
sql.Clear();
sql.DeleteTable("TabellaDiTest");
db.Execute(sql);

// Populate the table with six new elements
db.Insert<TestTable>(
    new List<TestTable>()
    {
        new TestTable() { PropertyName = "Prop1", PropertyDescription = "Property 1" },
        new TestTable() { PropertyName = "Prop2", PropertyDescription = "Property 2" },
        new TestTable() { PropertyName = "Prop3", PropertyDescription = "Property 3" },
        new TestTable() { PropertyName = "Prop4", PropertyDescription = "Property 4" },
        new TestTable() { PropertyName = "Prop5", PropertyDescription = "Property 5" },
        new TestTable() { PropertyName = "Prop6", PropertyDescription = "Property 6" }
    }
);

// Retrieve the elements in the form of an IEnumerable collections
sql.Clear();
sql.Select().From("TabellaDiTest");
var res1 = db.Query<TestTable>(sql);
var res2 = db.FindAll<TestTable>(sql);

// Retrieve the elements in the form of a list
sql.Where("Nome = @0", "Prop4");
var res3 = db.FindSingle<TestTable>(sql);

// Insert/Update/Delete
try
{
    db.BeginTransaction();

    // Create a new element in the database
    var prop7 = new TestTable()
    {
        PropertyName = "Prop7",
        PropertyDescription = "Property 7"
    };
    db.Save<TestTable>(prop7);
    
    // Update the previously created element in the database
    prop7.PropertyDescription = "Updated Description Of Property 7";
    db.Save<TestTable>(prop7);
    
    // Delete the previously created and updated element in the database
    db.Delete<TestTable>(prop7);
    
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
