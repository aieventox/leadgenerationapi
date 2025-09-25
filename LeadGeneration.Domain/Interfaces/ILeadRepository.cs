using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeadGeneration.Domain.Models;

namespace LeadGeneration.Domain.Interfaces
{
    /// <summary>
    /// Unified repository boundary for leads, companies, lists, sequences, and engagement logs.
    /// </summary>
    public interface ILeadRepository
    {
        // -------- LEADS --------
        Task<PagedResult<Lead>> SearchLeadsAsync(
            LeadSearchCriteria criteria,
            CancellationToken ct = default);

        Task<Lead?> GetLeadByIdAsync(string leadId, CancellationToken ct = default);

        Task<IReadOnlyList<string>> UpsertLeadsAsync(
            IEnumerable<Lead> leads,
            CancellationToken ct = default);

        // -------- COMPANIES --------
        Task<Company?> GetCompanyByDomainAsync(string domain, CancellationToken ct = default);

        Task<IReadOnlyList<string>> UpsertCompaniesAsync(
            IEnumerable<Company> companies,
            CancellationToken ct = default);

        /// <summary>Page through companies (for export, admin, etc.).</summary>
        Task<PagedResult<Company>> GetCompaniesAsync(
            int page,
            int pageSize,
            CancellationToken ct = default);

        // -------- LISTS --------
        Task<string> CreateListAsync(string name, string? description, CancellationToken ct = default);
        Task AddLeadsToListAsync(string listId, IEnumerable<string> leadIds, CancellationToken ct = default);
        Task RemoveLeadsFromListAsync(string listId, IEnumerable<string> leadIds, CancellationToken ct = default);
        Task<ProspectList?> GetListByIdAsync(string listId, CancellationToken ct = default);
        Task<PagedResult<ProspectList>> GetListsAsync(int page, int pageSize, CancellationToken ct = default);

        // -------- SEQUENCES --------
        Task<string> CreateSequenceAsync(Sequence sequence, CancellationToken ct = default);
        Task<Sequence?> GetSequenceAsync(string sequenceId, CancellationToken ct = default);
        Task<PagedResult<Sequence>> GetSequencesAsync(int page, int pageSize, CancellationToken ct = default);

        // -------- ENGAGEMENT LOGS --------
        Task LogEngagementAsync(EngagementLog log, CancellationToken ct = default);
    }
}
