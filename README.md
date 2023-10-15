# LothiumDB [![NuGet Version](https://img.shields.io/nuget/v/LothiumDB.svg?style=flat)](https://www.nuget.org/packages/LothiumDB/) [![NuGet Downloads](https://img.shields.io/nuget/v/LothiumDB.svg?style=flat)](https://www.nuget.org/packages/LothiumDB/)

LothiumDB is a simple micro ORM for .Net applications written entirely in C# for fun and offer a lot of different methods to find data in and out of a database. The library has a very simple syntax and give flexibility to the final user if he want to use a query written inside a string variable, or using the SqlBuilder class.

```csharp
db.query<PocoType>("SELECT * FROM TABLE");

var obj = new PocoObject()
{
    Prop1 = "Prop1",
    Prop2 = "Prop2";  
};
db.Insert<PocoType>(obj);
```

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

### Getting started

You can install LothiumDB directly from NuGet using the NuGet Manager inside Visual Studio or with this command:

```
dotnet add package LothiumDB
```

The simplest way to set up LothiumDB is using the `Database` class and the `SqlBuilder` query constructor.

```csharp
using LothiumDB;

public class Program
{
    // Instance the configuration object
    var config = new DatabaseConfiguration();
    // Set the provider settings
    config.Provider
        .AddProvider(providerName: "MSSqlServer")
        .AddConnectionString(connectionString);
    // Set the audit settings
    config.Audit
        .AddAudit()
        .SetUser(auditUser: "DatabaseAdmin");
    // Create the new db instance
    var db = config.BuildDatabase(); 
    
    // Execute the first query
    var sql = new SqlBuilder().Select("*").From("TableName"); 
    List<PocoObject> list = db.FetchAll<PocoObject>(sql);
    
    // Execute the second query
    var sql2 = new SqlBuilder().SelectTop(1, "*").From("TableName"); 
    PocoObject pocoObj = db.FetchSingle<PocoObject>(sql);
}
```

### Bug Reports

If you want to contribute to this project, you can send a pull request to the GitHub Repository and i'll be happy to add it to the project.
In The feature i'll separate the Master Branch from the Develop Branch

I welcome any type of bug reports and suggestions through my GitHub [Issue Tracker](https://github.com/AndreaSantinato/LothiumDB/issues).

_LothiumDB is copyright &copy; 2023 - Provided under the [GNU General Public License v3.0](https://github.com/AndreaSantinato/LothiumDB/blob/main/LICENSE)._
