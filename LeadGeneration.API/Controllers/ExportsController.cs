using System;
using System.Threading;
using System.Threading.Tasks;
using LeadGeneration.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeadGeneration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class ExportsController : ControllerBase
    {
        private readonly ExportService _service;

        public ExportsController(ExportService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok(new { ok = true, module = "Exports", utc = DateTime.UtcNow });

        /// <summary>
        /// Export leads in batches. Default batchSize=10.
        /// GET /api/exports/leads?page=1&batchSize=10
        /// </summary>
        [HttpGet("leads")]
        public async Task<ActionResult<object>> ExportLeads([FromQuery] int page = 1, [FromQuery] int batchSize = 10)
        {
            var ct = HttpContext?.RequestAborted ?? CancellationToken.None;
            var res = await _service.GetLeadBatchAsync(page, batchSize, ct);
            var nextPage = (res.Page * res.PageSize) < res.Total ? res.Page + 1 : (int?)null;

            return Ok(new
            {
                items = res.Items,
                page = res.Page,
                batchSize = res.PageSize,
                total = res.Total,
                nextPage
            });
        }

        /// <summary>
        /// Export companies in batches. Default batchSize=10.
        /// GET /api/exports/companies?page=1&batchSize=10
        /// </summary>
        [HttpGet("companies")]
        public async Task<ActionResult<object>> ExportCompanies([FromQuery] int page = 1, [FromQuery] int batchSize = 10)
        {
            var ct = HttpContext?.RequestAborted ?? CancellationToken.None;
            var res = await _service.GetCompanyBatchAsync(page, batchSize, ct);
            var nextPage = (res.Page * res.PageSize) < res.Total ? res.Page + 1 : (int?)null;

            return Ok(new
            {
                items = res.Items,
                page = res.Page,
                batchSize = res.PageSize,
                total = res.Total,
                nextPage
            });
        }
    }
}
