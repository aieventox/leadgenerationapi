using System;
using System.Collections.Generic;

namespace LeadGeneration.Application.DTO
{
    /// <summary>
    /// Unified lead view used by API responses and services.
    /// Combines person + (lite) company info + contact channels.
    /// </summary>
    public sealed class LeadDto
    {
        public string LeadId { get; init; }                // internal DB id or provider id
        public PersonDto Person { get; init; } = new();
        public CompanyLiteDto Company { get; init; } = new();
        public ContactDto Contact { get; init; } = new();

        public string Source { get; init; }                // e.g., "Apollo", "DB"
        public DateTime? FirstSeenUtc { get; init; }
        public DateTime? LastUpdatedUtc { get; init; }
        public bool IsEnriched { get; init; }              // indicates provider enrichment performed
        public Dictionary<string, string>? ProviderRefs { get; init; } // providerName -> externalId
    }

    /// <summary>
    /// Person core profile.
    /// </summary>
    public sealed class PersonDto
    {
        public string FirstName { get; init; } = "";
        public string LastName { get; init; } = "";
        public string FullName => string.IsNullOrWhiteSpace(FirstName)
            ? LastName
            : $"{FirstName} {LastName}".Trim();

        public string Title { get; init; } = "";          // e.g., Senior Data Engineer
        public string Department { get; init; } = "";     // e.g., Engineering, Marketing
        public string Seniority { get; init; } = "";      // e.g., Manager, Director, C-Level
        public string LinkedInUrl { get; init; } = "";
        public string Location { get; init; } = "";       // city, state, country (free-form)
        public List<string> Skills { get; init; } = new(); // optional tags/tech stack
    }

    /// <summary>
    /// Contact channels for a person.
    /// </summary>
    public sealed class ContactDto
    {
        public string WorkEmail { get; init; } = "";
        public string PersonalEmail { get; init; } = "";
        public string DirectPhone { get; init; } = "";
        public string MobilePhone { get; init; } = "";
        public string CompanyPhone { get; init; } = "";
        public string TwitterUrl { get; init; } = "";
        public string GithubUrl { get; init; } = "";
        public bool EmailVerified { get; init; }
    }

    /// <summary>
    /// Lightweight company view for lead listing/search.
    /// </summary>
    public sealed class CompanyLiteDto
    {
        public string CompanyId { get; init; } = "";      // internal DB id or provider id
        public string Name { get; init; } = "";
        public string Domain { get; init; } = "";
        public string Industry { get; init; } = "";
        public string Size { get; init; } = "";           // e.g., 51-200
        public double? AnnualRevenueUsd { get; init; }
        public string HqLocation { get; init; } = "";
        public List<string> TechStack { get; init; } = new();
        public string LinkedinUrl { get; init; } = "";
    }
}
