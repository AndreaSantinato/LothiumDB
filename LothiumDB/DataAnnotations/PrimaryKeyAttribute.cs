namespace LothiumDB.DataAnnotations;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
public class PrimaryKeyAttribute : Attribute
{
    #region Constructors
    
    public PrimaryKeyAttribute() : this(false) { }
    
    public PrimaryKeyAttribute(bool isAutoIncremenetKey)
        => this.IsAutoIncrementKey = isAutoIncremenetKey;

    #endregion Constructors
    
    #region Property
    
    /// <summary>
    /// Indicates if the key has an auto increment value
    /// </summary>
    public bool IsAutoIncrementKey { get; }
    
    #endregion Property
}