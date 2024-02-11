namespace LothiumDB.DataAnnotations;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
public class ColumnAttribute : Attribute
{
    private readonly string? _type;

    #region Property

    /// <summary>
    /// Retrieve the name of the column
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Retrieve the type of the column
    /// </summary>
    public string Type => string.IsNullOrEmpty(this._type) ? "Null" : this._type;

    #endregion Property

    #region Constructors

    public ColumnAttribute(string name) : this(name, null) { }

    public ColumnAttribute(string name, string? type)
    {
        this.Name = name;
        this._type = type;
    }

    #endregion Constructors

    
}