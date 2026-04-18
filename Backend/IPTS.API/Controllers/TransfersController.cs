using IPTS.Core.DTOs;
using IPTS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IPTS.API.Controllers
{
    // ══════════════════════════════════════════════════════════════════
    // TRANSFERS CONTROLLER
    // Handles Phase 1 & 2 flow:
    // Sending doctor: create broadcast → get matches → notify hospitals
    // Receiving doctor: see incoming → accept or decline
    // ══════════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransfersController : ControllerBase
    {
        private readonly IBroadcastService _broadcastService;

        public TransfersController(IBroadcastService broadcastService)
            => _broadcastService = broadcastService;

        private Guid CallerHospitalId =>
            Guid.Parse(User.FindFirstValue("hospitalId")!);

        private Guid CallerUserId =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ── SENDING DOCTOR ────────────────────────────────────────────

        /// <summary>
        /// Phase 1: Sending doctor creates anonymous broadcast.
        /// Zero patient data — only medical requirements.
        /// </summary>
        [HttpPost("broadcast")]
        [Authorize(Policy = "DoctorOnly")]
        public async Task<IActionResult> CreateBroadcast([FromBody] CreateBroadcastRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var broadcast = await _broadcastService.CreateAsync(
                request, CallerHospitalId, CallerUserId);

            return CreatedAtAction(nameof(GetById), new { id = broadcast.Id }, broadcast);
        }

        /// <summary>
        /// Phase 2: Get ranked list of matching hospitals.
        /// Called right after creating a broadcast.
        /// </summary>
        [HttpGet("broadcast/{id:guid}/matches")]
        [Authorize(Policy = "DoctorOnly")]
        public async Task<IActionResult> GetMatches(Guid id)
        {
            var matches = await _broadcastService.GetMatchesAsync(id, CallerHospitalId);
            return Ok(matches);
        }

        /// <summary>
        /// Sending doctor selects hospitals from the match list and notifies them all at once.
        /// </summary>
        [HttpPost("broadcast/notify")]
        [Authorize(Policy = "DoctorOnly")]
        public async Task<IActionResult> NotifyHospitals([FromBody] NotifyHospitalsRequest request)
        {
            await _broadcastService.NotifyHospitalsAsync(request.BroadcastId, request.HospitalIds);
            return Ok(new { message = $"{request.HospitalIds.Count} hospital(s) notified." });
        }

        /// <summary>
        /// Get full broadcast with all hospital responses.
        /// Used by sending doctor to watch responses in the waiting room.
        /// </summary>
        [HttpGet("broadcast/{id:guid}")]
        [Authorize(Policy = "DoctorOnly")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var broadcast = await _broadcastService.GetByIdAsync(id);
            if (broadcast is null)
                return NotFound(new { message = "Broadcast not found." });

            if (broadcast.SendingHospitalId != CallerHospitalId)
                return Forbid();

            return Ok(broadcast);
        }

        /// <summary>
        /// All broadcasts created by the caller's hospital.
        /// </summary>
        [HttpGet("my-broadcasts")]
        [Authorize(Policy = "DoctorOnly")]
        public async Task<IActionResult> GetMyBroadcasts()
        {
            var broadcasts = await _broadcastService.GetMyBroadcastsAsync(CallerHospitalId);
            return Ok(broadcasts);
        }

        // ── RECEIVING DOCTOR ──────────────────────────────────────────

        /// <summary>
        /// Receiving hospital sees all pending anonymous requests.
        /// Shows ZERO patient data — only medical requirements.
        /// </summary>
        [HttpGet("incoming")]
        [Authorize(Policy = "DoctorOnly")]
        public async Task<IActionResult> GetIncoming()
        {
            var incoming = await _broadcastService.GetIncomingRequestsAsync(CallerHospitalId);
            return Ok(incoming);
        }

        /// <summary>
        /// Receiving doctor accepts or declines the anonymous request.
        /// On accept → patient data reveal happens in Week 4 (Phase 3).
        /// </summary>
        [HttpPost("broadcast/{broadcastId:guid}/respond")]
        [Authorize(Policy = "DoctorOnly")]
        public async Task<IActionResult> Respond(
            Guid broadcastId,
            [FromBody] RespondToBroadcastRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var response = await _broadcastService.RespondAsync(
                broadcastId, CallerHospitalId, CallerUserId, request);

            if (response is null)
                return NotFound(new { message = "Transfer request not found or not addressed to your hospital." });

            return Ok(response);
        }
    }
}
