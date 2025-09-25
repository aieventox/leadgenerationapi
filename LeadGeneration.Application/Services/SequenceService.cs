using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LeadGeneration.Application.DTO;
using LeadGeneration.Domain.Interfaces;
using LeadGeneration.Domain.Models;

namespace LeadGeneration.Application.Services
{
    /// <summary>
    /// Handles creating sequences, listing/fetching them, and kicking off the first step
    /// by logging a task/email/call engagement for each target lead.
    /// </summary>
    public sealed class SequenceService
    {
        private readonly ILeadRepository _repo;

        public SequenceService(ILeadRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        // -------- CRUD-ish --------

        public async Task<string> CreateAsync(SequenceDto dto, CancellationToken ct = default)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Sequence name is required.", nameof(dto));

            var model = MapToModel(dto);
            return await _repo.CreateSequenceAsync(model, ct);
        }

        public async Task<SequenceDto?> GetAsync(string sequenceId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sequenceId)) return null;

            var s = await _repo.GetSequenceAsync(sequenceId, ct);
            return s is null ? null : MapToDto(s);
        }

        public async Task<(IReadOnlyList<SequenceDto> Items, int Page, int PageSize, long Total)>
            GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 25 : pageSize;

            var res = await _repo.GetSequencesAsync(page, pageSize, ct);
            var items = res.Items.Select(MapToDto).ToList();
            return (items, res.Page, res.PageSize, res.Total);
        }

        // -------- Start (first step fan-out) --------

        public async Task<object> StartAsync(StartSequenceDto body, CancellationToken ct = default)
        {
            if (body is null) throw new ArgumentNullException(nameof(body));
            if (string.IsNullOrWhiteSpace(body.SequenceId))
                throw new ArgumentException("SequenceId is required.", nameof(body));
            if (body.LeadIds == null || body.LeadIds.Count == 0)
                throw new ArgumentException("At least one lead is required.", nameof(body));

            var seq = await _repo.GetSequenceAsync(body.SequenceId, ct);
            if (seq is null) throw new InvalidOperationException("Sequence not found.");

            var first = seq.Steps.OrderBy(s => s.Order).FirstOrDefault();
            if (first is null) throw new InvalidOperationException("Sequence has no steps.");

            var when = DateTime.UtcNow; // first step now; future steps handled by scheduler in your app

            var logs = new List<EngagementLog>(body.LeadIds.Count);
            foreach (var leadId in body.LeadIds)
            {
                logs.Add(new EngagementLog
                {
                    LeadId = leadId,
                    Channel = NormalizeStepChannel(first.Type), // email | call | task
                    Direction = "out",
                    OccurredUtc = when,
                    Subject = $"Seq:{seq.Name} Step:{first.Order}",
                    BodyPreview = Truncate(first.Template, 180),
                    Status = "sent",  // or "scheduled" if you prefer
                    ProviderRef = seq.Id
                });
            }

            // write logs
            foreach (var log in logs)
                await _repo.LogEngagementAsync(log, ct);

            return new
            {
                ok = true,
                sequenceId = seq.Id,
                step = first.Order,
                count = logs.Count,
                scheduledUtc = when
            };
        }

        // -------- Mapping --------

        private static Sequence MapToModel(SequenceDto d) =>
            new()
            {
                Id = d.SequenceId ?? string.Empty,
                Name = d.Name ?? string.Empty,
                Description = d.Description ?? string.Empty,
                Steps = (d.Steps ?? new List<SequenceStepDto>())
                    .OrderBy(s => s.Order)
                    .Select(s => new SequenceStep
                    {
                        Order = s.Order,
                        Type = string.IsNullOrWhiteSpace(s.Type) ? "email" : s.Type,
                        WaitHours = s.WaitHours <= 0 ? 48 : s.WaitHours,
                        Template = s.Template ?? string.Empty
                    })
                    .ToList(),
                CreatedUtc = d.CreatedUtc == default ? DateTime.UtcNow : d.CreatedUtc,
                IsActive = d.IsActive
            };

        private static SequenceDto MapToDto(Sequence m) =>
            new()
            {
                SequenceId = m.Id,
                Name = m.Name,
                Description = m.Description ?? "",
                CreatedUtc = m.CreatedUtc,
                IsActive = m.IsActive,
                Steps = (m.Steps ?? new List<SequenceStep>())
                    .OrderBy(s => s.Order)
                    .Select(s => new SequenceStepDto
                    {
                        Order = s.Order,
                        Type = s.Type,
                        WaitHours = s.WaitHours,
                        Template = s.Template
                    })
                    .ToList()
            };

        private static string NormalizeStepChannel(string? type)
        {
            var t = (type ?? "email").Trim().ToLowerInvariant();
            return t is "email" or "call" or "task" ? t : "email";
        }

        private static string Truncate(string? s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s[..max] + "…";
        }
    }
}
