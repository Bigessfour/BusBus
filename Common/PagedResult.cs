using System.Collections.Generic;

#nullable enable

namespace BusBus.Common
{
    /// <summary>
    /// Represents a paginated result set
    /// </summary>
    /// <typeparam name="T">The type of items in the result set</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// The items in the current page
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// The total count of items across all pages
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// The current page number (1-based)
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// The size of each page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// The total number of pages
        /// </summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNext => PageNumber < TotalPages;

        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPrevious => PageNumber > 1;
    }
}
