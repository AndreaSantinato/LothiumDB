namespace LothiumDB.Linq;

/// <summary>
/// Extension Class Used By The Database Class
/// </summary>
public static partial class QueryExtension
{
    /// <summary>
    /// Retrieve the first element from a source list
    /// </summary>
    /// <typeparam name="TSource">Indicate the type of the source</typeparam>
    /// <param name="source">Contains the list of element</param>
    /// <returns>First element inside the source list</returns>
    public static TSource? FirstElement<TSource>(this IList<TSource> source)
        => source.ElementAtIndex(0);

    /// <summary>
    /// Retrieve the last element from a source list
    /// </summary>
    /// <typeparam name="TSource">Indicate the type of the source</typeparam>
    /// <param name="source">Contains the list of element</param>
    /// <returns>Last element inside the source list</returns>
    public static TSource? LastElement<TSource>(this IList<TSource> source)
        => source.ElementAtIndex(source.Count - 1);

    /// <summary>
    /// Retrieve an element at a specific index from a source list
    /// </summary>
    /// <typeparam name="TSource">Indicate the type of the source</typeparam>
    /// <param name="source">Contains the list of element</param>
    /// <param name="index">Indicate the index of the element to be searched</param>
    /// <returns>The element inside the source list at a specific index</returns>
    public static TSource? ElementAtIndex<TSource>(this IList<TSource> source, int index)
    {
        if (!source.Any()) return default;
        if (index > source.Count) return default;
        return index < 0 ? default : source[index];
    }
}