using IPTS.Core.DTOs;
using IPTS.Core.Interfaces;
using IPTS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IPTS.API.Services
{
    public class FamilyService : IFamilyService
    {
        private readonly AppDbContext _db;

        public FamilyService(AppDbContext db) => _db = db;

        public async Task<FamilyTrackingDto?> GetTrackingDataAsync(string token)
        {
            // 1. Find the token row
            var tracking = await _db.FamilyTrackingTokens
                .Include(t => t.TransferRequest)
                    .ThenInclude(tr => tr.SendingHospital)
                .Include(t => t.TransferRequest)
                    .ThenInclude(tr => tr.ReceivingHospital)
                .Include(t => t.TransferRequest)
                    .ThenInclude(tr => tr.AssignedAmbulance)
                .FirstOrDefaultAsync(t => t.Token == token);

            // 2. Not found or expired
            if (tracking is null) return null;
            if (!tracking.IsActive) return null;

            var transfer = tracking.TransferRequest;
            var ambulance = transfer.AssignedAmbulance;
            var receiving = transfer.ReceivingHospital;
            var sending = transfer.SendingHospital;

            // 3. Build and return DTO
            return new FamilyTrackingDto(
                PatientStatus: transfer.Status.ToString(),
                SendingHospitalName: sending?.Name ?? "Unknown",
                ReceivingHospitalName: receiving?.Name ?? "Unknown",
                ReceivingHospitalAddress: receiving?.Address ?? "",
                AmbulanceLatitude: ambulance?.CurrentLatitude ?? 0,
                AmbulanceLongitude: ambulance?.CurrentLongitude ?? 0,
                LastLocationUpdate: ambulance?.LastLocationUpdate ?? DateTime.UtcNow,
                AmbulanceUnit: ambulance?.UnitNumber ?? "",
                IsExpired: !tracking.IsActive
            );
        }
    }

}
