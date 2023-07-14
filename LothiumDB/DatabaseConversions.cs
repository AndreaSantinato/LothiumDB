// System Class
using System;
using System.Data;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LothiumDB
{
    /// <summary>
    /// Class Extension With All The Type Of Database Data's Conversions
    /// </summary>
    public static class DatabaseConversions
    {
        /// <summary>
        /// Convert a source of IEnumerable type into a new Dataset
        /// </summary>
        /// <typeparam name="TSource">Indicate the type of the source</typeparam>
        /// <param name="source">Contains the source with data to be converted into a dataset</param>
        /// <returns>A Dataset Formatted From An Enumerable Source</returns>
        public static DataSet ToDataSet<TSource>(this IList<TSource> source)
        {
            if (source == null) return null;
         
            DataSet? ds = new DataSet();
            DataTable? dt = new DataTable(typeof(TSource).Name);

            // Creating the headers for the table
            var tableHeaders = typeof(TSource).GetProperties().Select(p => p.Name);
            foreach (var header in tableHeaders)
            {
                dt.Columns.Add(header);
            }

            // Adding all the value to the table
            foreach (var elem in source.ToArray()) 
            {
                var values = new object[dt.Columns.Count];
                for (int i = 0;  i < dt.Columns.Count; i++)
                {
                    values[i] = typeof(TSource).GetProperty(dt.Columns[i].ToString()).GetValue(elem, null);
                }
                dt.Rows.Add(values);
            }

            // Adding the new table into the dataset and return it
            ds.Tables.Add(dt);
            return ds;
        }
    }
}
