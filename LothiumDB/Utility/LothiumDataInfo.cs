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

namespace LothiumDB.Helpers
{
    internal class PrimaryKeyInfo
    {

        /// <summary>
        /// Contains the name of the object model's associated database's primary key
        /// </summary>
        public string? PrimaryKey { get; set; }

        /// <summary>
        /// Indicates if the provided Primarykey is an AutoIncrement Identity
        /// </summary>
        public bool IsPrimaryKeyAutoIncremenet { get; set; }

        public PrimaryKeyInfo()
        {
            PrimaryKey = string.Empty;
            IsPrimaryKeyAutoIncremenet = false;
        }

        public PrimaryKeyInfo(string pKey, bool autoIncrement)
        {
            PrimaryKey = pKey;
            IsPrimaryKeyAutoIncremenet = autoIncrement;
        }
    }

    internal class LothiumDataInfo
    {
        /// <summary>
        /// Contains the name of the object model's associated database's table
        /// </summary>
        public string? TableName { get; set; }

        /// <summary>
        /// Contains all the names of the object model's associated database's Primary Keys
        /// </summary>
        public List<PrimaryKeyInfo> PrimaryKeys { get; set; }

        /// <summary>
        /// Contains all the names of the object model's associated database's columns
        /// </summary>
        public Dictionary<string, object> TableColumns { get; set; }

        /// <summary>
        /// Contains all the names of the object model's associated database's excluded columns
        /// </summary>
        public Dictionary<string, object> TableExcludedColumns { get; set; }

        /// <summary>
        /// Default Costructor
        /// </summary>
        /// <param name="classType"></param>
        public LothiumDataInfo(Type classType)
        {
            TableName = string.Empty;
            PrimaryKeys = new List<PrimaryKeyInfo>();
            TableColumns = new Dictionary<string, object>();
            TableExcludedColumns = new Dictionary<string, object>();

            try
            {
                // Get the Table's Name
                TableNameAttribute tbNameAttr1 = (TableNameAttribute)Attribute.GetCustomAttribute(classType, typeof(TableNameAttribute));
                if (tbNameAttr1 != null) TableName = tbNameAttr1.Table;

                // Get All the PrimaryKeys and for each one retrive the Name and if it's auto-increment
                PrimaryKeyAttribute[] tbPrimaryKeysAttr = (PrimaryKeyAttribute[])Attribute.GetCustomAttributes(classType, typeof(PrimaryKeyAttribute));
                if (tbPrimaryKeysAttr != null && tbPrimaryKeysAttr.Length > 0)
                {
                    foreach (PrimaryKeyAttribute pk in tbPrimaryKeysAttr)
                    {
                        PrimaryKeys.Add(new PrimaryKeyInfo(pk.PrimaryKey, pk.IsAutoIncremenetKey));
                    }
                }

                PropertyInfo[] pInfo = classType.GetTypeInfo().GetProperties();
                foreach (PropertyInfo pi in pInfo)
                {
                    // Get all the Table's Columns
                    ColumnNameAttribute colNameAttr = (ColumnNameAttribute)Attribute.GetCustomAttribute(pi, typeof(ColumnNameAttribute));
                    if (colNameAttr != null) TableColumns.Add(pi.Name, colNameAttr.Column);

                    // Get all the Excluded Table's Columns
                    ExcludeColumnAttribute exColNameAttr = (ExcludeColumnAttribute)Attribute.GetCustomAttribute(pi, typeof(ExcludeColumnAttribute));
                    if (exColNameAttr != null) TableExcludedColumns.Add(pi.Name, exColNameAttr.isExcluded);
                }
            }
            catch (Exception ex)
            {
                //
                // ToDo: Manage exception errors
                //
            }
        }

        /// <summary>
        /// Retrieve all the DB Mapped Properties and Exclude all the Property with the ExcludeColumn Attribute
        /// </summary>
        /// <param name="classType">Contains the object type</param>
        /// <returns></returns>
        public static PropertyInfo[] GetProperties(Type classType)
        {
            PropertyInfo[]? propertyInfo = null;

            try
            {
                propertyInfo = classType.GetProperties().Where(pi => pi.GetCustomAttributes(typeof(ExcludeColumnAttribute), true).Length == 0).ToArray();
            }
            catch (Exception ex)
            {
                propertyInfo = classType.GetProperties();
            }

            return propertyInfo;
        }
    }
}
