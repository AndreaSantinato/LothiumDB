using System.Collections;
using System.Data;
using System.Reflection;
using LothiumDB.Exceptions;

namespace LothiumDB.Helpers;

/// <summary>
/// Utility centralized class specifically designed to retrieve and map a collection of data
/// from the database instance using a specific query or stored procedure
/// </summary>
/// <param name="reader"></param>
internal class ReaderManager(IDataReader reader, params Type[] types) : IDisposable
{
    private bool _disposed;
    private int _typesIndexPosition = 0;

    /// <summary>
    /// Dispose the current instance
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            reader.Dispose();
        }
        
        GC.SuppressFinalize(this);
        
        _disposed = true;
    }

    /// <summary>
    /// Retrieve a collection of data and automatically map the results inside object of the chosen type
    /// </summary>
    /// <returns>A collection of the chosen object's type</returns>
    internal IEnumerable<object>? RetrieveData()
    {
        var objectType = types[_typesIndexPosition];
        
        var listResults = (IList?)Activator.CreateInstance(typeof(List<>).MakeGenericType(objectType));
        DatabaseException.ThrowIfNull(listResults);
        
        var mapper = new AutoMapper(objectType);
        var props = AutoMapper.GetMappedProperties(objectType);
        
        while (reader.Read())
        {
            if (reader.FieldCount <= 0) 
                continue;

            var item = MapItem(objectType, mapper, props);

            if (item is not null)
                listResults?.Add(item);
        }

        return (IEnumerable<object>?)listResults;
    }

    private object? MapItem(Type objType, AutoMapper mapper, PropertyInfo[] props)
    {
        var item = Activator.CreateInstance(objType);

        ArgumentNullException.ThrowIfNull(mapper.TableData, nameof(mapper.TableData));
        ArgumentNullException.ThrowIfNull(mapper.ColumnsData, nameof(mapper.ColumnsData));

        foreach (var prop in props)
        {
            var colInfo = Array.Find(
                mapper.ColumnsData.ToArray(),
                col => col.PocoObjectPropertyName == prop.Name
            );
            ArgumentNullException.ThrowIfNull(colInfo, nameof(colInfo));

            var value = (string.IsNullOrEmpty(colInfo.Name))
                ? reader[colInfo.PocoObjectPropertyName]
                : reader[colInfo.Name];
                
            Utility.VerifyDbNullValue(colInfo, ref value);

            prop.SetValue(item, value, null);
        }

        return item;
    }
}