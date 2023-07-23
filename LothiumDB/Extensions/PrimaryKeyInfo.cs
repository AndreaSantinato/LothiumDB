using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LothiumDB.Extensions
{
    internal class PrimaryKeyInfo
    {
        /// <summary>
        /// Contains the name of the primary key
        /// </summary>
        public string? PrimaryKeyName { get; set; }

        /// <summary>
        /// Indicates if the primary key have an auto increment identity
        /// </summary>
        public bool? IsAutoIncrementKey { get; set; }
    }
}
