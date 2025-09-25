using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LeadGeneration.Application.DTO;
using LeadGeneration.Domain.Interfaces;
using LeadGeneration.Domain.Models;

namespace LeadGeneration.Application.Services
{
    /// <summary>
    /// Lead-facing business logic:
    /// 1) Search DB first; optionally fan-out to providers via ProviderRouter.
    /// 2) Normalize & upsert provider results; return DTOs.
    /// 3) Simple getters/operations for Leads.
    /// Lists/Sequences are handled by their own services (next modules).
    /// </summary>
    public sealed class LeadService
    {
        private readonly ILeadRepository _repo;
        private readonly ProviderRouter _router;

        public LeadService(ILeadRepository repo, ProviderRouter router)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _router = router ?? throw new ArgumentNullException(nameof(router));
        }

        // ---------- Queries ----------

        public async Task<SearchResultDto> SearchAsync(
            SearchRequestDto request,
            CancellationToken ct = default)
        {
            // 1) Try DB first unless forced
            var criteria = MapToCriteria(request);
            PagedResult<Lead> page;

            if (!request.ForceProvider)
            {
                page = await _repo.SearchLeadsAsync(criteria, ct);
                if (page.Items.Count > 0)
                {
                    return new SearchResultDto
                    {
                        Items = page.Items.Select(MapToDto).ToList(),
                        Page = page.Page,
                        PageSize = page.PageSize,
                        Total = page.Total,
                        FromCache = true,
                        Source = "DB"
                    };
                }
            }

            // 2) Provider fallback (or forced)
            page = await _router.SearchAsync(criteria, ct);

            // 3) Upsert what we got so future calls hit DB
            if (page.Items.Count > 0)
            {
                await _repo.UpsertLeadsAsync(page.Items, ct);
            }

            return new SearchResultDto
            {
                Items = page.Items.Select(MapToDto).ToList(),
                Page = page.Page,
                PageSize = page.PageSize,
                Total = page.Total,
                FromCache = false,
                Source = page.Source
            };
        }

        public async Task<LeadDto?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var lead = await _repo.GetLeadByIdAsync(id, ct);
            return lead is null ? null : MapToDto(lead);
        }

        // ---------- Commands (minimal, extend as needed) ----------

        public async Task<IReadOnlyList<string>> UpsertAsync(IEnumerable<LeadDto> dtos, CancellationToken ct = default)
        {
            var models = dtos.Select(MapToModel).ToList();
            return await _repo.UpsertLeadsAsync(models, ct);
        }

        // ---------- Mapping (Domain <-> DTO) ----------

        private static LeadSearchCriteria MapToCriteria(SearchRequestDto s) =>
            new()
            {
                Keyword = s.Keyword ?? string.Empty,
                Title = s.Title ?? string.Empty,
                Department = s.Department ?? string.Empty,
                Seniority = s.Seniority ?? string.Empty,
                CompanyName = s.CompanyName ?? string.Empty,
                CompanyDomain = s.CompanyDomain ?? string.Empty,
                Location = s.Location ?? string.Empty,
                TechIncludes = s.TechIncludes ?? new List<string>(),
                Page = s.Page <= 0 ? 1 : s.Page,
                PageSize = s.PageSize <= 0 ? 25 : s.PageSize,
                ForceProvider = s.ForceProvider
            };

        private static LeadDto MapToDto(Lead m) =>
            new LeadDto
            {
                LeadId = m.Id,
                Source = m.Source,
                FirstSeenUtc = m.FirstSeenUtc,
                LastUpdatedUtc = m.LastUpdatedUtc,
                IsEnriched = m.IsEnriched,
                ProviderRefs = m.ProviderRefs,

                Person = new PersonDto
                {
                    FirstName = m.Person.FirstName,
                    LastName = m.Person.LastName,
                    Title = m.Person.Title,
                    Department = m.Person.Department,
                    Seniority = m.Person.Seniority,
                    LinkedInUrl = m.Person.LinkedInUrl,
                    Location = m.Person.Location,
                    Skills = m.Person.Skills ?? new List<string>()
                },
                Company = new CompanyLiteDto
                {
                    CompanyId = m.Company.CompanyId,
                    Name = m.Company.Name,
                    Domain = m.Company.Domain,
                    Industry = m.Company.Industry,
                    Size = m.Company.Size,
                    AnnualRevenueUsd = m.Company.AnnualRevenueUsd,
                    HqLocation = m.Company.HqLocation,
                    TechStack = m.Company.TechStack ?? new List<string>(),
                    LinkedinUrl = m.Company.LinkedinUrl
                },
                Contact = new ContactDto
                {
                    WorkEmail = m.Contact.WorkEmail,
                    PersonalEmail = m.Contact.PersonalEmail,
                    DirectPhone = m.Contact.DirectPhone,
                    MobilePhone = m.Contact.MobilePhone,
                    CompanyPhone = m.Contact.CompanyPhone,
                    TwitterUrl = m.Contact.TwitterUrl,
                    GithubUrl = m.Contact.GithubUrl,
                    EmailVerified = m.Contact.EmailVerified
                }
            };

        private static Lead MapToModel(LeadDto d) =>
            new Lead
            {
                Id = d.LeadId ?? string.Empty,
                Source = d.Source ?? "DB",
                FirstSeenUtc = d.FirstSeenUtc,
                LastUpdatedUtc = d.LastUpdatedUtc,
                IsEnriched = d.IsEnriched,
                ProviderRefs = d.ProviderRefs ?? new Dictionary<string, string>(),
                Person = new Person
                {
                    FirstName = d.Person?.FirstName ?? string.Empty,
                    LastName = d.Person?.LastName ?? string.Empty,
                    Title = d.Person?.Title ?? string.Empty,
                    Department = d.Person?.Department ?? string.Empty,
                    Seniority = d.Person?.Seniority ?? string.Empty,
                    LinkedInUrl = d.Person?.LinkedInUrl ?? string.Empty,
                    Location = d.Person?.Location ?? string.Empty,
                    Skills = d.Person?.Skills ?? new List<string>()
                },
                Company = new CompanyLite
                {
                    CompanyId = d.Company?.CompanyId ?? string.Empty,
                    Name = d.Company?.Name ?? string.Empty,
                    Domain = d.Company?.Domain ?? string.Empty,
                    Industry = d.Company?.Industry ?? string.Empty,
                    Size = d.Company?.Size ?? string.Empty,
                    AnnualRevenueUsd = d.Company?.AnnualRevenueUsd,
                    HqLocation = d.Company?.HqLocation ?? string.Empty,
                    TechStack = d.Company?.TechStack ?? new List<string>(),
                    LinkedinUrl = d.Company?.LinkedinUrl ?? string.Empty
                },
                Contact = new ContactChannels
                {
                    WorkEmail = d.Contact?.WorkEmail ?? string.Empty,
                    PersonalEmail = d.Contact?.PersonalEmail ?? string.Empty,
                    DirectPhone = d.Contact?.DirectPhone ?? string.Empty,
                    MobilePhone = d.Contact?.MobilePhone ?? string.Empty,
                    CompanyPhone = d.Contact?.CompanyPhone ?? string.Empty,
                    TwitterUrl = d.Contact?.TwitterUrl ?? string.Empty,
                    GithubUrl = d.Contact?.GithubUrl ?? string.Empty,
                    EmailVerified = d.Contact?.EmailVerified ?? false
                }
            };
    }
}
