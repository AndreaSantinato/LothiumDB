namespace LothiumDB.Extensions;

/// <summary>
/// Custom object used to create a Paging helper
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class PageObject<T>
{
    /// <summary>
    /// Indicates the current page loaded for this page object
    /// </summary>
    public long CurrentPage { get; set; }

    /// <summary>
    /// Indicates the number of items to load for each page in this page object
    /// </summary>
    public long ItemsForEachPage { get; set; }

    /// <summary>
    /// Indicates the number of items to skip in the page query
    /// </summary>
    public long ItemsToBeSkipped { get; set; }

    /// <summary>
    /// Indicates the total number of items in this page object
    /// </summary>
    public long TotalItems { get; set; }

    /// <summary>
    /// Indicates the total number of pages created for this page object
    /// </summary>
    public long TotalPages { get; set; }

    /// <summary>
    /// Contains the pages with all the element extracted by a query
    /// </summary>
    public List<T>? Pages { get; set; }
}