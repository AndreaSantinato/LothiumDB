using System.Data.SqlClient;
using LothiumDB;
using LothiumDB.Tests.Testers;

Console.WriteLine("[I] Start Testing Console Project");

using var db = DatabaseBuilder
    .CreateBuilder()
    .SetProvider(
        DatabaseProviderTypesEnum.MicrosoftSqlServer,
        connection: new SqlConnection(
            new SqlConnectionStringBuilder()
            {
                ConnectRetryCount = 2,
                ConnectTimeout = 30,
                DataSource = "192.168.1.124",
                UserID = "SA",
                Password = "SntnAndr28021998",
                InitialCatalog = "LothiumDB_Dev",
                CurrentLanguage = "Italian",
                Encrypt = false,
                TrustServerCertificate = false
            }.ConnectionString
        )
    )
    .SetVariablePrefix("@")
    .SetCommandTimeOut(30)
    .Build();

TestCoreMethods.ExecuteTests(db);
TestExtendedMethods.ExecuteTests(db);
TestTransactionMethods.ExecuteTests(db);

return;
