using IPTS.Core.DTOs;
using IPTS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IPTS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WardsController : ControllerBase
    {
        private readonly IWardService _wardService;
        public WardsController(IWardService wardService)
            => _wardService = wardService;

        private Guid CallerHospitalId =>
            Guid.Parse(User.FindFirstValue("hospitalId")!);

        /// <summary> All wards with bed detail</summary>
        [HttpGet("hospital/{hospitalId:guid}")]
        public async Task<IActionResult> GetByHospital(Guid hospitalId)
        {
            var wards = await _wardService.GetByHospitalAsync(hospitalId);
            return Ok(wards);
        }

        /// <summary> Single ward with beds</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var ward = await _wardService.GetByIdAsync(id);
            if (ward is null)
                return NotFound(new { message = "Ward not found." });

            return Ok(ward);
        }

        /// <summary> Create ward + auto-generate beds. Admin only.</summary>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create([FromBody] CreateWardRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var ward = await _wardService.CreateAsync(CallerHospitalId, request);
            return CreatedAtAction(nameof(GetById), new { id = ward.Id }, ward);
        }

        /// <summary> Update ward name and type. Admin only.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWardRequest request)
        {
            var existing = await _wardService.GetByIdAsync(id);
            if (existing is null)
                return NotFound(new { message = "Ward not found." });

            if (existing.HospitalId != CallerHospitalId) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var ward = await _wardService.UpdateAsync(id, request);
            return Ok(ward);
        }

        /// <summary> Delete ward (blocked if beds are busy). Admin only.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existing = await _wardService.GetByIdAsync(id);
            if (existing is null)
                return NotFound(new { message = "Ward not found." });

            if (existing.HospitalId != CallerHospitalId) return Forbid();

            try
            {
                await _wardService.DeleteAsync(id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
