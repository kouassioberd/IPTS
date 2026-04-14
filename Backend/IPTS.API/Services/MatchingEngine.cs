using IPTS.Core.DTOs;
using IPTS.Core.Enums;
using IPTS.Core.Interfaces;
using IPTS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IPTS.API.Services
{
    // ══════════════════════════════════════════════════════════════════
    // MATCHING ENGINE — Rule-Based Hospital Matching
    // STEP 1: Hard Filters (all must pass):
    //   - Has available bed of required type
    //   - Within requested distance
    //   - Accepts requested insurance

    // STEP 2: Score remaining hospitals (0-100):
    //   - Distance score    30 pts  (closer = more)
    //   - Bed count score   30 pts  (more beds = more, capped at 3)
    //   - Response rate     20 pts  (acceptance history)
    //   - Avg accept time   20 pts  (faster history = more)
    // ══════════════════════════════════════════════════════════════════

    public class MatchingEngine : IMatchingEngine
    {
        private readonly AppDbContext _db;
        private readonly IDistanceService _distanceService;

        public MatchingEngine(AppDbContext db, IDistanceService distanceService)
        {
            _db = db;
            _distanceService = distanceService;
        }

        public async Task<MatchingResultDto> FindMatchesAsync(Guid broadcastId, Guid sendingHospitalId)
        {
            var broadcast = await _db.AnonymousTransferNeeds
                .FirstOrDefaultAsync(b => b.Id == broadcastId)
                ?? throw new InvalidOperationException("Broadcast not found.");

            var sendingHospital = await _db.Hospitals
                .FirstOrDefaultAsync(h => h.Id == sendingHospitalId)
                ?? throw new InvalidOperationException("Sending hospital not found.");

            // Load all other active hospitals with their wards, beds, and stats
            var allHospitals = await _db.Hospitals
                .Where(h => h.IsActive && h.Id != sendingHospitalId)
                .Include(h => h.Wards).ThenInclude(w => w.Beds)
                .ToListAsync();

            var allStats = await _db.HospitalPerformanceStats.ToListAsync();

            int totalChecked = allHospitals.Count;

            // ── STEP 1: HARD FILTERS ──────────────────────────────────
            var filtered = allHospitals.Where(h =>
            {
                // Must have available bed of required type
                bool hasBed = h.Wards.Any(w =>
                    w.Type.ToString().Equals(broadcast.BedTypeRequired, StringComparison.OrdinalIgnoreCase) &&
                    w.Beds.Any(b => b.Status == BedStatus.Available));

                // Must be within distance
                bool withinRange = _distanceService.IsWithinRange(
                    sendingHospital.Latitude, sendingHospital.Longitude,
                    h.Latitude, h.Longitude,
                    broadcast.MaxDistanceMiles);

                // Must accept insurance (skip check if insurance not specified)
                bool acceptsInsurance = string.IsNullOrWhiteSpace(broadcast.InsuranceType) ||
                    broadcast.InsuranceType.Equals("None", StringComparison.OrdinalIgnoreCase) ||
                    h.AcceptedInsuranceTypes
                     .Split(',', StringSplitOptions.RemoveEmptyEntries)
                     .Any(i => i.Trim().Equals(broadcast.InsuranceType, StringComparison.OrdinalIgnoreCase));

                return hasBed && withinRange && acceptsInsurance;
            }).ToList();

            // ── STEP 2: SCORE & RANK ──────────────────────────────────
            var matches = filtered.Select(h =>
            {
                var stats = allStats.FirstOrDefault(s => s.HospitalId == h.Id);

                int availableBeds = h.Wards
                    .Where(w => w.Type.ToString().Equals(broadcast.BedTypeRequired, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(w => w.Beds)
                    .Count(b => b.Status == BedStatus.Available);

                bool hasEquipment =
                    string.IsNullOrWhiteSpace(broadcast.EquipmentNeeded) ||
                    broadcast.EquipmentNeeded.Equals("None", StringComparison.OrdinalIgnoreCase);

                double distanceMiles = _distanceService.GetDistanceMiles(
                    sendingHospital.Latitude, sendingHospital.Longitude,
                    h.Latitude, h.Longitude);

                // Distance score: 0 miles = 30pts, maxDistance miles = 0pts
                int distanceScore = (int)(30.0 * (1.0 - distanceMiles / broadcast.MaxDistanceMiles));
                distanceScore = Math.Clamp(distanceScore, 0, 30);

                // Bed score: each available bed = 10pts, capped at 30
                int bedScore = Math.Min(30, availableBeds * 10);

                // Response rate score: based on historical acceptance rate
                double acceptanceRate = stats is null || stats.TotalRequestsReceived == 0
                    ? 50.0  // new hospitals start at 50%
                    : (double)stats.TotalAccepted / stats.TotalRequestsReceived * 100.0;
                int responseRateScore = Math.Clamp((int)(20.0 * (acceptanceRate / 100.0)), 0, 20);

                // Avg accept time score: 0 min = 20pts, 30+ min = 0pts
                double avgTime = stats?.AvgResponseTimeMinutes ?? 5.0;
                int acceptTimeScore = Math.Clamp((int)(20.0 * (1.0 - Math.Min(avgTime / 30.0, 1.0))), 0, 20);

                return new HospitalMatchDto(
                    HospitalId: h.Id,
                    HospitalName: h.Name,
                    Address: h.Address,
                    DistanceMiles: Math.Round(distanceMiles, 1),
                    AvailableBeds: availableBeds,
                    HasRequiredEquipment: hasEquipment,
                    AcceptsInsurance: true,
                    Score: distanceScore + bedScore + responseRateScore + acceptTimeScore,
                    DistanceScore: distanceScore,
                    BedScore: bedScore,
                    ResponseRateScore: responseRateScore,
                    AvgAcceptTimeScore: acceptTimeScore,
                    AvgResponseTimeMinutes: Math.Round(avgTime, 1),
                    AcceptanceRate: Math.Round(acceptanceRate, 1)
                );
            })
            .OrderByDescending(m => m.Score)
            .ThenBy(m => m.DistanceMiles)
            .ToList();

            return new MatchingResultDto(
                BroadcastId: broadcastId,
                Matches: matches,
                TotalHospitalsChecked: totalChecked,
                TotalFiltered: filtered.Count,
                GeneratedAt: DateTime.UtcNow
            );
        }
    }

}
