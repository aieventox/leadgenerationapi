using System.Collections.Generic;

namespace LeadGeneration.Domain.Models
{
    /// <summary>
    /// Full company document (used when you need richer org data).
    /// </summary>
    public sealed class Company
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public double? AnnualRevenueUsd { get; set; }
        public string HqLocation { get; set; } = string.Empty;
        public List<string> TechStack { get; set; } = new();
        public string LinkedinUrl { get; set; } = string.Empty;

        // optional enrichment blobs per provider
        public Dictionary<string, object> ProviderPayloads { get; set; } = new();
    }
}
