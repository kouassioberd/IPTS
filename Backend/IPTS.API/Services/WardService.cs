using IPTS.Core.DTOs;
using IPTS.Core.Entities;
using IPTS.Core.Enums;
using IPTS.Core.Interfaces;
using IPTS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IPTS.API.Services
{
    public class WardService : IWardService
    {
        private readonly AppDbContext _db;
        public WardService(AppDbContext db) => _db = db;

        public async Task<List<WardDetailDto>> GetByHospitalAsync(Guid hospitalId)
        {
            var wards = await _db.Wards
                .Where(w => w.HospitalId == hospitalId)
                .Include(w => w.Hospital)
                .Include(w => w.Beds)
                .OrderBy(w => w.Type).ThenBy(w => w.Name)
                .ToListAsync();

            return wards.Select(MapToDetail).ToList();
        }

        public async Task<WardDetailDto?> GetByIdAsync(Guid wardId)
        {
            var ward = await _db.Wards
                .Include(w => w.Hospital)
                .Include(w => w.Beds)
                .FirstOrDefaultAsync(w => w.Id == wardId);

            return ward is null ? null : MapToDetail(ward);
        }

        public async Task<WardDetailDto> CreateAsync(Guid hospitalId, CreateWardRequest request)
        {
            var ward = new Ward
            {
                Id = Guid.NewGuid(),
                HospitalId = hospitalId,
                Name = request.Name,
                Type = request.Type,
                TotalBeds = request.TotalBeds,
            };

            _db.Wards.Add(ward);

            var prefix = ward.Name.Length >= 3
                ? ward.Name[..3].ToUpper()
                : ward.Name.ToUpper();

            for (int i = 1; i <= request.TotalBeds; i++)
            {
                _db.Beds.Add(new Bed
                {
                    Id = Guid.NewGuid(),
                    WardId = ward.Id,
                    BedNumber = $"{prefix}-{i:D2}",
                    Status = BedStatus.Available,
                });
            }

            await _db.SaveChangesAsync();
            return (await GetByIdAsync(ward.Id))!;
        }

        public async Task<WardDetailDto?> UpdateAsync(Guid wardId, UpdateWardRequest request)
        {
            var ward = await _db.Wards.FindAsync(wardId);
            if (ward is null) return null;

            ward.Name = request.Name;
            ward.Type = request.Type;
            ward.TotalBeds = request.TotalBeds;

            await _db.SaveChangesAsync();
            return (await GetByIdAsync(wardId))!;
        }

        public async Task<bool> DeleteAsync(Guid wardId)
        {
            var ward = await _db.Wards
                .Include(w => w.Beds)
                .FirstOrDefaultAsync(w => w.Id == wardId);

            if (ward is null) return false;

            bool hasBusyBeds = ward.Beds.Any(b =>
                b.Status == BedStatus.Occupied || b.Status == BedStatus.Reserved);

            if (hasBusyBeds)
                throw new InvalidOperationException(
                    "Cannot delete ward: it has occupied or reserved beds.");

            _db.Wards.Remove(ward);
            await _db.SaveChangesAsync();
            return true;
        }

        private static WardDetailDto MapToDetail(Ward w) => new(
            Id: w.Id, HospitalId: w.HospitalId, HospitalName: w.Hospital?.Name ?? "",
            Name: w.Name, Type: w.Type, TotalBeds: w.Beds.Count,
            AvailableBeds: w.Beds.Count(b => b.Status == BedStatus.Available),
            OccupiedBeds: w.Beds.Count(b => b.Status == BedStatus.Occupied),
            ReservedBeds: w.Beds.Count(b => b.Status == BedStatus.Reserved),
            MaintenanceBeds: w.Beds.Count(b => b.Status == BedStatus.Maintenance),
            Beds: w.Beds.OrderBy(b => b.BedNumber)
                .Select(b => new BedSummaryDto(b.Id, b.BedNumber, b.Status, b.LastUpdated))
                .ToList()
        );
    }
}
