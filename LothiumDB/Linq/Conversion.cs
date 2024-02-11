// System Class
using System.Data;
using LothiumDB.Tools;

namespace LothiumDB.Linq;

/// <summary>
/// Class Extension With All The Type Of Database Data's Conversions
/// </summary>
public static partial class Conversion
{
    /// <summary>
    /// Convert a source of IEnumerable type into a new Dataset
    /// </summary>
    /// <typeparam name="TSource">Indicate the type of the source</typeparam>
    /// <param name="source">Contains the source with data to be converted into a dataset</param>
    /// <returns>A New Dataset Formatted From An Enumerable Source</returns>
    public static DataSet ToDataSet<TSource>(this IEnumerable<TSource> source)
    {
        var ds = new DataSet();
        var dt = new DataTable(typeof(TSource).Name);

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
            for (var i = 0; i < dt.Columns.Count; i++)
            {
                values[i] = typeof(TSource).GetProperty(dt.Columns[i].ToString()).GetValue(elem, null);
            }

            dt.Rows.Add(values);
        }

        // Adding the new table into the dataset and return it
        ds.Tables.Add(dt);
        return ds;
    }

    /// <summary>
    /// Convert a source of IEnumerable type into a new PageObject
    /// </summary>
    /// <typeparam name="TSource">Indicate the type of the source</typeparam>
    /// <param name="source">Contains the source with data to be converted into a PageObject</param>
    /// <param name="itemPerPages">The number of element to be shown for each page</param>
    /// <returns>A New PageObject From An Enumerable Source</returns>
    public static PageObject<TSource> ToPageObject<TSource>(this IEnumerable<TSource> source, long itemPerPages)
    {
        var elemCount = source.Count();
        var pageObject = new PageObject<TSource>
        {
            CurrentPage = 1,
            ItemsForEachPage = itemPerPages,
            TotalItems = elemCount,
            TotalPages = elemCount / itemPerPages,
            Pages = source.ToList()
        };
        return pageObject;
    }

    /// <summary>
    /// Convert a source of PageObject type into a new List
    /// </summary>
    /// <typeparam name="TSource">Indicate the type of the source</typeparam>
    /// <param name="source">Contains the source with data to be converted into a List</param>
    /// <returns>A New List From An Enumerable Source</returns>
    public static List<TSource>? ToListFromPageObject<TSource>(this PageObject<TSource> source)
    {
        return source.Pages ?? null;
    }
}