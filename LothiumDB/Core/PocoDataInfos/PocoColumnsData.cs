// System Class
using System;
using System.Linq;
using System.Reflection;
// Custom Class
using LothiumDB.Attributes;

namespace LothiumDB.Core.PocoDataInfos
{
    /// <summary>
    /// Define the info of an object associated database's table columns
    /// </summary>
    internal class PocoColumnsData
    {
        #region Class Property

        /// <summary>
        /// Indicates if the column is required for the object mapping
        /// </summary>
        public bool? IsColumnRequired { get; private set; }

        /// <summary>
        /// Indicates if the column is excluded for the object mapping
        /// </summary>
        public bool? IsColumnExcluded { get; private set; }

        /// <summary>
        /// Contains the info of the primary key if the colum have the dedicated attribute
        /// </summary>
        public PocoPrimaryKeyData? PrimaryKeyInfo { get; private set; }

        /// <summary>
        /// Contains the poco object associated property
        /// </summary>
        public string? PocoObjectProperty { get; private set; }

        /// <summary>
        /// Contains the name of the column
        /// </summary>
        public string? ColumnName { get; private set; }

        /// <summary>
        /// Contains the database's type of the column
        /// </summary>
        public string? ColumnType { get; private set; }

        /// <summary>
        /// Contains the value of the column
        /// </summary>
        public object? ColumnValue { get; private set; }

        #endregion

        #region Class Constructor

        /// <summary>
        /// Instance a new ColumnInfo object
        /// </summary>
        /// <param name="pocoObjPropertyName">Contains the poco object property name</param>
        /// <param name="columnRequired">Indicates if the poco object property is required for the mapping</param>
        /// <param name="columnExclued">Indicates if the poco object property is excluded for the mapping</param>
        /// <param name="columnName">Contains the name of the table's column</param>
        /// <param name="columType">Contains the type of the table's column</param>
        /// <param name="columnValue">Contains the value of the table's column</param>
        /// <param name="primaryKeyInfo">Contains all the info about the table's column primary key</param>
        public PocoColumnsData(
            string pocoObjPropertyName,
            bool? columnRequired,
            bool? columnExclued,
            string columnName,
            string columType,
            object columnValue,
            PocoPrimaryKeyData? primaryKeyInfo = null
        )
        {
            PocoObjectProperty = pocoObjPropertyName;
            IsColumnRequired = columnRequired;
            IsColumnExcluded = columnExclued == null ? false : columnExclued;
            ColumnName = columnName;
            ColumnType = columType;
            ColumnValue = columnValue;
            PrimaryKeyInfo = primaryKeyInfo;
        }

        #endregion
    }
}
