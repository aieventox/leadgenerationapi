using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LeadGeneration.Domain.Interfaces;
using LeadGeneration.Domain.Models;

namespace LeadGeneration.Application.Services
{
    /// <summary>
    /// Routes outbound searches to one or more providers.
    /// Currently uses a simple priority order; extensible later.
    /// </summary>
    public sealed class ProviderRouter
    {
        private readonly IReadOnlyList<ILeadProvider> _providers;

        /// <param name="providers">All registered providers (e.g., Apollo, ZoomInfo).</param>
        public ProviderRouter(IEnumerable<ILeadProvider> providers)
        {
            _providers = providers?.ToList() ?? throw new ArgumentNullException(nameof(providers));
            if (_providers.Count == 0)
                throw new InvalidOperationException("No ILeadProvider implementations registered.");
        }

        /// <summary>
        /// Fan-out search over providers until you have results (or exhaust all).
        /// Strategy: first provider with non-empty results wins. You can switch to
        /// 'aggregate' behavior later if you want to combine results across providers.
        /// </summary>
        public async Task<PagedResult<Lead>> SearchAsync(
            LeadSearchCriteria criteria,
            CancellationToken ct = default)
        {
            foreach (var provider in _providers)
            {
                var page = await provider.SearchAsync(criteria, ct);
                if (page.Items.Count > 0)
                {
                    // mark source for traceability
                    page.FromCache = false;
                    page.Source = provider.Name;
                    return page;
                }
            }

            // All providers returned nothing or errored -> empty page
            return new PagedResult<Lead>
            {
                Items = Array.Empty<Lead>(),
                Page = criteria.Page,
                PageSize = criteria.PageSize,
                Total = 0,
                FromCache = false,
                Source = _providers.First().Name
            };
        }
    }
}
