using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LeadGeneration.Domain.Interfaces;
using LeadGeneration.Domain.Models;

namespace LeadGeneration.Infrastructure.Integrations
{
    public sealed class ApolloLeadProvider : ILeadProvider
    {
        private readonly HttpClient _http;
        private readonly ApolloOptions _options;

        public string Name => "Apollo";

        public ApolloLeadProvider(HttpClient httpClient, ApolloOptions options)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
                throw new ArgumentException("Apollo BaseUrl is required.", nameof(options));
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new ArgumentException("Apollo ApiKey is required.", nameof(options));

            if (_http.BaseAddress == null)
                _http.BaseAddress = new Uri(_options.BaseUrl);
            if (!_http.DefaultRequestHeaders.Contains("Authorization"))
                _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
        }

        // -------- PEOPLE --------
        public async Task<PagedResult<Lead>> SearchAsync(
            LeadSearchCriteria criteria,
            CancellationToken ct = default)
        {
            var payload = new ApolloPeopleSearchRequest
            {
                Query = BlankToNull(criteria.Keyword),
                Title = BlankToNull(criteria.Title),
                Department = BlankToNull(criteria.Department),
                Seniority = BlankToNull(criteria.Seniority),
                Company = BlankToNull(criteria.CompanyName),
                Domain = BlankToNull(criteria.CompanyDomain),
                Location = BlankToNull(criteria.Location),
                TechIncludes = criteria.TechIncludes?.Count > 0 ? criteria.TechIncludes : null,
                Page = criteria.Page <= 0 ? 1 : criteria.Page,
                PageSize = criteria.PageSize <= 0 ? 25 : Math.Min(criteria.PageSize, 100)
            };

            try
            {
                var response = await _http.PostAsJsonAsync("v1/people/search", payload, ct);
                if (!response.IsSuccessStatusCode)
                    return EmptyPeople(criteria);

                var data = await response.Content.ReadFromJsonAsync<ApolloPeopleSearchResponse>(cancellationToken: ct)
                           ?? new ApolloPeopleSearchResponse();

                var items = (data.Results ?? new List<ApolloPerson>())
                    .Select(MapPersonToLead)
                    .ToList();

                return new PagedResult<Lead>
                {
                    Items = items,
                    Page = payload.Page,
                    PageSize = payload.PageSize,
                    Total = data.Total,
                    FromCache = false,
                    Source = Name
                };
            }
            catch
            {
                return EmptyPeople(criteria);
            }
        }

        // -------- COMPANIES --------
        public async Task<PagedResult<Company>> SearchCompaniesAsync(
            LeadSearchCriteria criteria,
            CancellationToken ct = default)
        {
            var payload = new ApolloCompanySearchRequest
            {
                Query = BlankToNull(criteria.Keyword),
                Domain = BlankToNull(criteria.CompanyDomain),
                Location = BlankToNull(criteria.Location),
                TechIncludes = criteria.TechIncludes?.Count > 0 ? criteria.TechIncludes : null,
                Page = criteria.Page <= 0 ? 1 : criteria.Page,
                PageSize = criteria.PageSize <= 0 ? 25 : Math.Min(criteria.PageSize, 100)
            };

            try
            {
                var response = await _http.PostAsJsonAsync("v1/companies/search", payload, ct);
                if (!response.IsSuccessStatusCode)
                    return EmptyCompanies(criteria);

                var data = await response.Content.ReadFromJsonAsync<ApolloCompanySearchResponse>(cancellationToken: ct)
                           ?? new ApolloCompanySearchResponse();

                var items = (data.Results ?? new List<ApolloCompany>())
                    .Select(MapCompanyToDomain)
                    .ToList();

                return new PagedResult<Company>
                {
                    Items = items,
                    Page = payload.Page,
                    PageSize = payload.PageSize,
                    Total = data.Total,
                    FromCache = false,
                    Source = Name
                };
            }
            catch
            {
                return EmptyCompanies(criteria);
            }
        }

        // -------- Helpers --------
        private static PagedResult<Lead> EmptyPeople(LeadSearchCriteria c) => new()
        {
            Items = Array.Empty<Lead>(),
            Page = c.Page <= 0 ? 1 : c.Page,
            PageSize = c.PageSize <= 0 ? 25 : c.PageSize,
            Total = 0,
            FromCache = false,
            Source = "Apollo"
        };

        private static PagedResult<Company> EmptyCompanies(LeadSearchCriteria c) => new()
        {
            Items = Array.Empty<Company>(),
            Page = c.Page <= 0 ? 1 : c.Page,
            PageSize = c.PageSize <= 0 ? 25 : c.PageSize,
            Total = 0,
            FromCache = false,
            Source = "Apollo"
        };

        private static Lead MapPersonToLead(ApolloPerson p)
        {
            var lead = new Lead
            {
                Person = new Person
                {
                    FirstName = p.FirstName ?? string.Empty,
                    LastName = p.LastName ?? string.Empty,
                    Title = p.Title ?? string.Empty,
                    Department = p.Department ?? string.Empty,
                    Seniority = p.Seniority ?? string.Empty,
                    LinkedInUrl = p.LinkedInUrl ?? string.Empty,
                    Location = p.Location ?? string.Empty,
                    Skills = p.Skills ?? new List<string>()
                },
                Company = new CompanyLite
                {
                    Name = p.Company?.Name ?? string.Empty,
                    Domain = p.Company?.Domain ?? string.Empty,
                    Industry = p.Company?.Industry ?? string.Empty,
                    Size = p.Company?.Size ?? string.Empty,
                    AnnualRevenueUsd = p.Company?.AnnualRevenueUsd,
                    HqLocation = p.Company?.HqLocation ?? string.Empty,
                    TechStack = p.Company?.TechStack ?? new List<string>(),
                    LinkedinUrl = p.Company?.LinkedinUrl ?? string.Empty
                },
                Contact = new ContactChannels
                {
                    WorkEmail = p.Emails?.FirstOrDefault(e => e.Type == "work")?.Address ?? string.Empty,
                    PersonalEmail = p.Emails?.FirstOrDefault(e => e.Type == "personal")?.Address ?? string.Empty,
                    DirectPhone = p.Phones?.FirstOrDefault(ph => ph.Type == "direct")?.Number ?? string.Empty,
                    MobilePhone = p.Phones?.FirstOrDefault(ph => ph.Type == "mobile")?.Number ?? string.Empty,
                    CompanyPhone = p.Phones?.FirstOrDefault(ph => ph.Type == "company")?.Number ?? string.Empty,
                    TwitterUrl = p.Socials?.Twitter ?? string.Empty,
                    GithubUrl = p.Socials?.Github ?? string.Empty,
                    EmailVerified = p.Emails?.Any(e => e.Type == "work" && e.Verified) == true
                },
                Source = "Apollo",
                IsEnriched = true
            };

            if (!string.IsNullOrWhiteSpace(p.Id))
                lead.ProviderRefs["Apollo"] = p.Id;

            return lead;
        }

        private static Company MapCompanyToDomain(ApolloCompany c) =>
            new()
            {
                Name = c.Name ?? string.Empty,
                Domain = c.Domain ?? string.Empty,
                Industry = c.Industry ?? string.Empty,
                Size = c.Size ?? string.Empty,
                AnnualRevenueUsd = c.AnnualRevenueUsd,
                HqLocation = c.HqLocation ?? string.Empty,
                TechStack = c.TechStack ?? new List<string>(),
                LinkedinUrl = c.LinkedinUrl ?? string.Empty,
                ProviderPayloads = new Dictionary<string, object> { ["Apollo"] = c }
            };

        private static string? BlankToNull(string s) => string.IsNullOrWhiteSpace(s) ? null : s;
    }

    // -------- Options / Contracts --------
    public sealed class ApolloOptions
    {
        public string BaseUrl { get; init; } = "";
        public string ApiKey { get; init; } = "";
    }

    // People
    internal sealed class ApolloPeopleSearchRequest
    {
        [JsonPropertyName("query")] public string? Query { get; init; }
        [JsonPropertyName("title")] public string? Title { get; init; }
        [JsonPropertyName("department")] public string? Department { get; init; }
        [JsonPropertyName("seniority")] public string? Seniority { get; init; }
        [JsonPropertyName("company")] public string? Company { get; init; }
        [JsonPropertyName("domain")] public string? Domain { get; init; }
        [JsonPropertyName("location")] public string? Location { get; init; }
        [JsonPropertyName("tech_includes")] public List<string>? TechIncludes { get; init; }
        [JsonPropertyName("page")] public int Page { get; init; } = 1;
        [JsonPropertyName("page_size")] public int PageSize { get; init; } = 25;
    }
    internal sealed class ApolloPeopleSearchResponse
    {
        [JsonPropertyName("total")] public long Total { get; init; }
        [JsonPropertyName("results")] public List<ApolloPerson>? Results { get; init; }
    }
    internal sealed class ApolloPerson
    {
        [JsonPropertyName("id")] public string? Id { get; init; }
        [JsonPropertyName("first_name")] public string? FirstName { get; init; }
        [JsonPropertyName("last_name")] public string? LastName { get; init; }
        [JsonPropertyName("title")] public string? Title { get; init; }
        [JsonPropertyName("department")] public string? Department { get; init; }
        [JsonPropertyName("seniority")] public string? Seniority { get; init; }
        [JsonPropertyName("linkedin_url")] public string? LinkedInUrl { get; init; }
        [JsonPropertyName("location")] public string? Location { get; init; }
        [JsonPropertyName("skills")] public List<string>? Skills { get; init; }
        [JsonPropertyName("emails")] public List<ApolloEmail>? Emails { get; init; }
        [JsonPropertyName("phones")] public List<ApolloPhone>? Phones { get; init; }
        [JsonPropertyName("socials")] public ApolloSocials? Socials { get; init; }
        [JsonPropertyName("company")] public ApolloCompany? Company { get; init; }
    }
    internal sealed class ApolloEmail
    {
        [JsonPropertyName("type")] public string? Type { get; init; }
        [JsonPropertyName("address")] public string? Address { get; init; }
        [JsonPropertyName("verified")] public bool Verified { get; init; }
    }
    internal sealed class ApolloPhone
    {
        [JsonPropertyName("type")] public string? Type { get; init; }
        [JsonPropertyName("number")] public string? Number { get; init; }
    }
    internal sealed class ApolloSocials
    {
        [JsonPropertyName("twitter")] public string? Twitter { get; init; }
        [JsonPropertyName("github")] public string? Github { get; init; }
    }

    // Companies
    internal sealed class ApolloCompanySearchRequest
    {
        [JsonPropertyName("query")] public string? Query { get; init; }
        [JsonPropertyName("domain")] public string? Domain { get; init; }
        [JsonPropertyName("location")] public string? Location { get; init; }
        [JsonPropertyName("tech_includes")] public List<string>? TechIncludes { get; init; }
        [JsonPropertyName("page")] public int Page { get; init; } = 1;
        [JsonPropertyName("page_size")] public int PageSize { get; init; } = 25;
    }
    internal sealed class ApolloCompanySearchResponse
    {
        [JsonPropertyName("total")] public long Total { get; init; }
        [JsonPropertyName("results")] public List<ApolloCompany>? Results { get; init; }
    }
    internal sealed class ApolloCompany
    {
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("domain")] public string? Domain { get; init; }
        [JsonPropertyName("industry")] public string? Industry { get; init; }
        [JsonPropertyName("size")] public string? Size { get; init; }
        [JsonPropertyName("revenue_usd")] public double? AnnualRevenueUsd { get; init; }
        [JsonPropertyName("hq_location")] public string? HqLocation { get; init; }
        [JsonPropertyName("tech_stack")] public List<string>? TechStack { get; init; }
        [JsonPropertyName("linkedin_url")] public string? LinkedinUrl { get; init; }
    }
}
