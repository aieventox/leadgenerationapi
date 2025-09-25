using System;
using System.Threading;
using System.Threading.Tasks;
using LeadGeneration.Application.DTO;
using LeadGeneration.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeadGeneration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class ImportsController : ControllerBase
    {
        private readonly ImportService _service;

        public ImportsController(ImportService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok(new { ok = true, module = "Imports", utc = DateTime.UtcNow });

        /// <summary>
        /// Import PEOPLE from the configured provider(s) and upsert into DB.
        /// Body = SearchRequestDto (e.g., keyword, location, page/pageSize).
        /// </summary>
        [HttpPost("people")]
        public async Task<ActionResult<object>> ImportPeople([FromBody] SearchRequestDto request)
        {
            if (request is null) return BadRequest("Request is required.");
            var ct = HttpContext?.RequestAborted ?? CancellationToken.None;
            var res = await _service.ImportPeopleAsync(request, ct);
            return Ok(res);
        }

        /// <summary>
        /// Import COMPANIES from the configured provider and upsert into DB.
        /// Body = SearchRequestDto (uses keyword/domain/location/page/pageSize).
        /// </summary>
        [HttpPost("companies")]
        public async Task<ActionResult<object>> ImportCompanies([FromBody] SearchRequestDto request)
        {
            if (request is null) return BadRequest("Request is required.");
            var ct = HttpContext?.RequestAborted ?? CancellationToken.None;
            var res = await _service.ImportCompaniesAsync(request, ct);
            return Ok(res);
        }
    }
}
