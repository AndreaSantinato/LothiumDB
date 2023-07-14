// System Class
using System;
using System.Reflection;
// Custom Class
using LothiumDB;
using LothiumDB.Interfaces;

namespace LothiumDB.Helpers
{
    /// <summary>
    /// This class is used to generate automaticly queries for different operations
    /// </summary>
	public static class AutoQueryGenerator
	{
        /// <summary>
        /// Create an Auto Insert Into [Table]
        /// This methods needs a specific database provider, an object that contains all the property to insert into the table
        /// and optional the table name
        /// </summary>
        /// <param name="dbProvider">Contains the database provider that will be utilized</param>
        /// <param name="obj">Contains the object that need to be inserted into the database</param>
        /// <param name="tableName">Contains the table name to use for insert the object property values</param>
        /// <returns></returns>
        public static SqlBuilder GenerateInsertClause(IDbProvider dbProvider, object obj, string tableName = "")
        {
            SqlBuilder sql = new SqlBuilder();
            PropertyInfo[] pInfo = LothiumDataInfo.GetProperties(obj.GetType());
            List<object> tblColumns = new List<object>();
            List<object> tblValues = new List<object>();
            List<object> tblParams = new List<object>();

            LothiumDataInfo dtInfo = new LothiumDataInfo(obj.GetType());
            if (dtInfo == null || dtInfo.TableColumns.Count == 0)
            {
                // Without any information about the primary key, the library will try to generate an insert query command and execute it
                int index = 0;
                foreach (var p in pInfo)
                {
                    tblColumns.Add(p.ToString());
                    tblValues.Add(p.GetValue(obj));
                    tblParams.Add($"{dbProvider.VariablePrefix()}Par{index}");
                    index++;
                }

                if (!String.IsNullOrEmpty(tableName))
                    sql.Insert(tableName, (object[])tblColumns.ToArray());
                else
                    sql.Insert(obj.GetType().Name.ToString());
            }
            else
            {
                int index = 0;
                foreach (var p in pInfo)
                {
                    if (!dtInfo.PrimaryKeys.Where(x => x.PrimaryKey.ToString() == dtInfo.TableColumns[p.Name].ToString()).Select(x => x.IsPrimaryKeyAutoIncremenet).SingleOrDefault())
                    {
                        tblColumns.Add(dtInfo.TableColumns[p.Name]);
                        tblValues.Add(p.GetValue(obj));
                        tblParams.Add($"{dbProvider.VariablePrefix()}Par{index}");
                    }
                    index++;
                }

                sql.Insert(dtInfo.TableName, (object[])tblColumns.ToArray()).Values((object[])tblParams.ToArray());
            }

            sql.ClearParameters();
            sql.UpdateParameters((object[])tblValues.ToArray());

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
        public static SqlBuilder GenerateUpdateClause(IDbProvider dbProvider, object obj, string tableName = "")
        {
            SqlBuilder sql = new SqlBuilder();
            PropertyInfo[] pInfo = LothiumDataInfo.GetProperties(obj.GetType());

            LothiumDataInfo dtInfo = new LothiumDataInfo(obj.GetType());
            if (dtInfo == null || dtInfo.TableColumns.Count == 0)
            {
                // Without any information about the primary key, the library can't execute the update query command
                return null;
            }
            else
            {
                Dictionary<string, object> WhereValues = new Dictionary<string, object>();
                Dictionary<string, object> SetValues = new Dictionary<string, object>();
                List<object> tblValues = new List<object>();

                // Get all the primary keys and their values
                for (int x = 0; x < dtInfo.PrimaryKeys.Count(); x++)
                {
                    var pKey = dtInfo.PrimaryKeys[x];
                    for (int y = 0; y < pInfo.Count(); y++)
                    {
                        if (pKey.PrimaryKey.ToString() == dtInfo.TableColumns[pInfo[y].Name].ToString())
                        {
                            WhereValues.Add(pKey.PrimaryKey, pInfo[y].GetValue(obj));
                            break;
                        }
                    }
                }

                // Get All the Columns and their changed values
                int parIndex = WhereValues.Count() - 1;
                for (int i = 0; i < pInfo.Count(); i++)
                {
                    var p = pInfo[i];
                    if (!WhereValues.ContainsKey(dtInfo.TableColumns[p.Name].ToString()))
                    {
                        SetValues.Add(dtInfo.TableColumns[p.Name].ToString(), String.Concat(dbProvider.VariablePrefix().ToString(), "Par", parIndex));
                        tblValues.Add(p.GetValue(obj));
                        parIndex++;
                    }
                }

                if (String.IsNullOrEmpty(tableName)) tableName = dtInfo.TableName;
                sql.Update(tableName);

                foreach (var elem in SetValues) sql.Set($"{elem.Key} = {elem.Value}");

                sql.ClearParameters();
                sql.UpdateParameters((object[])tblValues.ToArray());

                foreach (var elem in WhereValues)
                {
                    sql.Where($"{elem.Key} = {String.Concat(dbProvider.VariablePrefix().ToString(), "Par", parIndex)}", elem.Value);
                    parIndex++;
                }
            }

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
        public static SqlBuilder GenerateDeleteClause(IDbProvider dbProvider, object obj, string tableName = "")
        {
            SqlBuilder sql = new SqlBuilder();
            List<object> objArgs = new List<object>();
            PropertyInfo[] pInfo = LothiumDataInfo.GetProperties(obj.GetType());

            LothiumDataInfo dtInfo = new LothiumDataInfo(obj.GetType());
            if (dtInfo == null || dtInfo.TableColumns.Count == 0)
            {
                // Without any information about the primary key, the library can't execute the delete query command
                return null;
            }
            else
            {
                if (!String.IsNullOrEmpty(tableName))
                    sql.Delete(tableName);
                else
                    sql.Delete(dtInfo.TableName);

                int index = 0;
                foreach (var pKey in dtInfo.PrimaryKeys)
                {
                    sql.Where($"[{pKey.PrimaryKey}] = {String.Concat(dbProvider.VariablePrefix().ToString(), "Par", index)}");
                    foreach (var p in pInfo)
                    {
                        if (pKey.PrimaryKey.ToString() == dtInfo.TableColumns[p.Name].ToString())
                        {
                            objArgs.Add(p.GetValue(obj));
                            break;
                        }
                    }
                }
            }

            sql.ClearParameters();
            sql.UpdateParameters((object[])objArgs.ToArray());

            return sql;
        }
    }
}

