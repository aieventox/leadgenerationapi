using System.Collections.Generic;

namespace LeadGeneration.Application.DTO
{
    /// <summary>
    /// Single, clean search request used by LeadsController and LeadService.
    /// Keep it small and future-proof.
    /// </summary>
    public sealed class SearchRequestDto
    {
        public string Keyword { get; init; } = "";         // free text
        public string Title { get; init; } = "";           // optional filter
        public string Department { get; init; } = "";
        public string Seniority { get; init; } = "";
        public string CompanyName { get; init; } = "";
        public string CompanyDomain { get; init; } = "";
        public string Location { get; init; } = "";        // city/state/country
        public List<string> TechIncludes { get; init; } = new();

        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 25;           // default page size
        public bool ForceProvider { get; init; } = false;  // skip cache/DB and go to provider(s)
    }

    /// <summary>
    /// Standard search result with paging and cache hint.
    /// </summary>
    public sealed class SearchResultDto
    {
        public List<LeadDto> Items { get; init; } = new();
        public int Page { get; init; }
        public int PageSize { get; init; }
        public long Total { get; init; }
        public bool FromCache { get; init; }           // true when served from DB
        public string Source { get; init; } = "";      // "DB", "Apollo", "Mixed"
    }
}
