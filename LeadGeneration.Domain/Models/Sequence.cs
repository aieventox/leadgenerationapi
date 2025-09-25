using System;
using System.Collections.Generic;

namespace LeadGeneration.Domain.Models
{
    public sealed class Sequence
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<SequenceStep> Steps { get; set; } = new();
        public DateTime CreatedUtc { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class SequenceStep
    {
        public int Order { get; set; }                 // 1..N
        public string Type { get; set; } = "email";    // email | call | task
        public int WaitHours { get; set; } = 48;
        public string Template { get; set; } = string.Empty;
    }
}
