// System Class
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
// Custom Class
using LothiumDB.Enumerations;

namespace LothiumDB.Exceptions
{
    /// <summary>
    /// Dedicated Class For All The Classess Exception
    /// </summary>
    public class DatabaseException : Exception
    {
        public string? ErrorMSG { get; set; }
    }
}
