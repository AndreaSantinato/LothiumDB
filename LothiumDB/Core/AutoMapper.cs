using System.Reflection;
using LothiumDB.Core.PocoDataInfo;
using LothiumDB.DataAnnotations;

namespace LothiumDB.Core;

/// <summary>
/// AutoMapper class that provides a set of methods to automatically extract
/// information stored inside a class using the dedicated data annotations
/// </summary>
internal static class AutoMapper
{
    /// <summary>
    /// Find all the information about the object class if it contains the necessary attribute
    /// </summary>
    /// <param name="tableType">Contains the object's type</param>
    /// <returns>The table information</returns>
    public static PocoTableData? RetrieveTableData(Type tableType)
    {
        var tbData = tableType.GetCustomAttribute(typeof(TableAttribute)) as TableAttribute;
        
        if (tbData is null) return null;
        
        return new PocoTableData
        {
            PocoObjectClassName = tableType.Name,
            TableName = tbData.Name,
            TableSchema = tbData.Schema
        };
    }

    /// <summary>
    /// Find all the information about an object property if it contains the necessary attributes
    /// </summary>
    /// <param name="propertyInfo">Contains the property's information</param>
    /// <param name="propertyValue">Contains the property's value</param>
    /// <returns>The Column Information</returns>
    public static PocoColumnData? RetrieveColumnData(PropertyInfo propertyInfo, object? propertyValue)
    {
        var type = propertyInfo.GetType(); 
        var nullableInfo = type.GetCustomAttribute(typeof(NullableAttribute)) as NullableAttribute;
        var ignoreInfo = type.GetCustomAttribute(typeof(IgnoreAttribute)) as IgnoreAttribute;
        var positionInfo = type.GetCustomAttribute(typeof(PositionAttribute)) as PositionAttribute;
        var defaultInfo = type.GetCustomAttribute(typeof(DefaultAttribute)) as DefaultAttribute;
        var pkeyInfo = type.GetCustomAttribute(typeof(PrimaryKeyAttribute)) as PrimaryKeyAttribute;
        var columnInfo = type.GetCustomAttribute(typeof(ColumnAttribute)) as ColumnAttribute;
        
        if (columnInfo is null) return null;
        
        return new PocoColumnData
        {
            Name = columnInfo.Name,
            Type = columnInfo.Type,
            Value = propertyValue,
            DefaultValue = defaultInfo?.DefaultValue,
            Position = positionInfo?.ColumnPosition ?? 0,
            Nullable = nullableInfo is not null,
            IgnoreMapping = ignoreInfo is not null,
            PrimaryKeyInfo = (pkeyInfo is null) 
                ? null 
                : new PocoPrimaryKeyData
                  {
                     PrimaryKeyType = propertyInfo.PropertyType,
                     IsAutoIncrementKey = pkeyInfo.IsAutoIncrementKey
                  },
            PocoObjectPropertyName = propertyInfo.Name
        };
    }
}