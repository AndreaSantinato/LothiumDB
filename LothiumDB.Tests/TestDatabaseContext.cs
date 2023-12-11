// Custom Class
using LothiumDB.Configurations;
using LothiumDB.Data.Providers;

// Namespace
namespace LothiumDB.Tests;

public class TestDatabaseContext : DatabaseContext
{
    protected override void SetConfiguration(DatabaseContextConfiguration dbConfiguration)
    {
        dbConfiguration.Provider = new MsSqlServerProvider();
        dbConfiguration.ConnectionString = dbConfiguration
            .Provider
            .CreateConnectionString(
                "192.168.1.124", 
                "SA",
                "SntnAndr28021998",
                "LothiumDB_Dev",
                "Italian",
                false,
                false
        );
        dbConfiguration.AuditMode = false;
        dbConfiguration.AuditUser = "TestingUser";
    }
}