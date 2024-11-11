namespace LothiumDB.Enumerations;

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
}