using System.Threading;
using System.Threading.Tasks;
using LeadGeneration.Domain.Models;

namespace LeadGeneration.Domain.Interfaces
{
    /// <summary>
    /// Single integration contract for any external lead source (Apollo, etc.).
    /// </summary>
    public interface ILeadProvider
    {
        /// <summary>Provider name for diagnostics (e.g., "Apollo").</summary>
        string Name { get; }

        /// <summary>Provider-side search for PEOPLE mapped into domain Leads.</summary>
        Task<PagedResult<Lead>> SearchAsync(
            LeadSearchCriteria criteria,
            CancellationToken ct = default);

        /// <summary>Provider-side search for COMPANIES mapped into domain Companies.</summary>
        Task<PagedResult<Company>> SearchCompaniesAsync(
            LeadSearchCriteria criteria,
            CancellationToken ct = default);
    }
}
