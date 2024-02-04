using System.Reflection;
using LothiumDB.DataAnnotations;

namespace LothiumDB.Core.PocoDataInfo;

internal sealed class PocoObject<T>
{
    private readonly Type _objectType = typeof(T);
    
    #region Class Property
    
    /// <summary>
    /// Store all the information about database's table
    /// </summary>
    public PocoTableData? TableDataInfo { get; private set; }

    /// <summary>
    /// Store all the information about the database table's columns values
    /// </summary>
    public List<PocoColumnData>? ColumnDataInfo { get; private set; }
    
    /// <summary>
    /// Indicates if the mapper process was completed
    /// If one error occured the mapping is considered not completed
    /// </summary>
    public bool IsMappingComplete { get; set; }

    #endregion

    #region Class Constructor

    /// <summary>
    /// Class Constructor
    /// </summary>
    public PocoObject() => InitializeObject();

    /// <summary>
    /// Class Constructor
    /// </summary>
    public PocoObject(object obj) => InitializeObject(obj);

    #endregion

    /// <summary>
    /// Initialize this poco instance
    /// </summary>
    /// <param name="obj">Contains an object with all the information stored inside</param>
    private void InitializeObject(object? obj = null)
    {
        try
        {
            // Check if the passed object is null
            ArgumentNullException.ThrowIfNull(obj);

            TableDataInfo = AutoMapper.RetrieveTableData(_objectType);
            ArgumentNullException.ThrowIfNull(TableDataInfo, nameof(TableDataInfo));
            
            foreach (var prop in GetProperties(_objectType))
            {
                ColumnDataInfo ??= new List<PocoColumnData>();

                var columnData = AutoMapper.RetrieveColumnData(prop, prop.GetValue(prop));
                ArgumentNullException.ThrowIfNull(columnData, nameof(columnData));
                
                ColumnDataInfo.Add(columnData);
            }
            
            IsMappingComplete = true;
        }
        catch (Exception ex)
        {
            IsMappingComplete = false;
        }
    }

    /// <summary>
    /// Retrieve all the DB Mapped Properties and Exclude all the Property with the ExcludeColumn Attribute
    /// </summary>
    /// <param name="objType">Contains the type of the poco object that store all the information</param>
    /// <returns>An array with all the properties of the object</returns>
    public PropertyInfo[] GetProperties(Type objType)
    {
        PropertyInfo[]? propertyInfo = null;

        try
        {
            propertyInfo = objType
                .GetProperties()
                .Where(pi => pi.GetCustomAttributes(typeof(IgnoreAttribute), true).Length == 0)
                .ToArray();
        }
        catch (Exception ex)
        {
            propertyInfo = objType.GetProperties();
        }

        return propertyInfo;
    }
}