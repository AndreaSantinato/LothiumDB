using LothiumDB;
using LothiumDB.Providers;
using LothiumDB.Tools;
using LothiumDB.Tests.Testers;

Console.WriteLine("Start Testing Console Project");

// Generate a new db instance from a specific provider configuration //
var prov = new MsSqlServerProvider(
    dataSource: "192.168.1.124",
    userId: "SA",
    password: "SntnAndr28021998",
    initialCatalog: "LothiumDB_Dev",
    currentLanguage: "Italian",
    encrypt: false,
    trustServerCertificate: false
);
var db = new Database(prov, false);
var sql = new SqlBuilder();

// Execute test examples //

TestCoreMethods.ExecuteTests(db);
TestExtendedMethods.ExecuteTests(db);
TestTransactionMethods.ExecuteTests(db);

// Dispose the database instance //

db.Dispose();
db = null;

return;
