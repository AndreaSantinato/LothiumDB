# LothiumDB [![NuGet Version](https://img.shields.io/nuget/v/LothiumDB.svg?style=flat)](https://www.nuget.org/packages/LothiumDB/) [![NuGet Downloads](https://img.shields.io/nuget/v/LothiumDB.svg?style=flat)](https://www.nuget.org/packages/LothiumDB/)

LothiumDB is a simple micro ORM written completely in C# for fun and offer a lot of different methods to find data in and out of a database. The library has a very simple syntax and give flexibility to the final user if he want to use a query written inside a string variable, or using the SqlBuilder class.
This library is written on the .Net 6 version and utilize some NuGet packages for database communication protocols.

### Getting started

LothiumDB is upload inside the microsoft's NuGet Store and you can get it inside your project using the NuGet Manager inside Visual Studio or with this command:

```nuget
dotnet add package LothiumDB
```

The library was designed to be easy and simple to use.
The first step is to create a new class that will be your database context and extend the **`Database`** class.
After this step you need to override the **`SetConfiguration`** and put your specific configuration inside it as shown below:

```csharp
// LothiumDB Classes
using LothiumDB.Configurations;
using LothiumDB.Data.Providers;

public class UserDatabase : Database
{
    protected override void SetConfiguration(DatabaseContextConfiguration dbConfiguration)
    {
        // Choose the database provider and the value for the connection's string 
        // The connection string generator accept an array of values ore the final formatted connection string
        dbConfiguration.Provider = new MsSqlServerProvider();
        dbConfiguration.ConnectionString = dbConfiguration
            .Provider
            .CreateConnectionString(
                "localhost", // DB's Instance
                "lt", // USer
                "pswd", // Password
                "TEST", // DB's Name
                "Italian", // Language
                false, // Encrypt
                false // TrustServerCertificate
            );
        
        // Choose to enable or disable the Audit sink
        dbConfiguration.AuditMode = false;
        dbConfiguration.AuditUser = "TestingUser";
    }
}
```

After this step you are ready to go! Inside your classes you can use your name database context class simply by create a new instance of as shown below:

```csharp
var db = new UserDatabase();
db.Query<Type>("SELECT * FROM TableName");
```

### Query Builder

The library offers the built-in class to generate automatically a new query in the simple way possible.
To utilize it you will need to instance of the SqlBuilder class and start using its methods as shown below:

```csharp
var sql = new SqlBuilder().
    Select("*")
    .From("TableName")
    .Where("Prop = @0", 1);
```

### Database Table Attributes

LothiumDB offers the ability to work with Poco Object to make operations inside the database.
The library work well when a Poco Object have inside it all the needed attribute, in fact all the information about the associated table and column will be extracted automatically.

```csharp
using LothiumDB.Attributes;
namespace TestModels
{
    [Serializable]
    [TableName("TableName")]
    [PrimaryKey("Prop1")]
    public class TabellaDiTest
    {
        [ColumnName("Prop1")] public string? Property1 { get; set; }
        [ColumnName("Prop2")] public string? Property2 { get; set; }
    }
}
```

### Bug Reports

If you want to contribute to this project, you can send a pull request to the GitHub Repository and i'll be happy to add it to the project.
In The feature i'll separate the Master Branch from the Develop Branch

I welcome any type of bug reports and suggestions through my GitHub [Issue Tracker](https://github.com/AndreaSantinato/LothiumDB/issues).

_LothiumDB is copyright &copy; 2023 - Provided under the [GNU General Public License v3.0](https://github.com/AndreaSantinato/LothiumDB/blob/main/LICENSE)._
