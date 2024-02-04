namespace LothiumDB.DataAnnotations;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
public class ColumnAttribute : Attribute
{
    private readonly string? _type;

    #region Constructors
    
    public ColumnAttribute(string name) : this(name, null) { }
    
    public ColumnAttribute(string name, string? type, bool required = false) : this(name, type, -1) { }

    public ColumnAttribute(string name, string? type, int order)
    {
        this.Name = name;
        this.Order = order;
        this._type = type;
    }

    #endregion Constructors

    #region Property
    
    /// <summary>
    /// Retrieve the name of the column
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Retrieve the order where the column is placed
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Retrieve the type of the column
    /// </summary>
    public string Type => string.IsNullOrEmpty(this._type) ? "Null" : this._type;
    
    #endregion Property
}