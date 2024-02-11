using System.Reflection;
using System.Text;
using LothiumDB.Core.PocoDataInfo;
using LothiumDB.DataAnnotations;
using LothiumDB.Tools;

namespace LothiumDB.Core;

/// <summary>
/// AutoMapper class that provides a set of methods to automatically extract
/// information stored inside a class using the dedicated data annotations
/// </summary>
internal class AutoMapper
{
    #region Property
    
    /// <summary>
    /// Contains all the table information store inside an object's type
    /// </summary>
    public PocoTableData? TableData { get; set; }

    /// <summary>
    /// Contains all the table's column information store inside an object's type
    /// </summary>
    public List<PocoColumnData>? ColumnsData { get; set; }

    #endregion Property

    #region Constructors

    /// <summary>
    /// Defaut constructor
    /// </summary>
    public AutoMapper()
    {
        this.TableData = null;
        this.ColumnsData = null;
    }

    /// <summary>
    /// Constructor that define all the mapper properties
    /// </summary>
    /// <param name="type">Contains the object's type</param>
    /// <param name="obj"></param>
    public AutoMapper(Type type, object? obj = null) : this()
    {
        this.TableData = RetrieveTableData(type);
        this.ColumnsData = RetrieveColumnsData(type, obj);
    }

    #endregion Constructors

    #region Object Mapper Methods

    /// <summary>
    /// Retrieve all the table information stored inside an object's type
    /// that use the TableAttribute data annotation
    /// </summary>
    /// <param name="type">Contains the object's type that store all the information</param>
    /// <returns>The table information</returns>
    public static PocoTableData? RetrieveTableData(Type type)
    {
        var tbData = type.GetCustomAttribute(typeof(TableAttribute)) as TableAttribute;     
        return new PocoTableData
        {
            PocoObjectClassName = type.Name,
            TableName = (tbData is null) 
                ? string.Empty
                : tbData.Name,
            TableSchema = (tbData is null) 
                ? string.Empty 
                : tbData.Schema
        };
    }

    /// <summary>
    /// Retrieve all the column information for each property of an object's type
    /// that use this data annotations:
    ///     - ColumnAttribute
    ///     - PrimaryKeyAttribute
    ///     - NullableAttribute
    ///     - IgnoreAttribute
    ///     - DefaultAttribute
    /// </summary>
    /// <param name="type">Contains the object's type that store all the information</param>
    /// <param name="obj"></param>
    /// <returns>The columns information</returns>
    public static List<PocoColumnData>? RetrieveColumnsData(Type type, object? obj = null) 
    {
        var data = new List<PocoColumnData>();
        var props = type.GetProperties();

        foreach (var prop in props)
        {
            var columnInfo = prop.GetCustomAttribute(typeof(ColumnAttribute)) as ColumnAttribute;
            var primaryKeyInfo = prop.GetCustomAttribute(typeof(PrimaryKeyAttribute)) as PrimaryKeyAttribute;
            var defaultInfo = prop.GetCustomAttribute(typeof(DefaultAttribute)) as DefaultAttribute;
            var positionInfo = prop.GetCustomAttribute(typeof(PositionAttribute)) as PositionAttribute;
            var nullableInfo = prop.GetCustomAttribute(typeof(NullableAttribute)) as NullableAttribute;
            var ignoreInfo = prop.GetCustomAttribute(typeof(IgnoreAttribute)) as IgnoreAttribute;

            var propData = new PocoColumnData
            {
                Name = (columnInfo is null)
                    ? string.Empty
                    : columnInfo.Name,
                Type = (columnInfo is null)
                    ? string.Empty
                    : columnInfo.Type,
                Value = (obj is null)
                    ? null
                    : prop.GetValue(obj),
                DefaultValue = defaultInfo?.DefaultValue,
                Position = positionInfo?.ColumnPosition ?? 0,
                Nullable = (nullableInfo is not null),
                IgnoreMapping = (ignoreInfo is not null),
                PrimaryKeyInfo = (primaryKeyInfo is null)
                    ? null
                    : new PocoPrimaryKeyData
                        {
                            PrimaryKeyType = prop.PropertyType,
                            IsAutoIncrementKey = primaryKeyInfo.IsAutoIncrementKey
                        },
                PocoObjectPropertyName = prop.Name
            };
            
            data.Add(propData);
        }

        return data;
    }

    /// <summary>
    /// Retrieve all the DB Mapped Properties and Exclude all the Property with the ExcludeColumn Attribute
    /// </summary>
    /// <param name="type">Contains the type of the poco object that store all the information</param>
    /// <returns>An array with all the properties of the object</returns>
    public static PropertyInfo[] GetMappedProperties(Type type)
    {
        return type
            .GetProperties()
            .Where(prop => prop.GetCustomAttribute<IgnoreAttribute>() is null)
            .ToArray();
    }

    /// <summary>
    /// Retrieve all the DB Mapped Properties and Exclude all the Property with the ExcludeColumn Attribute
    /// </summary>
    /// <typeparam name="T">Contains the type of the poco object that store all the information</typeparam>
    /// <returns>An array with all the properties of the object</returns>
    public static PropertyInfo[] GetMappedProperties<T>()
        => GetMappedProperties(typeof(T));

    #endregion Object Mapper Methods

    #region Query Mapper Methods

    /// <summary>
    /// Generate an auto select clause by automatic mapping a poco object
    /// </summary>
    /// <typeparam name="T">Contains the poco object's type to be mapped</typeparam>
    /// <returns>An SQLBuilder Object</returns>
    public static SqlBuilder AutoSelectClause(Type type)
    {
        var mapper = new AutoMapper(type);
        var sb = new StringBuilder();

        return new SqlBuilder()
            .Select(mapper.ColumnsData!.Where(c => !c.IgnoreMapping).Select(c => c.Name).ToArray())
            .From(mapper.TableData!.TableFullName);
    }

    /// <summary>
    /// Generate an auto select clause by automatic mapping a poco object
    /// </summary>
    /// <typeparam name="T">Contains the poco object's type to be mapped</typeparam>
    /// <returns>An SQLBuilder Object</returns>
    public static SqlBuilder AutoSelectClause<T>()
        => AutoSelectClause(typeof(T));

    /// <summary>
    /// Generate an auto if exists clayse by automatic mapping a poco object
    /// </summary>
    /// <param name="type">Contains the poco object's type to be mapped</param>
    /// <returns>An SQLBuilder Object</returns>
    public static SqlBuilder AutoExistClause(Type type, object? obj = null)
    {
        var mapper = new AutoMapper(type, obj);

        var searchedSql = new SqlBuilder()
            .Select(mapper.TableData!.TableFullName);

        mapper.ColumnsData!.ForEach(x =>
        {
            if (x.PrimaryKeyInfo == null) return;

            var pkName = x.Name;
            object? pkValue = null;

            mapper.ColumnsData.ForEach(x =>
            {
                if (x.Name == pkName) pkValue = x.Value;
            });

            searchedSql.Where($"{pkName} = @0", pkValue);
        });

        return new SqlBuilder(@$"
                IF EXISTS ({searchedSql.Query})
                BEGIN
                    SELECT 1
                END
                ELSE
                BEGIN
                    SELECT 0
                END
            ", searchedSql.Params);
    }

    /// <summary>
    /// Generate an auto if exists clayse by automatic mapping a poco object
    /// </summary>
    /// <typeparam name="T">Contains the poco object's type to be mapped</typeparam>
    /// <returns>An SQLBuilder Object</returns>
    public static SqlBuilder AutoExistClause<T>(object? obj = null)
        => AutoExistClause(typeof(T), obj);


    /// <summary>
    /// Generate an auto inset clause by automatic mapping a poco object
    /// </summary>
    /// <param name="type">Contains the poco object's type to be mapped</param>
    /// <returns>An SQLBuilder Object</returns>
    public static SqlBuilder AutoInsertClause(Type type, object? obj = null)
    {
        var mapper = new AutoMapper(type, obj);

        var tbColNames = new List<string>();
        var tbColValues = new List<object>();

        mapper.ColumnsData!
            .Where(x => x.IgnoreMapping == false).ToList()
            .ForEach(x =>
            {
                tbColNames.Add(x.Name);
                tbColValues.Add(x.Value);
            }
            );

        return new SqlBuilder()
            .InsertIntoTable(
                mapper.TableData!.TableFullName,
                tbColNames.ToArray(),
                tbColValues.ToArray()
        );
    }

    /// <summary>
    /// Generate an auto inset clause by automatic mapping a poco object
    /// </summary>
    /// <typeparam name="T">Contains the poco object's type to be mapped</typeparam>
    /// <returns>An SQLBuilder Object</returns>
    public static SqlBuilder AutoInsertClause<T>(object? obj = null)
        => AutoInsertClause(typeof(T), obj);

    /// <summary>
    /// Generate an auto update clause by automatic mapping a poco object
    /// </summary>
    /// <param name="type">Contains the poco object's type to be mapped</param>
    /// <returns>An SQLBuilder Object</returns>
    public static SqlBuilder AutoUpdateClause(Type type, object? obj = null)
    {
        var mapper = new AutoMapper(type, obj);

        var setValues = new Dictionary<string, object>();
        mapper.ColumnsData!
            .Where(x => x.IgnoreMapping == false && x.PrimaryKeyInfo == null)
            .ToList()
            .ForEach(x => { setValues.Add(x.Name, x.Value); }
            );

        var whereValues = new Dictionary<string, object>();
        mapper.ColumnsData!
            .Where(x => x.IgnoreMapping == false && x.PrimaryKeyInfo != null)
            .ToList()
            .ForEach(x => { whereValues.Add(x.Name, x.Value); }
            );

        return new SqlBuilder()
            .UpdateTable(
                mapper.TableData!.TableFullName,
                setValues, 
                whereValues
        );
    }

    /// <summary>
    /// Generate an auto update clause by automatic mapping a poco object
    /// </summary>
    /// <typeparam name="T">Contains the poco object's type to be mapped</typeparam>
    /// <returns>An SQLBuilder Object</returns>
    public static SqlBuilder AutoUpdateClause<T>(object? obj = null)
        => AutoUpdateClause(typeof(T), obj);

    /// <summary>
    /// Generate an auto delete clause by automatic mapping a poco object
    /// </summary>
    /// <param name="type">Contains the poco object's type to be mapped</param>
    /// <returns>An SQLBuilder Object</returns>
    public static SqlBuilder AutoDeleteClause(Type type, object? obj = null)
    {
        var mapper = new AutoMapper(type, obj);

        var whereValues = new Dictionary<string, object>();
        mapper.ColumnsData!
            .Where(x => x.IgnoreMapping == false && x.PrimaryKeyInfo != null)
            .ToList()
            .ForEach(x => { whereValues.Add(x.Name, x.Value); }
            );

        return new SqlBuilder()
            .DeleteTable(
                mapper.TableData!.TableFullName, 
                whereValues
        );
    }

    /// <summary>
    /// Generate an auto delete clause by automatic mapping a poco object
    /// </summary>
    /// <typeparam name="T">Contains the poco object's type to be mapped</typeparam>
    /// <returns>An SQLBuilder Object</returns>
    public static SqlBuilder AutoDeleteClause<T>(object? obj = null)
        => AutoDeleteClause(typeof(T), obj);

    #endregion Query Mapper Methods
}