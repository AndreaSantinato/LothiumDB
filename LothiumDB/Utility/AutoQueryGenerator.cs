// System Class
using System;
using System.Reflection;
// Custom Class
using LothiumDB;
using LothiumDB.Extensions;
using LothiumDB.Interfaces;

namespace LothiumDB.Helpers
{
    /// <summary>
    /// This class is used to generate automaticly queries for different operations
    /// </summary>
	public static class AutoQueryGenerator
	{
        /// <summary>
        /// Create an Auto Select * From [Table]
        /// This methods needs an SQL with only Where, Join, GroupBy and OrderBy Conditions and a LothiumDataInfo object
        /// </summary>
        /// <param name="sqlConditions">Contains a partials query with only conditions</param>
        /// <param name="objType">Contains the type of the choosen object to create the auto select query</param>
        /// <returns></returns>
        public static SqlBuilder GenerateAutoSelectClauseFromPocoObject(SqlBuilder sqlConditions, Type objType)
        {
            PropertyInfo[] pInfo = LothiumDataInfo.GetProperties(objType);
            List<String> cols = new List<String>();
            LothiumObject lothiumObj = new LothiumObject(objType);
            SqlBuilder sql = new SqlBuilder();            

            if (!sqlConditions.Sql.Contains("SELECT") && !sqlConditions.Sql.Contains("FROM"))
            {
                foreach (PropertyInfo pi in pInfo)
                {
                    cols.Add(lothiumObj.columnInfo[pi.Name].ColumnName);
                }
                sql.Select(cols.ToArray()).From(lothiumObj.tableInfo.TableName);
            }
            sql.Append(sqlConditions.Sql);
            sql.OrderBy(string.Join(", ", (from x in lothiumObj.tableInfo.PrimaryKeys select x.PrimaryKeyName).ToArray()));

            sql.ClearParameters();
            sql.UpdateParameters(sqlConditions.Params);
            
            return sql;
        }

        /// <summary>
        /// Create an Auto Select * From [Table]
        /// This methods needs an SQL with only Where, Join, GroupBy and OrderBy Conditions and a LothiumDataInfo object
        /// </summary>
        /// <param name="sqlConditions">Contains a partials query with only conditions</param>
        /// <param name="dataInfo">Contains a LothiumDataInfo object with Attributes values</param>
        /// <param name="topElement">Contains the number of element to select</param>
        /// <param name = "args" > Contains all the query parameter's values</param>
        /// <returns></returns>
        public static SqlBuilder GenerateAutoSelectClauseFromPocoObject(SqlBuilder sqlConditions, Type objType, int topElement)
        {
            PropertyInfo[] pInfo = LothiumDataInfo.GetProperties(objType);
            List<String> cols = new List<String>();
            LothiumObject lothiumObj = new LothiumObject(objType);
            SqlBuilder sql = new SqlBuilder();

            if (!sqlConditions.Sql.Contains("SELECT") && !sqlConditions.Sql.Contains("FROM"))
            {
                foreach (PropertyInfo pi in pInfo)
                {
                    cols.Add(lothiumObj.columnInfo[pi.Name].ColumnName);
                }
                sql.SelectTop(topElement, cols.ToArray()).From(lothiumObj.tableInfo.TableName);
            }
            sql.Append(sqlConditions.Sql);
            sql.OrderBy(string.Join(", ", (from x in lothiumObj.tableInfo.PrimaryKeys select x.PrimaryKeyName).ToArray()));

            sql.ClearParameters();
            sql.UpdateParameters(sqlConditions.Params);
            
            return sql;
        }

        /// <summary>
        /// Create an Auto Insert Into [Table]
        /// This methods needs a specific database provider, an object that contains all the property to insert into the table
        /// and optional the table name
        /// </summary>
        /// <param name="dbProvider">Contains the database provider that will be utilized</param>
        /// <param name="obj">Contains the object that need to be inserted into the database</param>
        /// <param name="tableName">Contains the table name to use for insert the object property values</param>
        /// <returns></returns>
        public static SqlBuilder GenerateInsertClauseFromPocoObject(IDbProvider dbProvider, object obj, string tableName = "")
        {
            List<object> tableColumns = new List<object>();
            List<object> tableValues = new List<object>();
            List<object> tableParams = new List<object>();
            SqlBuilder sql = new SqlBuilder();
            PropertyInfo[] pInfo = LothiumDataInfo.GetProperties(obj.GetType());
            LothiumObject lothiumObj = new LothiumObject(obj);

            int index = 0;
            foreach (var p in pInfo)
            {
                tableColumns.Add(lothiumObj.columnInfo[p.Name].ColumnName);
                tableParams.Add($"{dbProvider.VariablePrefix()}Par{index}");
                tableValues.Add(lothiumObj.columnInfo[p.Name].ColumnValue);
                index++;
            }

            sql.Insert(lothiumObj.tableInfo.TableName, (object[])tableColumns.ToArray()).Values((object[])tableParams.ToArray());
            sql.ClearParameters();
            sql.UpdateParameters((object[])tableValues.ToArray());

            return sql;
        }

        /// <summary>
        /// Create an Auto Update [Table]
        /// This methods needs a specific database provider, an object that contains all the property to insert into the table
        /// and optional the table name
        /// </summary>
        /// <param name="dbProvider">Contains the database provider that will be utilized</param>
        /// <param name="obj">Contains the object that need to be updated into the database</param>
        /// <param name="tableName">Contains the table name to use for update the object property values</param>
        /// <returns></returns>
        public static SqlBuilder GenerateUpdateClauseFromPocoObject(IDbProvider dbProvider, object obj, string tableName = "")
        {
            Dictionary<string, object> SetValues = new Dictionary<string, object>();
            Dictionary<string, object> WhereValues = new Dictionary<string, object>();
            Dictionary<string, object> ParamsValues = new Dictionary<string, object>();     
            List<object> tblValues = new List<object>();
            PropertyInfo[] pInfo = LothiumDataInfo.GetProperties(obj.GetType());
            LothiumObject lothiumObj = new LothiumObject(obj);
            SqlBuilder sql = new SqlBuilder();

            if (String.IsNullOrEmpty(tableName)) tableName = lothiumObj.tableInfo.TableName;

            int index = 0;
            foreach (var p in pInfo)
            {
                var colName = lothiumObj.columnInfo[p.Name].ColumnName;
                var colValue = lothiumObj.columnInfo[p.Name].ColumnValue;
                var pk = lothiumObj.tableInfo.PrimaryKeys.Where(x => x.PrimaryKeyName == colName).Select(x => x.PrimaryKeyName).FirstOrDefault();
                if (pk == null)
                {
                    SetValues.Add(colName, colValue);
                }
                else
                {
                    WhereValues.Add(colName, colValue);
                }
                ParamsValues.Add(colName, $"{dbProvider.VariablePrefix()}Par{index}");
                tblValues.Add(colValue);
                index++;
            }

            sql.Update(tableName);
            foreach (var elem in SetValues) sql.Set($"{elem.Key} = {ParamsValues[elem.Key]}");
            foreach (var elem in WhereValues) sql.Where($"{elem.Key} = {ParamsValues[elem.Key]}");

            sql.ClearParameters();
            sql.UpdateParameters((object[])tblValues.ToArray());

            return sql;
        }

        /// <summary>
        /// Create an DELETE FROM [Table]
        /// This methods needs a specific database provider, an object that contains all the property to insert into the table
        /// and optional the table name
        /// </summary>
        /// <param name="dbProvider">Contains the database provider that will be utilized</param>
        /// <param name="obj">Contains the object that need to be deleted into the database</param>
        /// <param name="tableName">Contains the table name to use for delete the object property values</param>
        /// <returns></returns>
        public static SqlBuilder GenerateDeleteClauseFromPocoObject(IDbProvider dbProvider, object obj, string tableName = "")
        {
            Dictionary<string, object> WhereValues = new Dictionary<string, object>();
            Dictionary<string, object> ParamsValues = new Dictionary<string, object>();
            List<object> tblValues = new List<object>();
            PropertyInfo[] pInfo = LothiumDataInfo.GetProperties(obj.GetType());
            LothiumObject lothiumObj = new LothiumObject(obj);
            SqlBuilder sql = new SqlBuilder();

            if (String.IsNullOrEmpty(tableName)) tableName = lothiumObj.tableInfo.TableName;

            int index = 0;
            foreach (var p in pInfo)
            {
                var colName = lothiumObj.columnInfo[p.Name].ColumnName;
                var colValue = lothiumObj.columnInfo[p.Name].ColumnValue;
                var pk = lothiumObj.tableInfo.PrimaryKeys.Where(x => x.PrimaryKeyName == colName).Select(x => x.PrimaryKeyName).FirstOrDefault();
                if (pk != null)
                {
                    WhereValues.Add(colName, colValue);
                    ParamsValues.Add(colName, $"{dbProvider.VariablePrefix()}Par{index}");
                    tblValues.Add(colValue);
                }
                index++;
            }

            sql.Delete(tableName);
            foreach (var elem in WhereValues) sql.Where($"{elem.Key} = {ParamsValues[elem.Key]}");

            sql.ClearParameters();
            sql.UpdateParameters((object[])tblValues.ToArray());

            return sql;
        }
    }
}

