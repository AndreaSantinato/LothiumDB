// System Class
using System;
using System.Linq;
// Custom Class
using LothiumDB.Attributes;

namespace LothiumDB.Core.PocoDataInfos
{
    /// <summary>
    /// Define the info of an object associated database's table
    /// </summary>
    internal class PocoTableData
    {
        #region Class Property

        /// <summary>
        /// Contains the name of the poco class
        /// </summary>
        public string? PocoObjectClassName { get; private set; }

        /// <summary>
        /// Contains the name of the table
        /// </summary>
        public string? TableName { get; private set; }

        /// <summary>
        /// Contains the schema of the table
        /// </summary>
        public string? TableSchema { get; private set; }

        /// <summary>
        /// Contains the combined value of table's schema and table's name
        /// </summary>
        public string TableFullName
        {
            get => $"{TableSchema}.{TableName}";
        }

        #endregion

        #region Class Constructor

        /// <summary>
        /// Instance a new TableInfo object
        /// </summary>
        /// <param name="className">Contains the class's name</param>
        /// <param name="tableName">Contains the table's name</param>
        public PocoTableData(string className, string tableName)
        {
            PocoObjectClassName = className;
            TableName = tableName;
            TableSchema = "dbo";
        }

        /// <summary>
        /// Instance a new TableInfo object
        /// </summary>
        /// <param name="className">Contains the class's name</param>
        /// <param name="tableName">Contains the table's name</param>
        /// <param name="tableSchema">Contains the table's schema</param>
        public PocoTableData(string className, string tableName, string tableSchema)
        {
            PocoObjectClassName = className;
            TableName = tableName;
            TableSchema = tableSchema;
        }

        #endregion
    }
}
