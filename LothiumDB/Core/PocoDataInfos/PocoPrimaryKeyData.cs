// System Class
using System;
using System.Linq;
// Custom Class
using LothiumDB.Attributes;

namespace LothiumDB.Core.PocoDataInfos
{
    /// <summary>
    /// Define the info of an object associated database's table primary keys
    /// </summary>
    internal class PocoPrimaryKeyData
    {
        #region Class Property

        /// <summary>
        /// Contains the type of the primary key
        /// </summary>
        public Type PrimaryKeyType { get; private set; }

        /// <summary>
        /// Indicates if the primary key have an auto increment identity
        /// </summary>
        public bool? IsAutoIncrementKey { get; private set; }

        #endregion

        #region Class Constructor

        /// <summary>
        /// Instance a new PrimaryKeyInfo object
        /// </summary>
        /// <param name="primaryKeyType">Contains the type of the primary key</param>
        /// <param name="isAutoIncrementKey">Indicates if the primary key is auto-increment</param>
        public PocoPrimaryKeyData(Type primaryKeyType, bool isAutoIncrementKey)
        {
            PrimaryKeyType = primaryKeyType;
            IsAutoIncrementKey = isAutoIncrementKey;
        }

        #endregion
    }
}
