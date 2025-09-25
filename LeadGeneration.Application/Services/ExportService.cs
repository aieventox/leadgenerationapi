using System;
using System.Threading;
using System.Threading.Tasks;
using LeadGeneration.Domain.Interfaces;
using LeadGeneration.Domain.Models;

namespace LeadGeneration.Application.Services
{
    /// <summary>
    /// Batches for exporting (default 10 at a time).
    /// </summary>
    public sealed class ExportService
    {
        private readonly ILeadRepository _repo;

        public ExportService(ILeadRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public Task<PagedResult<Lead>> GetLeadBatchAsync(int page, int batchSize, CancellationToken ct = default)
        {
            var criteria = new LeadSearchCriteria
            {
                Page = page <= 0 ? 1 : page,
                PageSize = batchSize <= 0 ? 10 : batchSize
            };
            return _repo.SearchLeadsAsync(criteria, ct);
        }

        public Task<PagedResult<Company>> GetCompanyBatchAsync(int page, int batchSize, CancellationToken ct = default)
        {
            page = page <= 0 ? 1 : page;
            batchSize = batchSize <= 0 ? 10 : batchSize;
            return _repo.GetCompaniesAsync(page, batchSize, ct);
        }
    }
}
