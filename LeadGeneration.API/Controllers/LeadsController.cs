using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeadGeneration.Application.DTO;
using LeadGeneration.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeadGeneration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class LeadsController : ControllerBase
    {
        private readonly LeadService _leadService;

        public LeadsController(LeadService leadService)
        {
            _leadService = leadService ?? throw new ArgumentNullException(nameof(leadService));
        }

        /// <summary>
        /// Quick health check for the Leads module.
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health() => Ok(new { ok = true, module = "Leads", utc = DateTime.UtcNow });

        /// <summary>
        /// Search leads via query string. DB-first, then provider fallback (unless ForceProvider=true).
        /// </summary>
        /// <remarks>
        /// Example:
        /// GET /api/leads/search?keyword=data%20engineer&location=NY&page=1&pageSize=25
        /// </remarks>
        [HttpGet("search")]
        public async Task<ActionResult<SearchResultDto>> SearchGet(
            [FromQuery] string? keyword,
            [FromQuery] string? title,
            [FromQuery] string? department,
            [FromQuery] string? seniority,
            [FromQuery] string? companyName,
            [FromQuery] string? companyDomain,
            [FromQuery] string? location,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] bool forceProvider = false)
        {
            var req = new SearchRequestDto
            {
                Keyword = keyword ?? string.Empty,
                Title = title ?? string.Empty,
                Department = department ?? string.Empty,
                Seniority = seniority ?? string.Empty,
                CompanyName = companyName ?? string.Empty,
                CompanyDomain = companyDomain ?? string.Empty,
                Location = location ?? string.Empty,
                Page = page,
                PageSize = pageSize,
                ForceProvider = forceProvider
            };

            var ct = HttpContext?.RequestAborted ?? CancellationToken.None;
            var result = await _leadService.SearchAsync(req, ct);
            return Ok(result);
        }

        /// <summary>
        /// Search leads via POST body (useful for longer filters like TechIncludes).
        /// </summary>
        [HttpPost("search")]
        public async Task<ActionResult<SearchResultDto>> SearchPost([FromBody] SearchRequestDto request)
        {
            if (request is null) return BadRequest("Request body is required.");
            var ct = HttpContext?.RequestAborted ?? CancellationToken.None;
            var result = await _leadService.SearchAsync(request, ct);
            return Ok(result);
        }

        /// <summary>
        /// Get a single lead by id.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LeadDto>> GetById([FromRoute] string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("id is required.");
            var ct = HttpContext?.RequestAborted ?? CancellationToken.None;
            var dto = await _leadService.GetByIdAsync(id, ct);
            if (dto is null) return NotFound();
            return Ok(dto);
        }

        /// <summary>
        /// Upsert one or more leads. Accepts unified LeadDto (person + company + contact).
        /// </summary>
        [HttpPost("upsert")]
        public async Task<ActionResult<IReadOnlyList<string>>> Upsert([FromBody] List<LeadDto> leads)
        {
            if (leads is null || leads.Count == 0) return BadRequest("At least one lead is required.");

            var ct = HttpContext?.RequestAborted ?? CancellationToken.None;
            var ids = await _leadService.UpsertAsync(leads, ct);
            return Ok(ids);
        }
    }
}
