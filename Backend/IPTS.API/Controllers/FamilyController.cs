using IPTS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IPTS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FamilyController : ControllerBase
    {
        private readonly IFamilyService _familyService;

        public FamilyController(IFamilyService familyService)
            => _familyService = familyService;

        /// <summary>
        /// Public endpoint — no auth required.
        /// Returns ambulance GPS + transfer info for the family tracking page.
        /// </summary>
        [HttpGet("{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> Track(string token)
        {
            var result = await _familyService.GetTrackingDataAsync(token);
            if (result is null)
                return NotFound(new { message = "Invalid or expired tracking link." });
            return Ok(result);
        }
    }

}
