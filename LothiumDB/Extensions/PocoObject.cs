// System Class
using System.Reflection;
// Custom Class
using LothiumDB.Attributes;
using LothiumDB.Core.PocoDataInfos;

// Custom Class

namespace LothiumDB.Extensions;

internal sealed class PocoObject<T>
{
    #region Class Property

    /// <summary>
    /// Store all the information about database's table
    /// </summary>
    public PocoTableData? TableDataInfo { get; private set; }

    /// <summary>
    /// Store all the information about the database table's columns values
    /// </summary>
    public List<PocoColumnsData>? ColumnDataInfo { get; private set; }

    /// <summary>
    /// Store an occured generated exception during the poco object data mapping
    /// </summary>
    public Exception? MappingException { get; internal set; }

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

    #region Class Methods

    /// <summary>
    /// Initialize this poco instance
    /// </summary>
    /// <param name="obj">Contains an object with all the information stored inside</param>
    private void InitializeObject(object? obj = null)
    {
        try
        {
            // Retrieve the type of the passed object
            var pocoObjType = typeof(T);

            // Retrieve all the table's information
            var tbAttrData =
                Attribute.GetCustomAttribute(pocoObjType, typeof(TableNameAttribute)) as TableNameAttribute;
            if (tbAttrData == null) throw new ArgumentNullException(nameof(tbAttrData));

            TableDataInfo = new PocoTableData(typeof(T).Name, tbAttrData.Table);
            if (TableDataInfo == null) throw new ArgumentNullException(nameof(TableDataInfo));

            // Retrieve all the table's columns information
            var pInfo = GetProperties(pocoObjType);
            if (pInfo == null) throw new ArgumentNullException(nameof(pInfo));

            foreach (var p in pInfo)
            {
                ColumnDataInfo ??= new List<PocoColumnsData>();

                var colNameAttrData =
                    Attribute.GetCustomAttribute(p, typeof(ColumnNameAttribute)) as ColumnNameAttribute;
                var colRequiredAttrData =
                    Attribute.GetCustomAttribute(p, typeof(RequiredColumnAttribute)) as RequiredColumnAttribute;
                var colPrimaryKeyAttrData =
                    Attribute.GetCustomAttribute(p, typeof(PrimaryKeyAttribute)) as PrimaryKeyAttribute;
                var colExcludedColAttrData =
                    Attribute.GetCustomAttribute(p, typeof(ExcludeColumnAttribute)) as ExcludeColumnAttribute;

                ColumnDataInfo.Add(new PocoColumnsData(
                    p.Name,
                    colRequiredAttrData == null ? false : colRequiredAttrData.IsRequired,
                    colExcludedColAttrData != null ? colExcludedColAttrData.IsExcluded : false,
                    colNameAttrData is null ? null : colNameAttrData.Column,
                    p.PropertyType.Name.ToString(),
                    obj is not null ? p.GetValue(obj) : null,
                    colPrimaryKeyAttrData == null
                        ? null
                        : new PocoPrimaryKeyData(p.GetType(), colPrimaryKeyAttrData.IsAutoIncremenetKey)));
            }
        }
        catch (Exception ex)
        {
            MappingException = ex;
        }
    }

    /// <summary>
    /// Retrieve all the DB Mapped Properties and Exclude all the Property with the ExcludeColumn Attribute
    /// </summary>
    /// <param name="objType">Contains the type of the poco object that store all the informations</param>
    /// <returns>An array with all the properties of the object</returns>
    public PropertyInfo[] GetProperties(Type objType)
    {
        PropertyInfo[]? propertyInfo = null;

        try
        {
            propertyInfo = objType.GetProperties()
                .Where(pi => pi.GetCustomAttributes(typeof(ExcludeColumnAttribute), true).Length == 0).ToArray();
        }
        catch (Exception ex)
        {
            propertyInfo = objType.GetProperties();
        }

        return propertyInfo;
    }

    #endregion
}