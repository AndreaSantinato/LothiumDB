# LothiumDB [![NuGet Version](https://img.shields.io/nuget/v/LothiumDB.svg?style=flat)](https://www.nuget.org/packages/LothiumDB/) [![NuGet Downloads](https://img.shields.io/nuget/v/LothiumDB.svg?style=flat)](https://www.nuget.org/packages/LothiumDB/)

LothiumDB is a simple micro ORM written completely in C# for fun and offer a lot of different methods to find data in and out of a database. The library has a very simple syntax and give flexibility to the final user if he want to use a query written inside a string variable, or using the SqlBuilder class.
This library is written on the .Net 8 version and utilize some NuGet packages for database communication protocols.

### Getting started

LothiumDB is upload inside the microsoft's NuGet Store and you can get it inside your project using the NuGet Manager inside Visual Studio or with this command:

```nuget
dotnet add package LothiumDB
```

The library was designed to be easy and simple to use.
The first step to do is define what type of database's provider you want to use and set all the required properties.
After define it the next step is to invoke the database class constructor and pass the provider configuration
The library support this different providers:
    - Microsoft Sql Server
    - MySql
    - MariaDB
    - PostgreSQL

```csharp
using LothiumDB;
using LothiumDB.Providers;

var prov = new MsSqlServerProvider(
    dataSource: [INSTANCE NAME OR IP],
    userId: [INSTANCE USER],
    password: [INSTANCE USER'S PASSWORD],
    initialCatalog: [INSTANCE DATABASE'S NAME],
    currentLanguage: [INSTANCE LANGUAGE],
    encrypt: false,
    trustServerCertificate: false
);
var db = new Database(prov, false);
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
var sql = new SqlBuilder()
    .Select()
    .From("TableName")
    .Where("ColumnName = @0", 1);
```

### Database Table Attributes

LothiumDB offers the ability to work with Poco Object to make operations inside the database.
The library work well when a Poco Object have inside it all the needed attribute, in fact all the information about the associated table and column will be extracted automatically.

```csharp
using LothiumDB.Attributes;
namespace TestModels
{
    [Table("Name", "Schema")]
    public class Info
    {
        [PrimaryKey]
        [Position(1)]
        [Column("InfoID", "Guid")]
        public GUID Id { get; set; }
        
        [Nullable]
        [Default("")]
        [Position(2)]
        [Column("InfoDscr", "VarChar")]
        public string? Description { get; set; }
    }
}
```

### Bug Reports

If you want to contribute to this project, you can send a pull request to the GitHub Repository and i'll be happy to add it to the project.
In The feature i'll separate the Master Branch from the Develop Branch

I welcome any type of bug reports and suggestions through my GitHub [Issue Tracker](https://github.com/AndreaSantinato/LothiumDB/issues).

_LothiumDB is copyright &copy; 2024 - Provided under the [GNU General Public License v3.0](https://github.com/AndreaSantinato/LothiumDB/blob/main/LICENSE)._
