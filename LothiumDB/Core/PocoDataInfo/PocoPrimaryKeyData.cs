namespace LothiumDB.Core.PocoDataInfo;

/// <summary>
/// Define the info of an object associated database's table primary keys
/// </summary>
internal class PocoPrimaryKeyData
{
    /// <summary>
    /// Contains the type of the primary key
    /// </summary>
    public Type? PrimaryKeyType { get; set; }

    /// <summary>
    /// Indicates if the primary key have an auto increment identity
    /// </summary>
    public bool? IsAutoIncrementKey { get; set; }
}