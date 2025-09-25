using System;
using System.Threading;
using System.Threading.Tasks;
using LeadGeneration.Application.DTO;
using LeadGeneration.Domain.Interfaces;
using LeadGeneration.Domain.Models;

namespace LeadGeneration.Application.Services
{
    /// <summary>
    /// Imports PEOPLE and COMPANIES from provider(s) into DB.
    /// </summary>
    public sealed class ImportService
    {
        private readonly ILeadRepository _repo;
        private readonly ProviderRouter _router;     // for people
        private readonly ILeadProvider _provider;    // for companies (use primary provider)

        public ImportService(ILeadRepository repo, ProviderRouter router, ILeadProvider provider)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>Import PEOPLE (force provider) and upsert to DB.</summary>
        public async Task<object> ImportPeopleAsync(SearchRequestDto request, CancellationToken ct = default)
        {
            // Force external
            var criteria = new LeadSearchCriteria
            {
                Keyword = request.Keyword ?? string.Empty,
                Title = request.Title ?? string.Empty,
                Department = request.Department ?? string.Empty,
                Seniority = request.Seniority ?? string.Empty,
                CompanyName = request.CompanyName ?? string.Empty,
                CompanyDomain = request.CompanyDomain ?? string.Empty,
                Location = request.Location ?? string.Empty,
                TechIncludes = request.TechIncludes ?? new System.Collections.Generic.List<string>(),
                Page = request.Page <= 0 ? 1 : request.Page,
                PageSize = request.PageSize <= 0 ? 25 : request.PageSize,
                ForceProvider = true
            };

            var page = await _router.SearchAsync(criteria, ct);
            if (page.Items.Count > 0)
                await _repo.UpsertLeadsAsync(page.Items, ct);

            return new { ok = true, imported = page.Items.Count, source = page.Source };
        }

        /// <summary>Import COMPANIES from provider and upsert to DB.</summary>
        public async Task<object> ImportCompaniesAsync(SearchRequestDto request, CancellationToken ct = default)
        {
            var criteria = new LeadSearchCriteria
            {
                Keyword = request.Keyword ?? string.Empty,
                CompanyDomain = request.CompanyDomain ?? string.Empty,
                Location = request.Location ?? string.Empty,
                TechIncludes = request.TechIncludes ?? new System.Collections.Generic.List<string>(),
                Page = request.Page <= 0 ? 1 : request.Page,
                PageSize = request.PageSize <= 0 ? 25 : request.PageSize,
                ForceProvider = true
            };

            var page = await _provider.SearchCompaniesAsync(criteria, ct);
            if (page.Items.Count > 0)
                await _repo.UpsertCompaniesAsync(page.Items, ct);

            return new { ok = true, imported = page.Items.Count, source = page.Source };
        }
    }
}
