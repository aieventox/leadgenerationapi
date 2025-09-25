using System;
using System.Collections.Generic;

namespace LeadGeneration.Application.DTO
{
    public sealed class SequenceDto
    {
        public string SequenceId { get; init; } = "";
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public List<SequenceStepDto> Steps { get; init; } = new();
        public DateTime CreatedUtc { get; init; }
        public bool IsActive { get; init; }
    }

    public sealed class SequenceStepDto
    {
        public int Order { get; init; }              // 1..N
        public string Type { get; init; } = "email"; // email | call | task
        public int WaitHours { get; init; } = 48;    // delay after previous step
        public string Template { get; init; } = "";  // email or call script
    }

    public sealed class StartSequenceDto
    {
        public string SequenceId { get; init; } = "";
        public List<string> LeadIds { get; init; } = new();
    }
}
