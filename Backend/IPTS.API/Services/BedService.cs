using IPTS.Core.DTOs;
using IPTS.Core.Entities;
using IPTS.Core.Enums;
using IPTS.Core.Interfaces;
using IPTS.Infrastructure.Data;

namespace IPTS.API.Services
{
    public class BedService : IBedService
    {
        private readonly AppDbContext _db;
        public BedService(AppDbContext db) => _db = db;

        public async Task<BedSummaryDto> CreateBedAsync(CreateBedRequest request)
        {
            var bed = new Bed
            {
                Id = Guid.NewGuid(),
                WardId = request.WardId,
                BedNumber = request.BedNumber,
                Status = BedStatus.Available
            };

            _db.Beds.Add(bed);
            await _db.SaveChangesAsync();
            return new BedSummaryDto(bed.Id, bed.BedNumber, bed.Status, bed.LastUpdated);
        }

        public async Task<BedSummaryDto?> UpdateBedStatusAsync(Guid bedId, UpdateBedStatusRequest request)
        {
            var bed = await _db.Beds.FindAsync(bedId);
            if (bed is null) return null;

            bed.Status = request.Status;
            bed.LastUpdated = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return new BedSummaryDto(bed.Id, bed.BedNumber, bed.Status, bed.LastUpdated);
        }

        public async Task<bool> DeleteBedAsync(Guid bedId)
        {
            var bed = await _db.Beds.FindAsync(bedId);
            if (bed is null) return false;

            _db.Beds.Remove(bed);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
