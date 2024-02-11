// System Class
using System.Text;

namespace LothiumDB.Tools;

public sealed class SqlBuilder : IDisposable
{
    #region Class Property

    /// <summary>
    /// Contains the generated query
    /// </summary>
    public string Query { get; private set; }

    /// <summary>
    /// Contains the stored parameters
    /// </summary>
    public object[] Params { get; private set; }

    #endregion

    #region Class Constructor & Destructor Methods

    /// <summary>
    /// Builder for creating a new Sql Object
    /// </summary>
    public SqlBuilder() : this(string.Empty, Array.Empty<object>()) { }

    /// <summary>
    /// Builder for creating a new Sql Object
    /// </summary>
    /// <param name="query">Contains the custom query</param>
    /// <param name="args">Contains the custom query variable's values</param>
    public SqlBuilder(string query, params object[] args)
    {
        Query = query;
        Params = Array.Empty<object>();
        if (args.Any()) UpdateParameters(args);
    }

    /// <summary>
    /// Dispose the Sql Object instance previously created
    /// </summary>
    // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
    public void Dispose() => GC.SuppressFinalize(this);

    #endregion

    #region Core Methods

    /// <summary>
    /// Update all the arguments inside the query builder
    /// </summary>
    /// <param name="newArgs">Contains the set of new parameters to add</param>
    private void UpdateParameters(params object[] newArgs)
    {
        if (!newArgs.Any()) return;

        var unifiedArgsArray = new object[Params.Length + newArgs.Length];
        Array.Copy(Params, unifiedArgsArray, Params.Length);
        Array.Copy(newArgs, 0, unifiedArgsArray, Params.Length, newArgs.Length);

        Params = unifiedArgsArray;
    }

    /// <summary>
    /// Clear all the current saved parameters inside the query builder
    /// </summary>
    public void Clear()
    {
        Query = string.Empty;
        Params = Array.Empty<object>();
    }

    /// <summary>
    /// Generate a string with the query and all the stored parameters
    /// </summary>
    /// <returns>A Query Formatted With All The Parameters</returns>
    public string ToFormatQuery()
    {
        try
        {
            if (string.IsNullOrEmpty(Query)) return string.Empty;
            if (!Params.Any()) return $"{Query}\n\n/// No Params ///";

            // Format the parameters for the output result
            var formattedParameters = string.Empty;
            var parIndex = 0;
            Params?.ToList().ForEach(par =>
            {
                var parName = par.ToString();
                var parValue = Params[parIndex];
                formattedParameters += $"\n{parIndex}) {parName} = {parValue}";
                parIndex++;
            });

            // Return the final formatted result
            return $"{Query}\n\n/// Query Params ///\n{formattedParameters}";
        }
        catch (Exception ex)
        {
            return string.Empty;
        }
    }

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
        // Generate the query
        StringBuilder sb;
        if (!string.IsNullOrEmpty(Query))
        {
            sb = new StringBuilder(Query);
            sb.Append('\n');
        }
        else
        {
            sb = new StringBuilder();
        }
        sb.Append(value);
        Query = sb.ToString();

        // Update all the actual passed argument for the query
        UpdateParameters(args);

        // Return the current object instance
        return this;
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Append a Select Clause to the final Query
    /// </summary>
    /// <param name="columns">Contains all the table's columns</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder Select(params object[] columns)
    {
        Append(
            !columns.Any()
                ? "SELECT *"
                : $"SELECT {string.Join(", ", columns.Select(x => x.ToString()).ToArray())}"
            );
        return this;
    }

    /// <summary>
    /// Append a Select Clause to the final Query
    /// </summary>
    /// <param name="topElements">Contains the number of element to be selected</param>
    /// <param name="columns">Contains all the table's columns</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder Select(int topElements, params object[] columns)
    {
        Append(
            !columns.Any()
                ? $"SELECT TOP {topElements} *"
                : $"SELECT TOP {topElements} {string.Join(", ", columns.Select(x => x.ToString()).ToArray())}"
        );
        return this;
    }

    /// <summary>
    /// Append a From Clause to the final Query
    /// </summary>
    /// <param name="tables">Contains the names of all the tables</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder From(params object[] tables)
    {
        Append($"FROM {string.Join(", ", tables.Select(x => x.ToString()).ToArray())}");
        return this;
    }

    /// <summary>
    /// Append a From Clause to the final Query
    /// </summary>
    /// <param name="sql">Contains a query to nest inside the current query</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder From(SqlBuilder sql)
    {
        Append($"FROM (\n {sql.Query} \n)");
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
        var clause = Query.Contains("WHERE") ? "AND" : "WHERE";
        Append($"{clause} {condition}", args);
        return this;
    }

    /// <summary>
    /// Append a Group By Clause to the final Query
    /// </summary>
    /// <param name="args">Contains all the arguments to append</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder GroupBy(params object[] args)
    {
        Append($"GROUP BY {string.Join(", ", args.Select(x => x.ToString()).ToArray())}");
        return this;
    }

    /// <summary>
    /// Append an Order By Clause to the final Query
    /// </summary>
    /// <param name="args">Contains all the arguments to append</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder OrderBy(params object[] args)
    {
        Append($"ORDER BY {string.Join(", ", args.Select(x => x.ToString()).ToArray())}");
        return this;
    }

    #endregion

    #region Join Methods

    /// <summary>
    /// Append An On Join Clause to the final query
    /// </summary>
    /// <param name="condition">Contains the join main condition</param>
    /// <param name="args">Contains all the condition parameters</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder On(string condition, params object[] args)
    {
        Append($"ON {condition}", args);
        return this;
    }

    /// <summary>
    /// Append An And Clause to the final query
    /// </summary>
    /// <param name="condition">Contains the join main condition</param>
    /// <param name="args">Contains all the condition parameters</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder And(string condition, params object[] args)
    {
        Append($"AND {condition}", args);
        return this;
    }

    /// <summary>
    /// Append An Or Clause to the final query
    /// </summary>
    /// <param name="condition">Contains the join main condition</param>
    /// <param name="args">Contains all the condition parameters</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder Or(string condition, params object[] args)
    {
        Append($"OR {condition}", args);
        return this;
    }

    /// <summary>
    /// Append an Inner Join Clause To the final Query
    /// </summary>
    /// <param name="table">Contains the name of the table to join with</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder InnerJoin(string table)
    {
        Append($"INNER JOIN {table}");
        return this;
    }

    /// <summary>
    /// Append a Left Join Clause To the final Query
    /// </summary>
    /// <param name="table">Contains the name of the table to join with</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder LeftJoin(string table)
    {
        Append($"LEFT JOIN {table}");
        return this;
    }

    /// <summary>
    /// Append a Right Join Clause To the final Query
    /// </summary>
    /// <param name="table">Contains the name of the table to join with</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder RightJoin(string table)
    {
        Append($"RIGHT JOIN {table}");
        return this;
    }

    /// <summary>
    /// Append an Outer Join Clause To the final Query
    /// </summary>
    /// <param name="sql">Contains the outer join query</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder OuterJoin(SqlBuilder sql)
    {
        Append($"OUTER JOIN {sql.Query}", sql.Params);
        return this;
    }

    /// <summary>
    /// Append a Left Outer Join Clause To the final Query
    /// </summary>
    /// <param name="sql">Contains the left outer join query</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder LeftOuterJoin(SqlBuilder sql)
    {
        Append($"LEFT OUTER JOIN {sql.Query}", sql.Params);
        return this;
    }

    /// <summary>
    /// Append a Right Outer Join Clause To the final Query
    /// </summary>
    /// <param name="sql">Contains the right outer join query</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder RightOuterJoin(SqlBuilder sql)
    {
        Append($"RIGHT OUTER JOIN {sql.Query}", sql.Params);
        return this;
    }

    /// <summary>
    /// Append a Full Outer Join Clause To the final Query
    /// </summary>
    /// <param name="sql">Contains the right outer join query</param>
    /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
    public SqlBuilder FullOuterJoin(SqlBuilder sql)
    {
        Append($"FULL OUTER JOIN {sql.Query}", sql.Params);
        return this;
    }

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
        Append("DELETE").From(table);

        if (whereValues == null || !whereValues.Any()) return this;

        var parCount = 0;
        foreach (var item in whereValues)
        {
            Where($"{item.Key} = @Par{parCount}", item.Value);
            parCount++;
        }

        return this;
    }

    #endregion
}