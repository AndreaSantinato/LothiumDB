// System Class
using System.Text;
// Custom Class
using LothiumDB.Interfaces;

namespace LothiumDB.Extensions;

public sealed class SqlBuilder : ISqlBuilder, IDisposable
{
    #region Public Property

    /// <summary>
    /// Contains the Final Formatted Query
    /// </summary>
    public string SqlQuery { get; private set; }

    /// <summary>
    /// Contains all the Parameters to be replaced over the variables inside the Final Formatted Query
    /// </summary>
    public object[] SqlParams { get; private set; }

    #endregion

    #region Class Constructor & Destructor Methods

    /// <summary>
    /// Builder for creating a new Sql Object
    /// </summary>
    public SqlBuilder()
    {
        SqlQuery = string.Empty;
        SqlParams = Array.Empty<object>();
    }

    /// <summary>
    /// Builder for creating a new Sql Object
    /// </summary>
    /// <param name="query">Contains the custom query</param>
    /// <param name="args">Contains the custom query variable's values</param>
    public SqlBuilder(string query, params object[] args)
    {
        SqlQuery = query;
        SqlParams = Array.Empty<object>();

        if (args.Length > 0) UpdateParameters(args);
    }

    /// <summary>
    /// Dispose the Sql Object instance previously created
    /// </summary>
    // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
    public void Dispose() => GC.SuppressFinalize(this);

    #endregion

    #region Builder & Helper Methods

    /// <summary>
    /// Builder For The Sql Query
    /// </summary>
    /// <param name="value">Contains the value that needs to be appended to the final query result</param>
    /// <param name="args">Contains all the arguments that must be replaced on the final query result variables</param>
    private void BuildQuery(object value, params object[] args)
    {
        StringBuilder sb;
        if (!string.IsNullOrEmpty(SqlQuery))
        {
            sb = new StringBuilder(SqlQuery);
            sb.Append("\n");
        }
        else
        {
            sb = new StringBuilder();
        }

        sb.Append(value);

        SqlQuery = sb.ToString();
        UpdateParameters(args);
    }

    /// <summary>
    /// Update all the arguments inside the query builder
    /// </summary>
    /// <param name="newArgs">Contains the set of new parameters to add</param>
    private void UpdateParameters(params object[] newArgs)
    {
        if (!newArgs.Any()) return;

        var argsLenght = SqlParams.Length + newArgs.Length;
        var unifiedArgsArray = new object[argsLenght];

        Array.Copy(
            SqlParams, 
            unifiedArgsArray, 
            SqlParams.Length
        );
        Array.Copy(
            newArgs,
            0,
            unifiedArgsArray,
            SqlParams.Length,
            newArgs.Length
        );

        SqlParams = unifiedArgsArray;
    }

    /// <summary>
    /// Clear all the current saved parameters inside the query builder
    /// </summary>
    private void ClearParameters()
        => SqlParams = Array.Empty<object>();

    #endregion

    #region Append Methods

    /// <summary>
    /// Append Element to the final Query
    /// </summary>
    /// <param name="value">Contains all the arguments to append</param>
    /// <param name="args">Contains all the arguments to append</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder Append(string value, params object[] args)
    {
        BuildQuery(value, args);
        return this;
    }

    #endregion

    #region Query Methods

    /// <summary>
    ///     <para>
    ///         Append a Select Clause to the final Query
    ///     </para>
    /// </summary>
    /// <param name="tableName">Contains the name of the table</param>
    /// <param name="tableColumns">Contains all the table's columns</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder Select(string tableName, params object[] tableColumns)
    {
        if (string.IsNullOrEmpty(tableName)) return this;

        Append(!tableColumns.Any()
            ? "SELECT *"
            : $"SELECT {string.Join(", ", tableColumns.Select(x => x.ToString()).ToArray())}");
        Append("FROM {tableName}");
        
        return this;
    }
    
    /// <summary>
    ///     <para>
    ///         Append a Select Clause to the final Query
    ///     </para>
    /// </summary>
    /// <param name="topElements">Contains the number of element to be selected</param>
    /// <param name="tableName">Contains the name of the table</param>
    /// <param name="tableColumns">Contains all the table's columns</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder Select(int topElements, string tableName, params object[] tableColumns)
    {
        if (string.IsNullOrEmpty(tableName)) return this;
        
        Append(!tableColumns.Any()
            ? $"SELECT TOP {topElements} *"
            : $"SELECT TOP {topElements} {string.Join(", ", tableColumns.Select(x => x.ToString()).ToArray())}");
        Append("FROM {tableName}");
        
        return this;
    }

    /// <summary>
    /// Append a Where Clause to the final Query
    /// </summary>
    /// <param name="condition">Contains the sql where clause with/without variables</param>
    /// <param name="args">Contains all the variable's value to add</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder Where(string condition, params object[] args)
    {
        var clause = SqlQuery.Contains("WHERE") 
            ? "AND" 
            : "WHERE";
        Append($"{clause} {condition}", args);
        return this;
    }

    /// <summary>
    /// Append a Group By Clause to the final Query
    /// </summary>
    /// <param name="args">Contains all the arguments to append</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder GroupBy(params object[] args)
        => Append($"GROUP BY {string.Join(", ", args.Select(x => x.ToString()).ToArray())}");

    /// <summary>
    /// Append an Order By Clause to the final Query
    /// </summary>
    /// <param name="args">Contains all the arguments to append</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder OrderBy(params object[] args)
        => Append($"ORDER BY {string.Join(", ", args.Select(x => x.ToString()).ToArray())}");

    #endregion

    #region Join Methods

    /// <summary>
    /// Append an Inner Join Clause To the final Query
    /// </summary>
    /// <param name="table">Contains the name of the table to join with</param>
    /// <param name="conditions">Contains the join conditions</param>
    /// <param name="args">Contains the join conditions variables values</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder InnerJoin(string table, string conditions, params object[] args)
        => Append($"INNER JOIN {table} {conditions}", args);

    /// <summary>
    /// Append a Left Join Clause To the final Query
    /// </summary>
    /// <param name="table">Contains the name of the table to join with</param>
    /// <param name="conditions">Contains the join conditions</param>
    /// <param name="args">Contains the join conditions variables values</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder LeftJoin(string table, string conditions, params object[] args)
        => Append($"LEFT JOIN {table} {conditions}", args);

    /// <summary>
    /// Append a Right Join Clause To the final Query
    /// </summary>
    /// <param name="table">Contains the name of the table to join with</param>
    /// <param name="conditions">Contains the join conditions</param>
    /// <param name="args">Contains the join conditions variables values</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder RightJoin(string table, string conditions, params object[] args)
        => Append($"RIGHT JOIN {table} {conditions}", args);

    /// <summary>
    /// Append an Outer Join Clause To the final Query
    /// </summary>
    /// <param name="sql">Contains the outer join query</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder OuterJoin(SqlBuilder sql)
        => Append($"OUTER JOIN {sql.SqlQuery}", sql.SqlParams);

    /// <summary>
    /// Append a Left Outer Join Clause To the final Query
    /// </summary>
    /// <param name="sql">Contains the left outer join query</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder LeftOuterJoin(SqlBuilder sql)
        => Append($"LEFT OUTER JOIN {sql.SqlQuery}", sql.SqlParams);

    /// <summary>
    /// Append a Right Outer Join Clause To the final Query
    /// </summary>
    /// <param name="sql">Contains the right outer join query</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder RightOuterJoin(SqlBuilder sql)
        => Append($"RIGHT OUTER JOIN {sql.SqlQuery}", sql.SqlParams);

    /// <summary>
    /// Append a Full Outer Join Clause To the final Query
    /// </summary>
    /// <param name="sql">Contains the right outer join query</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder FullOuterJoin(SqlBuilder sql)
        => Append($"FULL OUTER JOIN {sql.SqlQuery}", sql.SqlParams);

    #endregion

    #region Insert && Update && Delete Methods

    /// <summary>
    /// Append an Insert To Table Clause to the final query
    /// </summary>
    /// <param name="table">Contains the table's name</param>
    /// <param name="columns">Contains all the columns</param>
    /// <param name="values">Contains all the columns values</param>
    /// <returns></returns>
    public SqlBuilder InsertIntoTable(string table, object[] columns, object[] values)
    {
        var insertValues = new List<string>();
        var insertParams = new List<string>();
        var parCount = 0;

        values.ToList().ForEach(x =>
        {
            insertValues.Add(x.ToString());
            insertParams.Add($"@Par{parCount}");
            parCount++;
        });

        var insertClause = string.Concat(
            $"INSERT INTO {table} ({string.Join(", ", (from x in columns select x.ToString()).ToArray())})",
            Environment.NewLine,
            $"VALUES ({string.Join(", ", (from x in insertParams select x.ToString()).ToArray())})"
        );

        Append(insertClause, insertValues.ToArray());

        return this;
    }

    /// <summary>
    /// Append an Update Clause to the final Query
    /// </summary>
    /// <param name="table">Contains the name of the table to modify</param>
    /// <param name="setValues">Contains a KeyValue Pair for the set columns to modify (Key = Column Name, Value = Column Value)</param>
    /// <param name="whereValues">Contains a KeyValue Pair for the where column to modify (Key = Column Name, Value = Column Value)</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder UpdateTable(string table, Dictionary<string, object> setValues,
        Dictionary<string, object> whereValues)
    {
        var values = new List<object>();
        var updateClause = string.Empty;
        var parCount = 0;

        foreach (var item in setValues)
        {
            var condition = updateClause.Contains("SET") ? "," : "SET";
            values.Add(item.Value);
            updateClause = $"{condition} {item.Key} = @Par{parCount}";
            parCount++;
        }

        Append($"UPDATE {table} {Environment.NewLine} {updateClause}", values.ToArray());

        foreach (var item in whereValues)
        {
            Where($"{item.Key} = @Par{parCount}", item.Value);
            parCount++;
        }

        return this;
    }

    /// <summary>
    /// Append an Update Clause to the final Query
    /// </summary>
    /// <param name="table">Contains the name of the table to delete all the data or a specific ones</param>
    /// <param name="whereValues">Contains all the where values for delete one or more specific records</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder DeleteTable(string table, Dictionary<string, object>? whereValues = null)
    {
        Append($"DELETE FROM {table}");

        if (whereValues != null && whereValues.Any())
        {
            int parCount = 0;
            foreach (var item in whereValues)
            {
                Where($"{item.Key} = @Par{parCount}", item.Value);
                parCount++;
            }
        }

        return this;
    }

    #endregion
}