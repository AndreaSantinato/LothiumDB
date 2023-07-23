using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LothiumDB.Extensions
{
    internal class TableInfo
    {
        /// <summary>
        /// Contains the name of the table
        /// </summary>
        public string? TableName { get; set; }

        /// <summary>
        /// Contains the schema of the table
        /// </summary>
        public string? TableSchema { get; set; }

        /// <summary>
        /// Contains all the primary keys of the table
        /// </summary>
        public List<PrimaryKeyInfo>? PrimaryKeys { get; set; }
    }
}
