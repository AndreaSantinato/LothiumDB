// System Class
using System;
using System.Data;
using System.Text;
using System.Runtime.InteropServices;
// Custom Class
using Microsoft.Win32.SafeHandles;
using LothiumDB.Interfaces;
using LothiumDB.Enumerations;
using LothiumDB.Helpers;

namespace LothiumDB
{
    public class SqlBuilder : ISqlBuilder, IDisposable
    {
        #region Private Property

        private string? _sql = string.Empty;
        private object[]? _args = new object[0];
        private int _nestedFromIndex = 0;
        private int _nestedWhereIndex = 0;
        private bool _disposedValue;
        private SafeHandle _safeHandle = new SafeFileHandle(IntPtr.Zero, true);

        #endregion

        #region Public Property

        /// <summary>
        /// Contains the Final Formatted Query
        /// </summary>
        public string Sql { get { return _sql; } }

        /// <summary>
        /// Contains all the Parameters to be replaced over the variables inside the Final Formatted Query
        /// </summary>
        public object[] Params { get { return _args; } }

        /// <summary>
        /// Contains the count of all the Nested Clause added to the Final Formatted Query
        /// </summary>
        public int NestedElements { get { return _nestedFromIndex + _nestedWhereIndex; } }

        #endregion

        #region Class Constructor & Destructor Methods

        /// <summary>
        /// Empty Constructor
        /// </summary>
        public SqlBuilder()
        {
            _sql = string.Empty;
            _args = new object[0];
            _nestedFromIndex = 0;
            _nestedWhereIndex = 0;
        }

        /// <summary>
        /// Contructor with parameters
        /// </summary>
        /// <param name="query"></param>
        /// <param name="args"></param>
        public SqlBuilder(string query, params object[] args)
        {
            _sql = query;
            if (_args == null) _args = new object[0];
            _args = DatabaseUtility.AddNewArgsToSqlParamsArray(_args, args);
            _nestedFromIndex = 0;
            _nestedWhereIndex = 0;
        }

        /// <summary>
        /// Protected Method Of The Dispose Pattern
        /// </summary>
        /// <param name="disposing">Indicate if the obgect's dispose is required</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing) _safeHandle.Dispose();
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Dispose the SqlBuilder Istance Previusly Created
        /// </summary>
        public void Dispose() => Dispose(true);

        #endregion

        #region Builder & Helper Methods

        /// <summary>
        /// Builder For The Sql Query
        /// </summary>
        /// <param name="value">Contains the value that needs to be appended to the final query result</param>
        /// <param name="args">Contains all the arguments that must be replaced on the final query result variables</param>
        private protected void BuildQuery(object value, params object[] args)
        {
            StringBuilder sb;
            if (!string.IsNullOrEmpty(Sql))
            {
                sb = new StringBuilder(Sql);
                sb.Append("\n");
            }
            else
            {
                sb = new StringBuilder();
            }
            sb.Append(value);

            _sql = sb.ToString();
            _args = DatabaseUtility.AddNewArgsToSqlParamsArray(Params, args);
        }

        /// <summary>
        /// Update all the arguments inside the query builder
        /// </summary>
        /// <param name="args">Contains the set of parameters that needs to be update</param>
        public void UpdateParameters(params object[] args) => _args = DatabaseUtility.AddNewArgsToSqlParamsArray(Params, args);

        /// <summary>
        /// Clear all the current saved parameters inside the query builder
        /// </summary>
        public void ClearParameters() => _args = null;

        #endregion

        #region Append Methods

        //// <summary>
        /// Append Element to the final Query
        /// </summary>
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
        /// Append a Select Clause to the final Query
        /// </summary>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder Select(params object[] args) => Append($"SELECT {string.Join(", ", (from x in args select x.ToString()).ToArray())}");

        /// <summary>
        /// Append a From Clause to the final Query
        /// </summary>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder From(params object[] args) => Append($"FROM {string.Join(", ", (from x in args select x.ToString()).ToArray())}");

        /// <summary>
        /// Append a Nested SQL Query to the final Query
        /// </summary>
        /// <param name="sql">Contains the Nested Query</param>
        /// <returns></returns>
        public SqlBuilder From(SqlBuilder sql)
        {
            _nestedFromIndex++;
            ClearParameters();
            UpdateParameters(DatabaseUtility.AddNewArgsToSqlParamsArray(_args, sql.Params));
            return Append($"FROM ({sql.Sql}) N{_nestedFromIndex}");
        }

        #region Join Methods

        /// <summary>
        /// Append a Specific Join Clause to the final Query
        /// </summary>
        /// <param name="tableName">Contains the name of the table to join with</param>
        /// <param name="joinType">Contains the type of join to append</param>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        private SqlBuilder Join(SqlCommandType joinType, string tableName, string joinConditions, params object[] args)
        {
            SqlBuilder sql = new SqlBuilder();

            if (joinType == SqlCommandType.InnerJoin) sql.Append($"INNER JOIN {tableName}");
            if (joinType == SqlCommandType.LeftJoin) sql.Append($"LEFT JOIN {tableName}");
            if (joinType == SqlCommandType.RightJoin) sql.Append($"RIGHT JOIN {tableName}");
            if (joinType == SqlCommandType.OuterJoin) sql.Append($"OUTER JOIN {tableName}");
            if (joinType == SqlCommandType.LeftOuterJoin) sql.Append($"LEFT OUTER JOIN {tableName}");
            if (joinType == SqlCommandType.RightOuterJoin) sql.Append($"RIGHT OUTER JOIN {tableName}");

            if (!joinConditions.StartsWith("ON")) joinConditions = string.Concat("ON", joinConditions);
            sql.Append($"ON {joinConditions}", args);

            return sql;
        }

        /// <summary>
        /// Append an Inner Join Clause To the final Query
        /// </summary>
        /// <param name="tableName">Contains the name of the table to join with</param>
        /// <param name="joinConditions">Contains the clause to add in the join</param>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder InnerJoin(string tableName, string joinConditions, params object[] args) => Join(SqlCommandType.InnerJoin, tableName, joinConditions, args);

        /// <summary>
        /// Append a Left Join Clause To the final Query
        /// </summary>
        /// <param name="tableName">Contains the name of the table to join with</param>
        /// <param name="joinConditions">Contains the clause to add in the join</param>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder LeftJoin(string tableName, string joinConditions, params object[] args) => Join(SqlCommandType.LeftJoin, tableName, joinConditions, args);

        /// <summary>
        /// Append a Right Join Clause To the final Query
        /// </summary>
        /// <param name="tableName">Contains the name of the table to join with</param>
        /// <param name="joinConditions">Contains the clause to add in the join</param>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder RightJoin(string tableName, string joinConditions, params object[] args) => Join(SqlCommandType.RightJoin, tableName, joinConditions, args);

        /// <summary>
        /// Append an Outer Join Clause To the final Query
        /// </summary>
        /// <param name="tableName">Contains the name of the table to join with</param>
        /// <param name="joinClause">Contains the clause to add in the join</param>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder OuterJoin(string tableName, string joinConditions, params object[] args) => Join(SqlCommandType.OuterJoin, tableName, joinConditions, args);

        /// <summary>
        /// Append a Left Outer Join Clause To the final Query
        /// </summary>
        /// <param name="tableName">Contains the name of the table to join with</param>
        /// <param name="joinConditions">Contains the clause to add in the join</param>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder LeftOuterJoin(string tableName, string joinConditions, params object[] args) => Join(SqlCommandType.LeftOuterJoin, tableName, joinConditions, args);

        /// <summary>
        /// Append a Right Outer Join Clause To the final Query
        /// </summary>
        /// <param name="tableName">Contains the name of the table to join with</param>
        /// <param name="joinConditions">Contains the clause to add in the join</param>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder RightOuterJoin(string tableName, string joinConditions, params object[] args) => Join(SqlCommandType.RightOuterJoin, tableName, joinConditions, args);

        /// <summary>
        /// Append a Full Outer Join Clause To the final Query
        /// </summary>
        /// <param name="tableName">Contains the name of the table to join with</param>
        /// <param name="joinConditions">Contains the clause to add in the join</param>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder FullOuterJoin(string tableName, string joinConditions, params object[] args) => Join(SqlCommandType.FullOuterJoin, tableName, joinConditions, args);

        #endregion

        /// <summary>
        /// Append a Where Clause to the final Query
        /// </summary>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder Where(string value, params object[] args)
        {
            if (Sql.Contains("WHERE"))
                value = $"AND {value}";
            else
                value = $"WHERE {value}";

            return Append(value, args);
        }

        /// <summary>
        /// Append a Nested SQL Where Clause to the final Query
        /// </summary>
        /// <param name="sql">Contains the Nested Where Clause</param>
        /// <returns></returns>
        public SqlBuilder Where(SqlBuilder sql)
        {
            _nestedWhereIndex++;
            return Append($"({sql.Sql})");
        }

        /// <summary>
        /// Append a Group By Clause to the final Query
        /// </summary>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder GroupBy(params object[] args) => Append($"GROUP BY {string.Join(", ", (from x in args select x.ToString()).ToArray())}");

        /// <summary>
        /// Append an Order By Clause to the final Query
        /// </summary>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder OrderBy(params object[] args) => Append($"ORDER BY {string.Join(", ", (from x in args select x.ToString()).ToArray())}");

        #endregion

        #region Insert Methods

        /// <summary>
        /// Append an Insert Into Clause to the final Query
        /// </summary>
        /// <param name="table">Contains the name of the table to insert the data</param>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder Insert(string table, params object[] args) => Append($"INSERT INTO {table} ({string.Join(", ", (from x in args select x.ToString()).ToArray())})");

        /// <summary>
        /// Append a Values Clause to a previus Insert Into Clause to the final Query
        /// </summary>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder Values(params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].ToString().Contains("@") && args[i].GetType() == typeof(string)) args[i] = $"'{args[i]}'";
            }

            if (Sql.Contains("VALUES"))
                return Append($", VALUES ({string.Join(", ", (from x in args select x.ToString()).ToArray())})");
            else
                return Append($"VALUES ({string.Join(", ", (from x in args select x.ToString()).ToArray())})");
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Append an Update Clause to the final Query
        /// </summary>
        /// <param name="table">Contains the name of the table to update the data</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder Update(string table) => Append($"UPDATE {table}");

        /// <summary>
        /// Append an Update Clause to the final Query
        /// </summary>
        /// <param name="table">Contains the name of the table to update the data</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder Update(string table, List<string> columns, params object[] values)
        {
            SqlBuilder tmpSql = new SqlBuilder();

            tmpSql.Append($"UPDATE {table}");

            if (columns.Count > 0 && values != null && values.Length == columns.Count)
                for (int i = 0; i < columns.Count; i++)
                    tmpSql.Set(columns[i], values[i]);

            return Append(tmpSql.Sql);
        }

        /// <summary>
        /// Append an Update Clause to the final Query
        /// </summary>
        /// <param name="column">Contains the name of the column to modify</param>
        /// <param name="value">Contains the value of the column to modify</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder Set(string column, object value)
        {
            if (value.GetType() == typeof(string)) value = $"'{value}'";

            if (Sql.Contains("SET"))
                return Append($", {column} = {value}");
            else
                return Append($"SET {column} = {value}");
        }

        /// <summary>
        /// Append an Update Clause to the final Query
        /// </summary>
        /// <param name="column">Contains the name of the column to modify</param>
        /// <param name="value">Contains the value of the column to modify</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder Set(List<string> columns, params object[] values)
        {
            SqlBuilder sql = new SqlBuilder();
            for (int i = 0; i < columns.Count(); i++) sql = Set(columns[i], values[i]);
            return sql;
        }

        /// <summary>
        /// Append an Update Clause to the final Query
        /// This could ne used the updated query need the Params variables to be used
        /// </summary>
        /// <param name="setClause">Contains the updaye clause to apped to the final query</param>
        /// <returns></returns>
        public SqlBuilder Set(string query)
        {
            SqlBuilder sql = new SqlBuilder();
            if (Sql.Contains("SET"))
                return Append($", {query}");
            else
                return Append($"SET {query}");
        }

        #endregion

        #region Delete Methods

        /// <summary>
        /// Append an Update Clause to the final Query
        /// </summary>
        /// <param name="table">Contains the name of the table to delete all the data or a specific ones</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        public SqlBuilder Delete(string table) => Append($"DELETE FROM {table}");

        #endregion
    }
}
