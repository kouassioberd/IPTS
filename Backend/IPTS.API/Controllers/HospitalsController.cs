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
    public class HospitalsController : ControllerBase
    {
        private readonly IHospitalService _hospitalService;
        public HospitalsController(IHospitalService hospitalService)
            => _hospitalService = hospitalService;

        private Guid CallerHospitalId =>
            Guid.Parse(User.FindFirstValue("hospitalId")!);

        /// <summary> All active hospitals with bed summary</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var hospitals = await _hospitalService.GetAllAsync();
            return Ok(hospitals);
        }

        /// <summary> Full hospital with wards and beds</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var hospital = await _hospitalService.GetByIdAsync(id);
            if (hospital is null)
                return NotFound(new { message = "Hospital not found." });

            return Ok(hospital);
        }

        /// <summary> Admin dashboard metrics</summary>
        [HttpGet("{id:guid}/dashboard")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetDashboard(Guid id)
        {
            if (CallerHospitalId != id)
                return Forbid();

            var dashboard = await _hospitalService.GetDashboardAsync(id);
            if (dashboard is null)
                return NotFound(new { message = "Hospital not found." });

            return Ok(dashboard);
        }

        /// <summary> Register a new hospital</summary>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create([FromBody] CreateHospitalRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var hospital = await _hospitalService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = hospital.Id }, hospital);
        }

        /// <summary> Update hospital info</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHospitalRequest request)
        {
            if (CallerHospitalId != id) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var hospital = await _hospitalService.UpdateAsync(id, request);
            if (hospital is null)
                return NotFound(new { message = "Hospital not found." });

            return Ok(hospital);
        }

        /// <summary> Soft deactivate</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            if (CallerHospitalId != id) return Forbid();

            var success = await _hospitalService.DeactivateAsync(id);
            if (!success)
                return NotFound(new { message = "Hospital not found." });

            return NoContent();
        }

        /// <summary>
        /// Admin-only: returns performance stats for all hospitals.
        /// Used by the Admin dashboard Reports tab.
        /// </summary>
        [HttpGet("performance-report")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetPerformanceReport()
        {
            var report = await _hospitalService.GetPerformanceReportAsync();
            return Ok(report);
        }

        /// <summary>
        /// Admin-only: returns week-by-week delivered transfer counts for one hospital.
        /// Used to render the weekly transfers bar chart.
        /// </summary>
        [HttpGet("{id:guid}/weekly-transfers")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetWeeklyTransfers(Guid id, [FromQuery] int weeks = 8)
        {
            var data = await _hospitalService.GetWeeklyTransfersAsync(id, weeks);
            return Ok(data);
        }
    }
}
