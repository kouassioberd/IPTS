using IPTS.Core.DTOs;
using IPTS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace IPTS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "DispatcherOnly")]
    public class DispatcherController : ControllerBase
    {
        private readonly IDispatcherService _dispatcherService;
        public DispatcherController(IDispatcherService dispatcherService)
            => _dispatcherService = dispatcherService;

        private Guid CallerHospitalId =>
            Guid.Parse(User.FindFirstValue("hospitalId")!);
        private Guid CallerUserId =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>
        /// Dispatcher sees summary counts + all active transfers.
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var dashboard = await _dispatcherService
                .GetDashboardAsync(CallerHospitalId);
            return Ok(dashboard);
        }

        /// <summary>
        /// Returns only Available ambulances for the caller's hospital.
        /// Used to populate the assignment dropdown.
        /// </summary>
        [HttpGet("ambulances/available")]
        public async Task<IActionResult> GetAvailableAmbulances()
        {
            var ambulances = await _dispatcherService
                .GetAvailableAmbulancesAsync(CallerHospitalId);
            return Ok(ambulances);
        }

        /// <summary>
        /// Dispatcher assigns an available ambulance.
        /// Transfer must be Confirmed. Ambulance must be Available.
        /// </summary>
        [HttpPost("assign")]
        public async Task<IActionResult> AssignAmbulance(
        [FromBody] AssignAmbulanceRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _dispatcherService.AssignAmbulanceAsync(
                    request, CallerUserId, CallerHospitalId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Progress the transfer: EnRoute → PatientOnBoard → InTransit → Delivered.
        /// On Delivered: ambulance freed, performance stats updated.
        /// </summary>
        [HttpPatch("status")]
        public async Task<IActionResult> UpdateStatus(
        [FromBody] UpdateTransferStatusRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _dispatcherService.UpdateStatusAsync(
                    request, CallerUserId, CallerHospitalId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get a single transfer with full ambulance detail.
        /// </summary>
        [HttpGet("transfers/{id:guid}")]
        public async Task<IActionResult> GetTransfer(Guid id)
        {
            var result = await _dispatcherService
                .GetTransferByIdAsync(id, CallerHospitalId);
            if (result is null)
                return NotFound(new { message = "Transfer not found." });
            return Ok(result);
        }
    }
}
