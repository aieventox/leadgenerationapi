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
    public sealed class ListsController : ControllerBase
    {
        private readonly ListService _service;

        public ListsController(ListService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok(new { ok = true, module = "Lists", utc = DateTime.UtcNow });

        /// <summary>Create a new prospect list.</summary>
        [HttpPost]
        public async Task<ActionResult<string>> Create([FromBody] ListDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Name is required.");

            var ct = HttpContext?.RequestAborted ?? CancellationToken.None;
            var id = await _service.CreateAsync(dto.Name, dto.Description, ct);
            return Ok(id);
        }

        /// <summary>Get lists with paging.</summary>
        [HttpGet]
        public async Task<ActionResult<object>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 25)
        {
            var ct = HttpContext?.RequestAborted ?? CancellationToken.None;
            var (items, p, ps, total) = await _service.GetPagedAsync(page, pageSize, ct);
            return Ok(new { items, page = p, pageSize = ps, total });
        }

        /// <summary>Get a list by id.</summary>
        [HttpGet("{listId}")]
        public async Task<ActionResult<ListDto>> GetById([FromRoute] string listId)
        {
            var ct = HttpContext?.RequestAborted ?? CancellationToken.None;
            var dto = await _service.GetByIdAsync(listId, ct);
            if (dto is null) return NotFound();
            return Ok(dto);
        }

        /// <summary>Add leads to a list.</summary>
        [HttpPost("add")]
        public async Task<IActionResult> AddLeads([FromBody] AddToListDto body)
        {
            if (body is null || string.IsNullOrWhiteSpace(body.ListId) || body.LeadIds.Count == 0)
                return BadRequest("ListId and LeadIds are required.");

            var ct = HttpContext?.RequestAborted ?? CancellationToken.None;
            await _service.AddLeadsAsync(body.ListId, body.LeadIds, ct);
            return Ok(new { ok = true });
        }

        /// <summary>Remove leads from a list.</summary>
        [HttpPost("remove")]
        public async Task<IActionResult> RemoveLeads([FromBody] RemoveFromListDto body)
        {
            if (body is null || string.IsNullOrWhiteSpace(body.ListId) || body.LeadIds.Count == 0)
                return BadRequest("ListId and LeadIds are required.");

            var ct = HttpContext?.RequestAborted ?? CancellationToken.None;
            await _service.RemoveLeadsAsync(body.ListId, body.LeadIds, ct);
            return Ok(new { ok = true });
        }
    }
}
