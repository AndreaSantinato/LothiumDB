namespace LothiumDB.DataAnnotations;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TableAttribute : Attribute
{
    #region Constructors
    
    public TableAttribute(string name) : this(name, string.Empty) { }
    
    public TableAttribute(string name, string schema)
    {
        this.Name = name;
        this.Schema = schema;
    }
    
    #endregion Constructors
    
    #region Property
    
    /// <summary>
    /// Contains the name of the table
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Contains the schema where the table is associated
    /// </summary>
    public string Schema { get; }
    
    #endregion Property
}