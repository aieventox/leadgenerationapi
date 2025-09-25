using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LeadGeneration.Domain.Interfaces;
using LeadGeneration.Domain.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LeadGeneration.Infrastructure.Repository
{
    public sealed class LeadRepository : ILeadRepository
    {
        private readonly MongoDbContext _context;
        public LeadRepository(MongoDbContext context)
        {
            _context = context;
        }

        // ---------------- LEADS ----------------

        public async Task<PagedResult<Lead>> SearchLeadsAsync(
            LeadSearchCriteria criteria,
            CancellationToken ct = default)
        {
            var filter = BuildLeadFilter(criteria);
            var find = _context.Leads.Find(filter).SortByDescending(x => x.LastUpdatedUtc);

            var skip = (criteria.Page - 1) * criteria.PageSize;
            var total = await find.CountDocumentsAsync(ct);
            var items = await find.Skip(skip).Limit(criteria.PageSize).ToListAsync(ct);

            return new PagedResult<Lead>
            {
                Items = items,
                Page = criteria.Page,
                PageSize = criteria.PageSize,
                Total = total,
                FromCache = true,
                Source = "DB"
            };
        }

        public async Task<Lead?> GetLeadByIdAsync(string leadId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(leadId)) return null;
            var filter = Builders<Lead>.Filter.Eq(x => x.Id, leadId);
            return await _context.Leads.Find(filter).FirstOrDefaultAsync(ct);
        }

        public async Task<IReadOnlyList<string>> UpsertLeadsAsync(
            IEnumerable<Lead> leads,
            CancellationToken ct = default)
        {
            var requests = new List<WriteModel<Lead>>();

            foreach (var lead in leads)
            {
                FilterDefinition<Lead> filter;

                if (!string.IsNullOrWhiteSpace(lead.Id))
                {
                    filter = Builders<Lead>.Filter.Eq(x => x.Id, lead.Id);
                }
                else if (!string.IsNullOrWhiteSpace(lead.Contact.WorkEmail))
                {
                    filter = Builders<Lead>.Filter.Eq(x => x.Contact.WorkEmail, lead.Contact.WorkEmail);
                }
                else
                {
                    var li = lead.Person.LinkedInUrl ?? string.Empty;
                    var dom = lead.Company.Domain ?? string.Empty;
                    filter = Builders<Lead>.Filter.Eq(x => x.Person.LinkedInUrl, li) &
                             Builders<Lead>.Filter.Eq(x => x.Company.Domain, dom);
                }

                if (string.IsNullOrWhiteSpace(lead.Id))
                    lead.Id = ObjectId.GenerateNewId().ToString();

                lead.LastUpdatedUtc ??= DateTime.UtcNow;
                lead.FirstSeenUtc ??= lead.LastUpdatedUtc;

                var update = Builders<Lead>.Update
                    .Set(x => x.Person, lead.Person)
                    .Set(x => x.Company, lead.Company)
                    .Set(x => x.Contact, lead.Contact)
                    .Set(x => x.Source, lead.Source)
                    .Set(x => x.IsEnriched, lead.IsEnriched)
                    .Set(x => x.ProviderRefs, lead.ProviderRefs)
                    .Set(x => x.LastUpdatedUtc, lead.LastUpdatedUtc)
                    .SetOnInsert(x => x.Id, lead.Id)
                    .SetOnInsert(x => x.FirstSeenUtc, lead.FirstSeenUtc);

                requests.Add(new UpdateOneModel<Lead>(filter, update) { IsUpsert = true });
            }

            if (requests.Count == 0) return Array.Empty<string>();

            await _context.Leads.BulkWriteAsync(requests, new BulkWriteOptions { IsOrdered = false }, ct);
            return leads.Select(l => l.Id).ToArray();
        }

        private static FilterDefinition<Lead> BuildLeadFilter(LeadSearchCriteria c)
        {
            var f = Builders<Lead>.Filter.Empty;
            var and = new List<FilterDefinition<Lead>>();

            if (!string.IsNullOrWhiteSpace(c.Keyword))
            {
                var regex = new BsonRegularExpression(c.Keyword, "i");
                and.Add(Builders<Lead>.Filter.Or(
                    Builders<Lead>.Filter.Regex(x => x.Person.FirstName, regex),
                    Builders<Lead>.Filter.Regex(x => x.Person.LastName, regex),
                    Builders<Lead>.Filter.Regex(x => x.Person.Title, regex),
                    Builders<Lead>.Filter.Regex(x => x.Company.Name, regex),
                    Builders<Lead>.Filter.Regex(x => x.Company.Domain, regex)
                ));
            }
            if (!string.IsNullOrWhiteSpace(c.Title))
                and.Add(Builders<Lead>.Filter.Regex(x => x.Person.Title, new BsonRegularExpression(c.Title, "i")));
            if (!string.IsNullOrWhiteSpace(c.Department))
                and.Add(Builders<Lead>.Filter.Regex(x => x.Person.Department, new BsonRegularExpression(c.Department, "i")));
            if (!string.IsNullOrWhiteSpace(c.Seniority))
                and.Add(Builders<Lead>.Filter.Regex(x => x.Person.Seniority, new BsonRegularExpression(c.Seniority, "i")));
            if (!string.IsNullOrWhiteSpace(c.CompanyName))
                and.Add(Builders<Lead>.Filter.Regex(x => x.Company.Name, new BsonRegularExpression(c.CompanyName, "i")));
            if (!string.IsNullOrWhiteSpace(c.CompanyDomain))
                and.Add(Builders<Lead>.Filter.Eq(x => x.Company.Domain, c.CompanyDomain));
            if (!string.IsNullOrWhiteSpace(c.Location))
                and.Add(Builders<Lead>.Filter.Regex(x => x.Person.Location, new BsonRegularExpression(c.Location, "i")));
            if (c.TechIncludes?.Count > 0)
                and.Add(Builders<Lead>.Filter.All(x => x.Company.TechStack, c.TechIncludes));

            if (and.Count > 0) f = Builders<Lead>.Filter.And(and);
            return f;
        }

        // ---------------- COMPANIES ----------------

        public Task<Company?> GetCompanyByDomainAsync(string domain, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(domain)) return Task.FromResult<Company?>(null);
            return _context.Companies.Find(c => c.Domain == domain).FirstOrDefaultAsync(ct);
        }

        public async Task<IReadOnlyList<string>> UpsertCompaniesAsync(
            IEnumerable<Company> companies,
            CancellationToken ct = default)
        {
            var requests = new List<WriteModel<Company>>();
            var ids = new List<string>();

            foreach (var c in companies)
            {
                if (string.IsNullOrWhiteSpace(c.Id))
                    c.Id = ObjectId.GenerateNewId().ToString();

                ids.Add(c.Id);

                var filter = Builders<Company>.Filter.Eq(x => x.Domain, c.Domain);
                var update = Builders<Company>.Update
                    .Set(x => x.Name, c.Name)
                    .Set(x => x.Industry, c.Industry)
                    .Set(x => x.Size, c.Size)
                    .Set(x => x.AnnualRevenueUsd, c.AnnualRevenueUsd)
                    .Set(x => x.HqLocation, c.HqLocation)
                    .Set(x => x.TechStack, c.TechStack)
                    .Set(x => x.LinkedinUrl, c.LinkedinUrl)
                    .Set(x => x.ProviderPayloads, c.ProviderPayloads)
                    .SetOnInsert(x => x.Id, c.Id)
                    .SetOnInsert(x => x.Domain, c.Domain);

                requests.Add(new UpdateOneModel<Company>(filter, update) { IsUpsert = true });
            }

            if (requests.Count > 0)
                await _context.Companies.BulkWriteAsync(requests, new BulkWriteOptions { IsOrdered = false }, ct);

            return ids;
        }

        public async Task<PagedResult<Company>> GetCompaniesAsync(int page, int pageSize, CancellationToken ct = default)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var find = _context.Companies.Find(Builders<Company>.Filter.Empty)
                                    .SortBy(c => c.Name);

            var skip = (page - 1) * pageSize;
            var total = await find.CountDocumentsAsync(ct);
            var items = await find.Skip(skip).Limit(pageSize).ToListAsync(ct);

            return new PagedResult<Company>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                Total = total,
                FromCache = true,
                Source = "DB"
            };
        }

        // ---------------- LISTS / SEQUENCES / LOGS ----------------
        public async Task<string> CreateListAsync(string name, string? description, CancellationToken ct = default)
        {
            var doc = new ProspectList
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = name,
                Description = description,
                CreatedUtc = DateTime.UtcNow,
                LeadIds = new List<string>()
            };
            await _context.ProspectLists.InsertOneAsync(doc, cancellationToken: ct);
            return doc.Id;
        }

        public async Task AddLeadsToListAsync(string listId, IEnumerable<string> leadIds, CancellationToken ct = default)
        {
            var filter = Builders<ProspectList>.Filter.Eq(x => x.Id, listId);
            var update = Builders<ProspectList>.Update.AddToSetEach(x => x.LeadIds, leadIds.ToList());
            await _context.ProspectLists.UpdateOneAsync(filter, update, cancellationToken: ct);
        }

        public async Task RemoveLeadsFromListAsync(string listId, IEnumerable<string> leadIds, CancellationToken ct = default)
        {
            var filter = Builders<ProspectList>.Filter.Eq(x => x.Id, listId);
            var update = Builders<ProspectList>.Update.PullAll(x => x.LeadIds, leadIds.ToList());
            await _context.ProspectLists.UpdateOneAsync(filter, update, cancellationToken: ct);
        }

        public Task<ProspectList?> GetListByIdAsync(string listId, CancellationToken ct = default)
        {
            return _context.ProspectLists.Find(l => l.Id == listId).FirstOrDefaultAsync(ct);
        }

        public async Task<PagedResult<ProspectList>> GetListsAsync(int page, int pageSize, CancellationToken ct = default)
        {
            var find = _context.ProspectLists.Find(Builders<ProspectList>.Filter.Empty)
                                        .SortByDescending(l => l.CreatedUtc);

            var skip = (page - 1) * pageSize;
            var total = await find.CountDocumentsAsync(ct);
            var items = await find.Skip(skip).Limit(pageSize).ToListAsync(ct);

            return new PagedResult<ProspectList>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                Total = total,
                FromCache = true,
                Source = "DB"
            };
        }

        public async Task<string> CreateSequenceAsync(Sequence sequence, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sequence.Id))
                sequence.Id = ObjectId.GenerateNewId().ToString();

            sequence.CreatedUtc = sequence.CreatedUtc == default ? DateTime.UtcNow : sequence.CreatedUtc;
            await _context.Sequences.InsertOneAsync(sequence, cancellationToken: ct);
            return sequence.Id;
        }

        public Task<Sequence?> GetSequenceAsync(string sequenceId, CancellationToken ct = default)
        {
            return _context.Sequences.Find(s => s.Id == sequenceId).FirstOrDefaultAsync(ct);
        }

        public async Task<PagedResult<Sequence>> GetSequencesAsync(int page, int pageSize, CancellationToken ct = default)
        {
            var find = _context.Sequences.Find(Builders<Sequence>.Filter.Empty)
                                    .SortByDescending(s => s.CreatedUtc);
            var skip = (page - 1) * pageSize;
            var total = await find.CountDocumentsAsync(ct);
            var items = await find.Skip(skip).Limit(pageSize).ToListAsync(ct);

            return new PagedResult<Sequence>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                Total = total,
                FromCache = true,
                Source = "DB"
            };
        }

        public async Task LogEngagementAsync(EngagementLog log, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(log.Id))
                log.Id = ObjectId.GenerateNewId().ToString();
            if (log.OccurredUtc == default)
                log.OccurredUtc = DateTime.UtcNow;

            await _context.EngagementLogs.InsertOneAsync(log, cancellationToken: ct);
        }
    }
}
