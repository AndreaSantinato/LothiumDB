namespace LothiumDB.Core.PocoDataInfo;

/// <summary>
/// Define the info of an object associated database's table columns
/// </summary>
internal class PocoColumnData
{
    /// <summary>
    /// Indicates the column's name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Indicates the column's type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Indicates the column's value
    /// </summary>
    public object? Value { get; set; } = null;

    /// <summary>
    /// /// Indicates the column default's value
    /// </summary>
    public object? DefaultValue { get; set; } = null;

    /// <summary>
    /// Indicates the column's position
    /// </summary>
    public int Position { get; set; } = 0;
    
    /// <summary>
    /// Indicates if the column accept nullable values
    /// </summary>
    public bool Nullable { get; set; } = true;

    /// <summary>
    /// Indicates if the column is excluded during mapping events
    /// </summary>
    public bool IgnoreMapping { get; set; } = false;

    /// <summary>
    /// Contains all the information if the column is a primary key
    /// </summary>
    public PocoPrimaryKeyData? PrimaryKeyInfo { get; init; } = null;

    /// <summary>
    /// Contains the name of the column mapped property
    /// </summary>
    public string PocoObjectPropertyName { get; set; } = string.Empty;
}