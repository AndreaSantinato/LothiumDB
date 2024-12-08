namespace LothiumDB;

/// <summary>
/// Define all the different types of operation available inside the Database class
/// </summary>
public enum DatabaseOperationTypesEnum
{
    Scalar = 1,
    ScalarAsync = 2,
    ExecuteQuery = 3,
    ExecuteQueryAsync = 4,
    Query = 5,
    QueryAsync = 6,
    StoredProcedure = 7,
    StoredProcedureAsync = 8,
}

/// <summary>
/// Indicates the type of Database's Provider
/// </summary>
public enum DatabaseProviderTypesEnum
{
    None = 0,
    MicrosoftSqlServer = 1,
    MySql = 2,
    MariaDb = 3,
    PostgreSql = 4,
    Oracle = 5,
    Firebird = 6
}