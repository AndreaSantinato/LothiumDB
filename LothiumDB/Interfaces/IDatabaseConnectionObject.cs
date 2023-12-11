// Namespace
namespace LothiumDB.Interfaces;

internal interface IDatabaseConnectionObject
{
    /// <summary>
    /// Open the current generated connection
    /// </summary>
    void OpenDatabaseConnection();

    /// <summary>
    /// Close the current generated connection
    /// </summary>
    void CloseDatabaseConnection();
}