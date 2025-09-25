using System;

namespace LeadGeneration.Domain.Models
{
    public sealed class EngagementLog
    {
        public string Id { get; set; } = string.Empty;
        public string LeadId { get; set; } = string.Empty;
        public string Channel { get; set; } = "email"; // email | call | task
        public string Direction { get; set; } = "out"; // out | in
        public DateTime OccurredUtc { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string BodyPreview { get; set; } = string.Empty;
        public string Status { get; set; } = "sent";   // sent | opened | replied | failed
        public string? ProviderRef { get; set; }       // message id, call id, etc.
    }
}
