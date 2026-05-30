using IPTS.Core.DTOs;
using IPTS.Core.Entities;
using IPTS.Core.Enums;
using IPTS.Core.Interfaces;
using IPTS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IPTS.API.Services
{
    public class HospitalService : IHospitalService
    {
        private readonly AppDbContext _db;
        public HospitalService(AppDbContext db) => _db = db;

        public async Task<List<HospitalSummaryDto>> GetAllAsync()
        {
            var hospitals = await _db.Hospitals
                .Where(h => h.IsActive)
                .Include(h => h.Wards).ThenInclude(w => w.Beds)
                .ToListAsync();

            return hospitals.Select(h => new HospitalSummaryDto(
                Id: h.Id,
                Name: h.Name,
                Address: h.Address,
                Phone: h.Phone,
                IsActive: h.IsActive,
                TotalBeds: h.Wards.SelectMany(w => w.Beds).Count(),
                AvailableBeds: h.Wards.SelectMany(w => w.Beds).Count(b => b.Status == BedStatus.Available)
            )).ToList();
        }

        public async Task<HospitalDto?> GetByIdAsync(Guid id)
        {
            var h = await _db.Hospitals
                .Include(h => h.Wards).ThenInclude(w => w.Beds)
                .FirstOrDefaultAsync(h => h.Id == id);

            return h is null ? null : MapToDto(h);
        }

        public async Task<HospitalDto> CreateAsync(CreateHospitalRequest request)
        {
            var hospital = new Hospital
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Address = request.Address,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Phone = request.Phone,
                AcceptedInsuranceTypes = request.AcceptedInsuranceTypes,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Hospitals.Add(hospital);
            _db.HospitalPerformanceStats.Add(new HospitalPerformanceStat
            {
                Id = Guid.NewGuid(),
                HospitalId = hospital.Id
            });

            await _db.SaveChangesAsync();
            return (await GetByIdAsync(hospital.Id))!;
        }

        public async Task<HospitalDto?> UpdateAsync(Guid id, UpdateHospitalRequest request)
        {
            var hospital = await _db.Hospitals.FindAsync(id);
            if (hospital is null) return null;

            hospital.Name = request.Name;
            hospital.Address = request.Address;
            hospital.Latitude = request.Latitude;
            hospital.Longitude = request.Longitude;
            hospital.Phone = request.Phone;
            hospital.AcceptedInsuranceTypes = request.AcceptedInsuranceTypes;

            await _db.SaveChangesAsync();
            return (await GetByIdAsync(id))!;
        }

        public async Task<bool> DeactivateAsync(Guid id)
        {
            var hospital = await _db.Hospitals.FindAsync(id);
            if (hospital is null) return false;

            hospital.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<HospitalDashboardDto?> GetDashboardAsync(Guid hospitalId)
        {
            var hospital = await _db.Hospitals
                .Include(h => h.Wards).ThenInclude(w => w.Beds)
                .Include(h => h.Ambulances)
                .FirstOrDefaultAsync(h => h.Id == hospitalId);

            if (hospital is null) return null;

            var allBeds = hospital.Wards.SelectMany(w => w.Beds).ToList();
            var stats = await _db.HospitalPerformanceStats
                            .FirstOrDefaultAsync(s => s.HospitalId == hospitalId);

            var today = DateTime.UtcNow.Date;
            var activeTransfers = await _db.TransferRequests.CountAsync(t =>
                (t.SendingHospitalId == hospitalId || t.ReceivingHospitalId == hospitalId) &&
                t.ConfirmedAt >= today &&
                t.Status != TransferStatus.Delivered &&
                t.Status != TransferStatus.Cancelled);

            return new HospitalDashboardDto(
                HospitalId: hospital.Id,
                HospitalName: hospital.Name,
                TotalBeds: allBeds.Count,
                AvailableBeds: allBeds.Count(b => b.Status == BedStatus.Available),
                OccupiedBeds: allBeds.Count(b => b.Status == BedStatus.Occupied),
                ReservedBeds: allBeds.Count(b => b.Status == BedStatus.Reserved),
                MaintenanceBeds: allBeds.Count(b => b.Status == BedStatus.Maintenance),
                ActiveTransfersToday: activeTransfers,
                AvgResponseTimeMinutes: stats?.AvgResponseTimeMinutes ?? 0,
                AcceptanceRate: stats is null || stats.TotalRequestsReceived == 0 ? 0
                                        : (double)stats.TotalAccepted / stats.TotalRequestsReceived * 100,
                Wards: hospital.Wards.Select(w => new WardDetailDto(
                    Id: w.Id,
                    HospitalId: w.HospitalId,
                    HospitalName: hospital.Name,
                    Name: w.Name,
                    Type: w.Type,
                    TotalBeds: w.Beds.Count,
                    AvailableBeds: w.Beds.Count(b => b.Status == BedStatus.Available),
                    OccupiedBeds: w.Beds.Count(b => b.Status == BedStatus.Occupied),
                    ReservedBeds: w.Beds.Count(b => b.Status == BedStatus.Reserved),
                    MaintenanceBeds: w.Beds.Count(b => b.Status == BedStatus.Maintenance),
                    Beds: w.Beds.OrderBy(b => b.BedNumber)
                        .Select(b => new BedSummaryDto(b.Id, b.BedNumber, b.Status, b.LastUpdated))
                        .ToList()
                )).ToList(),
                Ambulances: hospital.Ambulances.Select(a => new AmbulanceSummaryDto(
                    a.Id, a.UnitNumber, a.Status, a.CurrentLatitude, a.CurrentLongitude
                )).ToList()
            );
        }

        // ── PERFORMANCE REPORT ──────────────────────────────
        public async Task<PerformanceSummaryDto> GetPerformanceReportAsync()
        {
            // Load hospitals and their stats separately then join in memory
            var allStats = await _db.HospitalPerformanceStats.ToListAsync();
            var hospitalList = await _db.Hospitals.Where(h => h.IsActive).ToListAsync();
            var report = hospitalList.Select(h => {
                var stats = allStats.FirstOrDefault(s => s.HospitalId == h.Id);
                double acceptanceRate = stats == null || stats.TotalRequestsReceived == 0 ? 0
                : (double)stats.TotalAccepted / stats.TotalRequestsReceived * 100;
                return new HospitalPerformanceReportDto(
                HospitalId: h.Id,
                HospitalName: h.Name,
                TotalTransfersHandled: stats?.TotalTransfersHandled ?? 0,
                TotalRequestsReceived: stats?.TotalRequestsReceived ?? 0,
                TotalAccepted: stats?.TotalAccepted ?? 0,
                TotalDeclined: stats?.TotalDeclined ?? 0,
                AcceptanceRate: Math.Round(acceptanceRate, 1),
                AvgResponseTimeMinutes: Math.Round(stats?.AvgResponseTimeMinutes ?? 0, 1),
                AvgTransferDurationMinutes: Math.Round(stats?.AvgTransferDurationMinutes ?? 0, 1),
                LastUpdated: stats?.LastUpdated ?? DateTime.UtcNow
                );
            }).ToList();
            int totalTransfers = report.Sum(r => r.TotalTransfersHandled);
            int totalReceived = report.Sum(r => r.TotalRequestsReceived);
            int totalAccepted = report.Sum(r => r.TotalAccepted);
            double sysAcceptance = totalReceived == 0 ? 0
            : Math.Round((double)totalAccepted / totalReceived * 100, 1);
            double sysAvgResponse = report.Count == 0 ? 0
            : Math.Round(report.Average(r => r.AvgResponseTimeMinutes), 1);
            return new PerformanceSummaryDto(
                TotalHospitals: report.Count,
                TotalTransfersAllTime: totalTransfers,
                SystemWideAcceptanceRate: sysAcceptance,
                SystemWideAvgResponseMinutes: sysAvgResponse,
                Hospitals: report
            );
        }

        // ── WEEKLY TRANSFERS────────────────────────────────
        public async Task<List<WeeklyTransferDto>> GetWeeklyTransfersAsync(
            Guid hospitalId, int weeks = 8)
        {
            // Get all delivered transfers for this hospital in the last N weeks
            var cutoff = DateTime.UtcNow.AddDays(-(weeks * 7));
            var transfers = await _db.TransferRequests
            .Where(t => t.SendingHospitalId == hospitalId
            && t.Status == TransferStatus.Delivered
            && t.DeliveredAt.HasValue
            && t.DeliveredAt >= cutoff)
            .ToListAsync();
            // Group by ISO week
            var grouped = transfers
            .GroupBy(t => {
                var d = t.DeliveredAt!.Value;
                // ISO week calculation
                int week = System.Globalization.ISOWeek.GetWeekOfYear(d);
                int year = d.Year;
                // Handle week 1 of next year
                if (d.Month == 12 && week == 1) year++;
                return (year, week);
            })
            .OrderBy(g => g.Key.year).ThenBy(g => g.Key.week)
            .Select(g => new WeeklyTransferDto(
            WeekLabel: $"{g.Key.year}-W{g.Key.week:D2}",
            WeekStart: System.Globalization.ISOWeek.ToDateTime(g.Key.year, g.Key.week, DayOfWeek.Monday),
            TransferCount: g.Count()
            ))
            .ToList();
            // Fill in missing weeks with 0
            var result = new List<WeeklyTransferDto>();
            for (int i = weeks - 1; i >= 0; i--)
            {
                var weekStart = DateTime.UtcNow.AddDays(-(i * 7)).Date;
                // Find the Monday of this week
                int diff = (7 + (weekStart.DayOfWeek - DayOfWeek.Monday)) % 7;
                weekStart = weekStart.AddDays(-diff);
                int wNum = System.Globalization.ISOWeek.GetWeekOfYear(weekStart);
                int yr = weekStart.Year;
                if (weekStart.Month == 12 && wNum == 1) yr++;
                var label = $"{yr}-W{wNum:D2}";
                var existing = grouped.FirstOrDefault(g => g.WeekLabel == label);
                result.Add(existing ?? new WeeklyTransferDto(label, weekStart, 0));
            }
            return result;
        }


        private static HospitalDto MapToDto(Hospital h) => new(
            Id: h.Id, Name: h.Name, Address: h.Address,
            Latitude: h.Latitude, Longitude: h.Longitude,
            Phone: h.Phone, AcceptedInsuranceTypes: h.AcceptedInsuranceTypes,
            IsActive: h.IsActive,
            TotalBeds: h.Wards.SelectMany(w => w.Beds).Count(),
            AvailableBeds: h.Wards.SelectMany(w => w.Beds).Count(b => b.Status == BedStatus.Available),
            Wards: h.Wards.Select(w => new WardDetailDto(
                Id: w.Id, HospitalId: w.HospitalId, HospitalName: h.Name,
                Name: w.Name, Type: w.Type, TotalBeds: w.Beds.Count,
                AvailableBeds: w.Beds.Count(b => b.Status == BedStatus.Available),
                OccupiedBeds: w.Beds.Count(b => b.Status == BedStatus.Occupied),
                ReservedBeds: w.Beds.Count(b => b.Status == BedStatus.Reserved),
                MaintenanceBeds: w.Beds.Count(b => b.Status == BedStatus.Maintenance),
                Beds: w.Beds.OrderBy(b => b.BedNumber)
                    .Select(b => new BedSummaryDto(b.Id, b.BedNumber, b.Status, b.LastUpdated))
                    .ToList()
            )).ToList()
        );
    }
}
