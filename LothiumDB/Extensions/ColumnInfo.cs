using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LothiumDB.Extensions
{
    internal class ColumnInfo
    {
        /// <summary>
        /// Contains the name of the column
        /// </summary>
        public string? ColumnName { get; set; }

        /// <summary>
        /// Contains the database's type of the column
        /// </summary>
        public string? ColumnType { get; set; }

        /// Contains the value of the column
        public object? ColumnValue { get; set; }

        /// <summary>
        /// Indicate if the column is not included in the database table
        /// </summary>
        public bool? HaveColumnExcludedAttributes { get; set; }
    }
}
