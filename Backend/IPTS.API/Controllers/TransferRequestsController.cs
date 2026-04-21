using IPTS.Core.DTOs;
using IPTS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography.Xml;

namespace IPTS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "DoctorOnly")]
    public class TransferRequestsController : ControllerBase
    {
        private readonly ITransferService _transferService;
        public TransferRequestsController(ITransferService transferService)
            => _transferService = transferService;

        private Guid CallerHospitalId =>
            Guid.Parse(User.FindFirstValue("hospitalId")!);
        private Guid CallerUserId =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>
        /// Sending doctor enters full patient details after acceptance.
        /// Data is AES-256 encrypted before storage.
        /// </summary>
        [HttpPost("submit-patient-data")]
        public async Task<IActionResult> SubmitPatientData(
        [FromBody] SubmitPatientDataRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _transferService.SubmitPatientDataAsync(
                    request, CallerUserId, CallerHospitalId);
                return CreatedAtAction(nameof(GetById),
                    new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Receiving doctor retrieves decrypted patient data.
        /// Returns 403 if caller is not the receiving hospital.
        /// </summary>
        [HttpGet("{id:guid}/patient-data")]
        public async Task<IActionResult> GetPatientData(Guid id)
        {
            try
            {
                var data = await _transferService.GetPatientDataAsync(
                    id, CallerHospitalId, CallerUserId);
                if (data is null)
                    return NotFound(new
                    {
                        message = "Transfer request not found or patient data not yet submitted."
                    });
                return Ok(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Get transfer request status — both hospitals can call this.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _transferService.GetByIdAsync(id);
            if (result is null)
                return NotFound(new { message = "Transfer request not found." });
            return Ok(result);
        }

        /// <summary>
        /// Returns all transfers where caller's hospital is sending or receiving.
        /// </summary>
        [HttpGet("my-transfers")]
        public async Task<IActionResult> GetMyTransfers()
        {
            var list = await _transferService
                .GetByHospitalAsync(CallerHospitalId);
            return Ok(list);
        }

        /// <summary>
        /// Full timestamped audit trail — both hospitals can read this.
        /// </summary>
        [HttpGet("{id:guid}/audit-log")]
        public async Task<IActionResult> GetAuditLog(Guid id)
        {
            var logs = await _transferService
                .GetAuditLogAsync(id, CallerHospitalId);
            return Ok(logs);
        }
    }

}
