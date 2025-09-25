using System.Collections.Generic;

namespace LeadGeneration.Domain.Models
{
    /// <summary>
    /// Shared paging envelope used by repository interfaces (lives in Domain to avoid cycles).
    /// </summary>
    public sealed class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = new List<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long Total { get; set; }
        public bool FromCache { get; set; } = true;  // DB by default
        public string Source { get; set; } = "DB";
    }
}
