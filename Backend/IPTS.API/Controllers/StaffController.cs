using IPTS.Core.DTOs;
using IPTS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IPTS.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class StaffController : ControllerBase
    {
        private readonly IStaffService _staffService;
        public StaffController(IStaffService staffService)
            => _staffService = staffService;

        private Guid CallerHospitalId =>
            Guid.Parse(User.FindFirstValue("hospitalId")!);

        private Guid CallerUserId =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary> All staff in caller's hospital</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var staff = await _staffService.GetAllByHospitalAsync(CallerHospitalId);
            return Ok(staff);
        }

        /// <summary> Single staff member</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var staff = await _staffService.GetByIdAsync(id);
            if (staff is null)
                return NotFound(new { message = "Staff member not found." });

            if (staff.HospitalId != CallerHospitalId) return Forbid();

            return Ok(staff);
        }

        /// <summary> Create staff account. HospitalId from JWT always.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStaffRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (success, errors) = await _staffService.CreateAsync(request, CallerHospitalId);
            if (!success)
                return BadRequest(new { message = "Failed to create staff member.", errors });

            return Ok(new { message = $"{request.FullName} created successfully as {request.Role}." });
        }

        /// <summary> Update name, email, role</summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStaffRequest request)
        {
            var existing = await _staffService.GetByIdAsync(id);
            if (existing is null)
                return NotFound(new { message = "Staff member not found." });

            if (existing.HospitalId != CallerHospitalId) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _staffService.UpdateAsync(id, request);
            return Ok(updated);
        }

        /// <summary> Disable login</summary>
        [HttpPatch("{id:guid}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            if (id == CallerUserId)
                return BadRequest(new { message = "You cannot deactivate your own account." });

            var existing = await _staffService.GetByIdAsync(id);
            if (existing is null)
                return NotFound(new { message = "Staff member not found." });

            if (existing.HospitalId != CallerHospitalId) return Forbid();

            await _staffService.DeactivateAsync(id);
            return Ok(new { message = "Staff member deactivated." });
        }

        /// <summary> Re-enable login</summary>
        [HttpPatch("{id:guid}/reactivate")]
        public async Task<IActionResult> Reactivate(Guid id)
        {
            var existing = await _staffService.GetByIdAsync(id);
            if (existing is null)
                return NotFound(new { message = "Staff member not found." });

            if (existing.HospitalId != CallerHospitalId) return Forbid();

            await _staffService.ReactivateAsync(id);
            return Ok(new { message = "Staff member reactivated." });
        }

        /// <summary> Own account only, any role</summary>
        [HttpPatch("{id:guid}/change-password")]
        [Authorize]   // override class-level — any authenticated role
        public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request)
        {
            if (id != CallerUserId)
                return Forbid();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var success = await _staffService.ChangePasswordAsync(id, request);
            if (!success)
                return BadRequest(new { message = "Current password is incorrect." });

            return Ok(new { message = "Password changed successfully." });
        }
    }
}
