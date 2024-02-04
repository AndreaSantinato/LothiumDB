namespace LothiumDB.DataAnnotations;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
public class PositionAttribute : Attribute
{
    #region Constructor
    
    public PositionAttribute(int position)
    {
        this.ColumnPosition = position;
    }
    
    #endregion

    #region Property
    
    /// <summary>
    /// Indicates the position of the column inside the database's table
    /// </summary>
    public int ColumnPosition { get; init; }
    
    #endregion Property
}