using LothiumDB.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LothiumDB.Extensions
{
    internal class LothiumObject
    {
        /// <summary>
        /// Contains all the Table's information for a Poco Object
        /// </summary>
        public TableInfo? tableInfo { get; set; }

        /// <summary>
        /// Contains all the Columns's information for a Poco Object
        /// </summary>
        public Dictionary<string, ColumnInfo>? columnInfo { get; set; }

        /// <summary>
        /// Standard Constructor
        /// </summary>
        /// <param name="obj">Contains the Poco Object</param>
        public LothiumObject(Type objType)
        {
            tableInfo = LothiumDataInfo.GetTableInfoFromPocoObject(objType);
            columnInfo = LothiumDataInfo.GetColumnsInfoFromPocoObject(objType);
        }

        /// <summary>
        /// Extended Constructor
        /// </summary>
        /// <param name="obj"></param>
        public LothiumObject(object obj)
        {
            tableInfo = LothiumDataInfo.GetTableInfoFromPocoObject(obj.GetType());
            columnInfo = LothiumDataInfo.GetColumnsInfoFromPocoObject(obj.GetType(), obj);
        }
    }
}
