using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeadGeneration.Application.DTO;
using LeadGeneration.Domain.Interfaces;
using LeadGeneration.Domain.Models;

namespace LeadGeneration.Application.Services
{
    /// <summary>
    /// Business logic for prospect lists (create, query, add/remove leads).
    /// </summary>
    public sealed class ListService
    {
        private readonly ILeadRepository _repo;

        public ListService(ILeadRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<string> CreateAsync(string name, string? description, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("List name is required.", nameof(name));

            return await _repo.CreateListAsync(name.Trim(), description?.Trim(), ct);
        }

        public async Task<(IReadOnlyList<ListDto> Items, int Page, int PageSize, long Total)> GetPagedAsync(
            int page, int pageSize, CancellationToken ct = default)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 25 : pageSize;

            var res = await _repo.GetListsAsync(page, pageSize, ct);
            var items = new List<ListDto>(res.Items.Count);

            foreach (var l in res.Items)
                items.Add(Map(l));

            return (items, res.Page, res.PageSize, res.Total);
        }

        public async Task<ListDto?> GetByIdAsync(string listId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(listId)) return null;

            var list = await _repo.GetListByIdAsync(listId, ct);
            return list is null ? null : Map(list);
        }

        public Task AddLeadsAsync(string listId, IEnumerable<string> leadIds, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(listId))
                throw new ArgumentException("listId is required.", nameof(listId));

            return _repo.AddLeadsToListAsync(listId, leadIds, ct);
        }

        public Task RemoveLeadsAsync(string listId, IEnumerable<string> leadIds, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(listId))
                throw new ArgumentException("listId is required.", nameof(listId));

            return _repo.RemoveLeadsFromListAsync(listId, leadIds, ct);
        }

        private static ListDto Map(ProspectList m) =>
            new()
            {
                ListId = m.Id,
                Name = m.Name,
                Description = m.Description ?? "",
                CreatedUtc = m.CreatedUtc,
                LeadCount = m.LeadIds?.Count ?? 0
            };
    }
}
