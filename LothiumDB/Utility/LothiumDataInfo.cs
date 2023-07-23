// System Class
using Microsoft.Data.SqlClient.DataClassification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
// Custom Class
using LothiumDB.Attributes;
using LothiumDB.Extensions;
using LothiumDB.Exceptions;
using System.Net.NetworkInformation;
using System.Collections.Immutable;

namespace LothiumDB.Helpers
{
    internal class LothiumDataInfo
    {
        /// <summary>
        /// Retrieve all the DB Mapped Properties and Exclude all the Property with the ExcludeColumn Attribute
        /// </summary>
        /// <param name="obj">Contains the object that store all the table informations</param>
        /// <returns>An array with all the properties of the object</returns>
        public static PropertyInfo[] GetProperties(Type objType)
        {
            PropertyInfo[]? propertyInfo = null;

            try
            {
                propertyInfo = objType.GetProperties().Where(pi => pi.GetCustomAttributes(typeof(ExcludeColumnAttribute), true).Length == 0).ToArray();
            }
            catch (Exception ex)
            {
                propertyInfo = objType.GetProperties();
            }

            return propertyInfo;
        }

        /// <summary>
        /// Retrive all the database table informations from an object
        /// </summary>
        /// <param name="obj">Contains the object that store all the table informations</param>
        /// <returns>A new table info object</returns>
        public static TableInfo GetTableInfoFromPocoObject(Type objType)
        {
            TableInfo tbInfo = new TableInfo();

            try
            {
                TableNameAttribute tbNameAttr1 = (TableNameAttribute)Attribute.GetCustomAttribute(objType, typeof(TableNameAttribute));
                if (tbNameAttr1 != null) tbInfo.TableName = tbNameAttr1.Table;
                tbInfo.PrimaryKeys = GetPrimaryKeysInfoFromPocoObject(objType);
            }
            catch (DatabaseException ex)
            {
                //
                // ToDo: Manage The Exception
                //
            }

            return tbInfo;
        }

        /// <summary>
        /// Retrive all the database table's primary keys informations from an object
        /// </summary>
        /// <param name="obj">Contains the object that store all the table informations</param>
        /// <returns>A new primary key list</returns>
        public static List<PrimaryKeyInfo> GetPrimaryKeysInfoFromPocoObject(Type objType)
        {
            List<PrimaryKeyInfo> pkInfos = new List<PrimaryKeyInfo>();

            try
            {
                PrimaryKeyAttribute[] tbPrimaryKeysAttr = (PrimaryKeyAttribute[])Attribute.GetCustomAttributes(objType, typeof(PrimaryKeyAttribute));
                if (tbPrimaryKeysAttr != null && tbPrimaryKeysAttr.Length > 0)
                {
                    foreach (PrimaryKeyAttribute pk in tbPrimaryKeysAttr)
                    {
                        PrimaryKeyInfo pkInfo = new PrimaryKeyInfo();
                        pkInfo.PrimaryKeyName = pk.PrimaryKey;
                        pkInfo.IsAutoIncrementKey = pk.IsAutoIncremenetKey;
                        pkInfos.Add(pkInfo);
                    }
                }
            }
            catch (DatabaseException ex)
            {
                //
                // ToDo: Manage The Exception
                //
            }       

            return pkInfos;
        }

        /// <summary>
        /// Retrive all the database table's columns informations from an object
        /// </summary>
        /// <param name="obj">Contains the object that store all the table informations</param>
        /// <returns>A new columInfo list</returns>
        public static Dictionary<string, ColumnInfo> GetColumnsInfoFromPocoObject(Type objType, object obj = null)
        {
            Dictionary<string, ColumnInfo> colInfos = new Dictionary<string, ColumnInfo>();

            try
            {
                PropertyInfo[] pInfo = objType.GetTypeInfo().GetProperties();
                foreach (PropertyInfo pi in pInfo)
                {
                    ColumnNameAttribute colNameAttr = (ColumnNameAttribute)Attribute.GetCustomAttribute(pi, typeof(ColumnNameAttribute));
                    ExcludeColumnAttribute exColNameAttr = (ExcludeColumnAttribute)Attribute.GetCustomAttribute(pi, typeof(ExcludeColumnAttribute));

                    if (colNameAttr != null)
                    {
                        ColumnInfo columnInfo = new ColumnInfo();
                        columnInfo.ColumnName = colNameAttr.Column;
                        columnInfo.ColumnType = String.Empty; // ToDo
                        if (obj != null) columnInfo.ColumnValue = pi.GetValue(obj);
                        columnInfo.HaveColumnExcludedAttributes = false;
                        if (exColNameAttr != null) columnInfo.HaveColumnExcludedAttributes = exColNameAttr.isExcluded;
                        colInfos.Add(pi.Name, columnInfo);
                    }
                }
            }
            catch (DatabaseException ex)
            {
                //
                // ToDo: Manage The Exception
                //
            }

            return colInfos;
        }
    }
}
