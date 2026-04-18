using IPTS.Core.DTOs;
using IPTS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IPTS.API.Controllers
{
    // ══════════════════════════════════════════════════════════════════
    // MATCHING CONTROLLER
    // Live preview of matching results — no broadcast created.
    // Used by the sending doctor as they fill the form.
    // ══════════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "DoctorOnly")]
    public class MatchingController : ControllerBase
    {
        private readonly IMatchingEngine _matchingEngine;

        public MatchingController(IMatchingEngine matchingEngine)
            => _matchingEngine = matchingEngine;

        private Guid CallerHospitalId =>
            Guid.Parse(User.FindFirstValue("hospitalId")!);

        /// <summary>
        /// Returns matching hospitals without creating a broadcast.
        /// Used for live preview as the doctor fills the form.
        /// </summary>
        [HttpPost("preview")]
        public async Task<IActionResult> Preview([FromBody] CreateBroadcastRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _matchingEngine.FindMatchesAsync(request, CallerHospitalId);
            return Ok(result);
        }
    }
}
