using IPTS.Core.DTOs;
using IPTS.Core.Entities;
using IPTS.Core.Enums;
using IPTS.Core.Interfaces;
using IPTS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IPTS.API.Services
{
    public class CrewService : ICrewService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public CrewService(AppDbContext db, IConfiguration config)
        { _db = db; _config = config; }

        // ── LOGIN ─────────────────────────────────────────────
        public async Task<CrewAuthResponse?> LoginAsync(CrewLoginRequest request)
        {
            var crew = await _db.AmbulanceCrews
                .Include(c => c.Ambulance)
                .FirstOrDefaultAsync(c =>
                    c.Email == request.Email && c.IsActive);

            if (crew is null) return null;
            if (!BCrypt.Net.BCrypt.Verify(request.Password, crew.PasswordHash))
                return null;

            var token = GenerateCrewJwt(crew);
            var expiryMinutes = int.Parse(
                _config["JwtSettings:AccessTokenExpiryMinutes"] ?? "60");

            return new CrewAuthResponse(
                AccessToken: token,
                FullName: crew.FullName,
                Role: crew.Role.ToString(),
                CrewId: crew.Id,
                AmbulanceId: crew.AmbulanceId,
                AmbulanceUnit: crew.Ambulance?.UnitNumber ?? "",
                ExpiresAt: DateTime.UtcNow.AddMinutes(expiryMinutes)
            );
        }

        // ── ACTIVE JOB ────────────────────────────────────────
        public async Task<CrewActiveJobDto?> GetActiveJobAsync(Guid ambulanceId)
        {
            var transfer = await _db.TransferRequests
                .Where(t => t.AssignedAmbulanceId == ambulanceId &&
                            t.Status != TransferStatus.Delivered &&
                            t.Status != TransferStatus.Cancelled)
                .Include(t => t.SendingHospital)
                .Include(t => t.ReceivingHospital)
                .Include(t => t.AssignedAmbulance)
                .Include(t => t.Vitals)
                .OrderByDescending(t => t.ConfirmedAt)
                .FirstOrDefaultAsync();

            if (transfer is null) return null;

            return new CrewActiveJobDto(
                TransferRequestId: transfer.Id,
                SendingHospitalName: transfer.SendingHospital?.Name ?? "",
                ReceivingHospitalName: transfer.ReceivingHospital?.Name ?? "",
                ReceivingHospitalAddress: transfer.ReceivingHospital?.Address ?? "",
                ReceivingHospitalLatitude: transfer.ReceivingHospital?.Latitude ?? 0,
                ReceivingHospitalLongitude: transfer.ReceivingHospital?.Longitude ?? 0,
                Status: transfer.Status,
                AmbulanceUnit: transfer.AssignedAmbulance?.UnitNumber ?? "",
                ConfirmedAt: transfer.ConfirmedAt,
                HasVitalsSubmitted: transfer.Vitals.Any()
            );
        }

        // ── SUBMIT VITALS ─────────────────────────────────────
        public async Task<VitalsResponseDto> SubmitVitalsAsync(
            SubmitVitalsRequest request, Guid crewId)
        {
            var vitals = new VitalsRecord
            {
                Id = Guid.NewGuid(),
                TransferRequestId = request.TransferRequestId,
                SubmittedByCrewId = crewId,
                BloodPressure = request.BloodPressure,
                HeartRate = request.HeartRate,
                OxygenSaturation = request.OxygenSaturation,
                GlasgowComaScale = request.GlasgowComaScale,
                Notes = request.Notes,
                RecordedAt = DateTime.UtcNow,
            };
            _db.VitalsRecords.Add(vitals);
            await _db.SaveChangesAsync();

            return new VitalsResponseDto(
                vitals.Id, vitals.TransferRequestId,
                vitals.BloodPressure, vitals.HeartRate,
                vitals.OxygenSaturation, vitals.GlasgowComaScale,
                vitals.Notes, vitals.RecordedAt
            );
        }

        // ── UPDATE LOCATION ──────────────────────────
        public async Task<LocationUpdateResponse> UpdateLocationAsync(
            UpdateLocationRequest request, Guid ambulanceId)
        {
            // Find the ambulance belonging to this crew member
            var ambulance = await _db.Ambulances
                .FirstOrDefaultAsync(a => a.Id == ambulanceId)
                ?? throw new InvalidOperationException(
                    "Ambulance not found for this crew member.");

            // Update the GPS coordinates
            ambulance.CurrentLatitude = request.Latitude;
            ambulance.CurrentLongitude = request.Longitude;
            ambulance.LastLocationUpdate = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new LocationUpdateResponse(
                AmbulanceId: ambulance.Id,
                Latitude: ambulance.CurrentLatitude,
                Longitude: ambulance.CurrentLongitude,
                UpdatedAt: ambulance.LastLocationUpdate
            );
        }

        // ── UPDATE JOB STATUS (Driver only) ───────────────────
        public async Task<CrewActiveJobDto> UpdateJobStatusAsync(
            UpdateJobStatusRequest request, Guid ambulanceId)
        {
            // Find the transfer assigned to this ambulance
            var transfer = await _db.TransferRequests
                .Include(t => t.SendingHospital)
                .Include(t => t.ReceivingHospital)
                .Include(t => t.AssignedAmbulance)
                .Include(t => t.TrackingToken)
                .Include(t => t.Vitals)
                .FirstOrDefaultAsync(t =>
                    t.Id == request.TransferRequestId &&
                    t.AssignedAmbulanceId == ambulanceId)
                ?? throw new InvalidOperationException(
                    "Transfer not found or not assigned to your ambulance.");

            // Enforce valid transitions — driver cannot skip steps
            var allowed = new Dictionary<TransferStatus, List<TransferStatus>>
            {
                [TransferStatus.AmbulanceAssigned] = [TransferStatus.EnRoute],
                [TransferStatus.EnRoute] = [TransferStatus.PatientOnBoard],
                [TransferStatus.PatientOnBoard] = [TransferStatus.InTransit],
                [TransferStatus.InTransit] = [TransferStatus.Delivered],
            };

            if (!allowed.TryGetValue(transfer.Status, out var nextStatuses) ||
                !nextStatuses.Contains(request.NewStatus))
                throw new InvalidOperationException(
                    $"Cannot transition from {transfer.Status} to {request.NewStatus}.");

            // Apply the new status
            transfer.Status = request.NewStatus;

            // On Delivered: record delivery time, shorten family tracking access, and free the ambulance.
            if (request.NewStatus == TransferStatus.Delivered)
            {
                var deliveredAt = DateTime.UtcNow;
                transfer.DeliveredAt = deliveredAt;

                if (transfer.TrackingToken is not null)
                {
                    var deliveryExpiry = deliveredAt.AddMinutes(10);
                    if (deliveryExpiry < transfer.TrackingToken.ExpiresAt)
                        transfer.TrackingToken.ExpiresAt = deliveryExpiry;
                }

                if (transfer.AssignedAmbulance != null)
                    transfer.AssignedAmbulance.Status = AmbulanceStatus.Available;
            }

            // Write audit log
            _db.AuditLogs.Add(new TransferAuditLog
            {
                Id = Guid.NewGuid(),
                TransferRequestId = transfer.Id,
                Action = $"StatusUpdated:{request.NewStatus}",
                PerformedByUserId = ambulanceId,
                PerformedByRole = "Driver",
                Timestamp = DateTime.UtcNow,
                Details = $"Driver updated transfer status to {request.NewStatus}",
            });

            await _db.SaveChangesAsync();

            // Return updated job so the Android app reflects the new status instantly
            return new CrewActiveJobDto(
                TransferRequestId: transfer.Id,
                SendingHospitalName: transfer.SendingHospital?.Name ?? "",
                ReceivingHospitalName: transfer.ReceivingHospital?.Name ?? "",
                ReceivingHospitalAddress: transfer.ReceivingHospital?.Address ?? "",
                ReceivingHospitalLatitude: transfer.ReceivingHospital?.Latitude ?? 0,
                ReceivingHospitalLongitude: transfer.ReceivingHospital?.Longitude ?? 0,
                Status: transfer.Status,
                AmbulanceUnit: transfer.AssignedAmbulance?.UnitNumber ?? "",
                ConfirmedAt: transfer.ConfirmedAt,
                HasVitalsSubmitted: transfer.Vitals.Any()
            );
        }

        // ── JWT HELPER ────────────────────────────────────────
        private string GenerateCrewJwt(AmbulanceCrew crew)
        {
            var jwt = _config.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["SecretKey"]!));
            var creds = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddMinutes(
                int.Parse(jwt["AccessTokenExpiryMinutes"] ?? "60"));

            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, crew.Id.ToString()),
            new Claim(ClaimTypes.Email,          crew.Email),
            new Claim(ClaimTypes.Name,           crew.FullName),
            new Claim(ClaimTypes.Role,           crew.Role.ToString()),
            new Claim("ambulanceId",             crew.AmbulanceId.ToString()),
            new Claim("crewId",                  crew.Id.ToString()),
        };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

}
