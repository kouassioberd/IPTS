using IPTS.Core.DTOs;
using IPTS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IPTS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BedsController : ControllerBase
    {
        private readonly IBedService _bedService;
        public BedsController(IBedService bedService)
            => _bedService = bedService;

        /// <summary> Add a single bed to a ward. Admin only.</summary>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateBed([FromBody] CreateBedRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var bed = await _bedService.CreateBedAsync(request);
            return Created($"/api/beds/{bed.Id}", bed);
        }

        /// <summary> Update bed status. Admin only.</summary>
        [HttpPatch("{id:guid}/status")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateBedStatusRequest request)
        {
            var bed = await _bedService.UpdateBedStatusAsync(id, request);
            if (bed is null)
                return NotFound(new { message = "Bed not found." });

            return Ok(bed);
        }

        /// <summary> Remove a bed. Admin only.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _bedService.DeleteBedAsync(id);
            if (!success)
                return NotFound(new { message = "Bed not found." });

            return NoContent();
        }
    }
}
