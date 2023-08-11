// System Class
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Custom Class
using LothiumDB.Enumerations;
using LothiumDB.Exceptions;
using LothiumDB.Helpers;
using LothiumDB.Interfaces;

namespace LothiumDB
{
    /// <summary>
    /// Extension Class Used By The Database Class
    /// </summary>
    public static partial class DataExtensions
    {
        /// <summary>
        /// Retrive the first element from a source list
        /// </summary>
        /// <typeparam name="TSource">Indicate the type of the source</typeparam>
        /// <param name="source">Contains the list of element</param>
        /// <returns>First element inside the source list</returns>
        public static TSource FirstElement<TSource>(this IList<TSource> source)
        {
            if (source == null || source.Count() == 0) return default(TSource);
            return source[0];
        }

        /// <summary>
        /// Retrive the last element from a source list
        /// </summary>
        /// <typeparam name="TSource">Indicate the type of the source</typeparam>
        /// <param name="source">Contains the list of element</param>
        /// <returns>Last element inside the source list</returns>
        public static TSource LastElement<TSource>(this IList<TSource> source)
        {
            if (source == null || source.Count() == 0) return default(TSource);
            return source[source.Count() - 1];
        }

        /// <summary>
        /// Retrive an element at a specific index from a source list
        /// </summary>
        /// <typeparam name="TSource">Indicate the type of the source</typeparam>
        /// <param name="source">Contains the list of element</param>
        /// <param name="index">Indicate the index of the element to be searched</param>
        /// <returns>The element inside the source list at a specific index</returns>
        public static TSource ElementAtIndex<TSource>(this IList<TSource> source, int index)
        {
            if (source == null || source.Count() == 0 || index > source.Count() || index < 0) return default(TSource);
            return source[index];
        }
    }
}
