using System;
using System.Collections.Generic;

namespace LeadGeneration.Domain.Models
{
    /// <summary>
    /// Unified lead entity (person + lite company + contact channels).
    /// This is the core model used by repositories and services.
    /// </summary>
    public sealed class Lead
    {
        public string Id { get; set; } = string.Empty;           // DB id (e.g., Mongo _id as string)
        public Person Person { get; set; } = new();
        public CompanyLite Company { get; set; } = new();
        public ContactChannels Contact { get; set; } = new();

        public string Source { get; set; } = "DB";               // "DB", "Apollo", etc.
        public DateTime? FirstSeenUtc { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public bool IsEnriched { get; set; }
        public Dictionary<string, string> ProviderRefs { get; set; } = new(); // provider -> externalId
    }

    public sealed class Person
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Seniority { get; set; } = string.Empty;
        public string LinkedInUrl { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public List<string> Skills { get; set; } = new();
    }

    public sealed class ContactChannels
    {
        public string WorkEmail { get; set; } = string.Empty;
        public string PersonalEmail { get; set; } = string.Empty;
        public string DirectPhone { get; set; } = string.Empty;
        public string MobilePhone { get; set; } = string.Empty;
        public string CompanyPhone { get; set; } = string.Empty;
        public string TwitterUrl { get; set; } = string.Empty;
        public string GithubUrl { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
    }

    public sealed class CompanyLite
    {
        public string CompanyId { get; set; } = string.Empty;    // optional ref to Company
        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;         // e.g., "51-200"
        public double? AnnualRevenueUsd { get; set; }
        public string HqLocation { get; set; } = string.Empty;
        public List<string> TechStack { get; set; } = new();
        public string LinkedinUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Domain-level search criteria to avoid DTO dependency in repositories/providers.
    /// </summary>
    public sealed class LeadSearchCriteria
    {
        public string Keyword { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Seniority { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyDomain { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty; // city/state/country
        public List<string> TechIncludes { get; set; } = new();

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;

        /// <summary>
        /// When true, services may choose to bypass local DB cache and call external providers.
        /// Repository implementations may ignore this.
        /// </summary>
        public bool ForceProvider { get; set; } = false;
    }
}
