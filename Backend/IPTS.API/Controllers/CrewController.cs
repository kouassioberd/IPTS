using IPTS.Core.DTOs;
using IPTS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IPTS.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class CrewController : ControllerBase
    {
        private readonly ICrewService _crewService;
        public CrewController(ICrewService crewService)
            => _crewService = crewService;

        private Guid CallerCrewId =>
            Guid.Parse(User.FindFirstValue("crewId")!);
        private Guid CallerAmbulanceId =>
            Guid.Parse(User.FindFirstValue("ambulanceId")!);

        /// <summary>
        /// Login endpoint for ambulance crew members.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(
            [FromBody] CrewLoginRequest request)
        {
            var result = await _crewService.LoginAsync(request);
            if (result is null)
                return Unauthorized(new { message = "Invalid credentials." });
            return Ok(result);
        }

        /// <summary>
        /// Returns the active transfer job assigned to the crew's ambulance
        /// </summary>
        [HttpGet("active-job")]
        [Authorize(Policy = "AmbulanceCrew")]
        public async Task<IActionResult> GetActiveJob()
        {
            var job = await _crewService
                .GetActiveJobAsync(CallerAmbulanceId);
            if (job is null)
                return NotFound(new
                {
                    message = "No active job assigned to your ambulance."
                });
            return Ok(job);
        }

        /// <summary>
        /// Submits vitals data for an active transfer job. The crew can submit
        /// multiple vitals records for the same transfer job.
        /// </summary>
        [HttpPost("vitals")]
        [Authorize(Policy = "AmbulanceCrew")]
        public async Task<IActionResult> SubmitVitals(
            [FromBody] SubmitVitalsRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _crewService
                .SubmitVitalsAsync(request, CallerCrewId);
            return Ok(result);
        }
    }

}
