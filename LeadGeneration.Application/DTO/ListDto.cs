using System;
using System.Collections.Generic;

namespace LeadGeneration.Application.DTO
{
    public sealed class ListDto
    {
        public string ListId { get; init; } = "";
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public DateTime CreatedUtc { get; init; }
        public int LeadCount { get; init; }
    }

    public sealed class AddToListDto
    {
        public string ListId { get; init; } = "";
        public List<string> LeadIds { get; init; } = new();
    }

    public sealed class RemoveFromListDto
    {
        public string ListId { get; init; } = "";
        public List<string> LeadIds { get; init; } = new();
    }
}
