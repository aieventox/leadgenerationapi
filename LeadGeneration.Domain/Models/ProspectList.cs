using System;
using System.Collections.Generic;

namespace LeadGeneration.Domain.Models
{
    public sealed class ProspectList
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedUtc { get; set; }
        public List<string> LeadIds { get; set; } = new();
    }
}
