using System;
using System.Threading;
using System.Threading.Tasks;
using LeadGeneration.Domain.Models;
using MongoDB.Driver;

namespace LeadGeneration.Infrastructure
{
    /// <summary>
    /// Minimal Mongo context with strongly-typed collections and sensible indexes.
    /// Call EnsureIndexesAsync() once on startup (e.g., in Program.cs).
    /// </summary>
    public sealed class MongoDbContext
    {
        public IMongoDatabase Database { get; }
        public IMongoCollection<Lead> Leads { get; }
        public IMongoCollection<Company> Companies { get; }
        public IMongoCollection<ProspectList> ProspectLists { get; }
        public IMongoCollection<Sequence> Sequences { get; }
        public IMongoCollection<EngagementLog> EngagementLogs { get; }

        public MongoDbContext(string connectionString, string databaseName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Mongo connection string is required.", nameof(connectionString));
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Mongo database name is required.", nameof(databaseName));

            var client = new MongoClient(connectionString);
            Database = client.GetDatabase(databaseName);

            Leads = Database.GetCollection<Lead>("leads");
            Companies = Database.GetCollection<Company>("companies");
            ProspectLists = Database.GetCollection<ProspectList>("prospect_lists");
            Sequences = Database.GetCollection<Sequence>("sequences");
            EngagementLogs = Database.GetCollection<EngagementLog>("engagement_logs");
        }

        public async Task EnsureIndexesAsync(CancellationToken ct = default)
        {
            // Leads
            var leadKeys = Builders<Lead>.IndexKeys;
            await Leads.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<Lead>(leadKeys.Ascending(x => x.Person.LastName).Ascending(x => x.Person.FirstName)),
                new CreateIndexModel<Lead>(leadKeys.Text(x => x.Person.FirstName)
                                           .Text(x => x.Person.LastName)
                                           .Text(x => x.Person.Title)
                                           .Text(x => x.Company.Name)),
                new CreateIndexModel<Lead>(leadKeys.Ascending(x => x.Company.Domain)),
                new CreateIndexModel<Lead>(leadKeys.Ascending(x => x.Contact.WorkEmail)),
                new CreateIndexModel<Lead>(leadKeys.Ascending(x => x.LastUpdatedUtc)),
            }, ct);

            // Companies
            var companyKeys = Builders<Company>.IndexKeys;
            await Companies.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<Company>(companyKeys.Ascending(c => c.Domain), new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<Company>(companyKeys.Text(c => c.Name)),
            }, ct);

            // Lists
            var listKeys = Builders<ProspectList>.IndexKeys;
            await ProspectLists.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<ProspectList>(listKeys.Ascending(l => l.Name)),
                new CreateIndexModel<ProspectList>(listKeys.Ascending(l => l.CreatedUtc)),
            }, ct);

            // Sequences
            var seqKeys = Builders<Sequence>.IndexKeys;
            await Sequences.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<Sequence>(seqKeys.Ascending(s => s.IsActive)),
                new CreateIndexModel<Sequence>(seqKeys.Ascending(s => s.CreatedUtc)),
            }, ct);

            // Engagement Logs
            var logKeys = Builders<EngagementLog>.IndexKeys;
            await EngagementLogs.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<EngagementLog>(logKeys.Ascending(l => l.LeadId)),
                new CreateIndexModel<EngagementLog>(logKeys.Ascending(l => l.OccurredUtc)),
                new CreateIndexModel<EngagementLog>(logKeys.Ascending(l => l.Channel)),
            }, ct);
        }
    }
}
