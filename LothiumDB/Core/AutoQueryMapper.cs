using LothiumDB.Core.PocoDataInfo;

namespace LothiumDB.Core;

/// <summary>
///     <para>
///         Store all the static methods dedicated to automatic generate an sql clause from the C.R.U.D operations
///     </para>
/// </summary>
/// <remarks>
///     <para>
///         - AutoSelectClause => Select * From [Table]
///         - AutoExistClause => If Exists (Select * From [Table])
///         - AutoInsertClause => Insert Into [Table]
///         - AutoUpdateClause => Update [Table]
///         - AutoDeleteClause => Delete From [Table]
///     </para>
/// </remarks>
internal static class AutoQueryMapper
{
    /// <summary>
    /// Generate an auto select clause by automatic mapping a poco object
    /// </summary>
    /// <typeparam name="T">Contains the poco object's type to be mapped</typeparam>
    /// <returns>An SQLBuilder Object</returns>
    public static SqlBuilder AutoSelectClause<T>()
    {
        var sql = new SqlBuilder();
        var poco = new PocoObject<T>();

        if (poco.TableDataInfo is null) return sql;
        if (poco.ColumnDataInfo is null) return sql;

        return sql
            .Select(
                poco.TableDataInfo.TableFullName,
                string.Join(",", poco.ColumnDataInfo.Select(c => c.Name))
            );
    }

    /// <summary>
    /// Generate an auto if exists clayse by automatic mapping a poco object
    /// </summary>
    /// <typeparam name="T">Contains the poco object's type to be mapped</typeparam>
    /// <param name="poco">Contains the poco object to be maped</param>
    /// <returns></returns>
    public static SqlBuilder AutoExistClause<T>(PocoObject<T> poco)
    {
        var sql = new SqlBuilder();

        if (poco.TableDataInfo == null) return sql;
        if (poco.ColumnDataInfo == null) return sql;

        var searchedSql = new SqlBuilder()
            .Select(poco.TableDataInfo.TableFullName);

        poco.ColumnDataInfo.ForEach(x =>
        {
            if (x.PrimaryKeyInfo == null) return;

            var pkName = x.Name;
            object? pkValue = null;

            poco.ColumnDataInfo.ForEach(x =>
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
    /// Generate an auto inset clause by automatic mapping a poco object
    /// </summary>
    /// <typeparam name="T">Contains the poco object's type to be mapped</typeparam>
    /// <returns>An SQLBuilder Object</returns>
    public static SqlBuilder AutoInsertClause<T>(PocoObject<T> poco)
    {
        var sql = new SqlBuilder();

        if (poco.TableDataInfo == null) return sql;
        if (poco.ColumnDataInfo == null) return sql;

        var tbColNames = new List<string>();
        var tbColValues = new List<object>();

        poco.ColumnDataInfo
            .Where(x => x.IgnoreMapping == false).ToList()
            .ForEach(x =>
                {
                    tbColNames.Add(x.Name);
                    tbColValues.Add(x.Value);
                }
            );

        return new SqlBuilder()
            .InsertIntoTable(poco.TableDataInfo.TableFullName, tbColNames.ToArray(), tbColValues.ToArray());
    }

    /// <summary>
    /// Generate an auto update clause by automatic mapping a poco object
    /// </summary>
    /// <typeparam name="T">Contains the poco object's type to be mapped</typeparam>
    /// <returns>An SQLBuilder Object</returns>
    public static SqlBuilder AutoUpdateClause<T>(PocoObject<T> poco)
    {
        var sql = new SqlBuilder();

        if (poco.TableDataInfo == null) return sql;
        if (poco.ColumnDataInfo == null) return sql;

        var setValues = new Dictionary<string, object>();
        poco.ColumnDataInfo
            .Where(x => x.IgnoreMapping == false && x.PrimaryKeyInfo == null)
            .ToList()
            .ForEach(x => { setValues.Add(x.Name, x.Value); }
            );

        var whereValues = new Dictionary<string, object>();
        poco.ColumnDataInfo
            .Where(x => x.IgnoreMapping == false && x.PrimaryKeyInfo != null)
            .ToList()
            .ForEach(x => { whereValues.Add(x.Name, x.Value); }
            );

        return sql
            .UpdateTable(poco.TableDataInfo.TableFullName, setValues, whereValues);
    }

    /// <summary>
    /// Generate an auto delete clause by automatic mapping a poco object
    /// </summary>
    /// <typeparam name="T">Contains the poco object's type to be mapped</typeparam>
    /// <returns>An SQLBuilder Object</returns>
    public static SqlBuilder AutoDeleteClause<T>(PocoObject<T> poco)
    {
        var sql = new SqlBuilder();

        if (poco.TableDataInfo == null) return sql;
        if (poco.ColumnDataInfo == null) return sql;

        var whereValues = new Dictionary<string, object>();
        poco.ColumnDataInfo
            .Where(x => x.IgnoreMapping == false && x.PrimaryKeyInfo != null)
            .ToList()
            .ForEach(x => { whereValues.Add(x.Name, x.Value); }
            );

        return sql
            .DeleteTable(poco.TableDataInfo.TableFullName, whereValues);
    }
}