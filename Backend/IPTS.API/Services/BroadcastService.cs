using IPTS.Core.DTOs;
using IPTS.Core.Entities;
using IPTS.Core.Enums;
using IPTS.Core.Interfaces;
using IPTS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IPTS.API.Services
{
    public class BroadcastService : IBroadcastService
    {
        private readonly AppDbContext _db;
        private readonly IMatchingEngine _engine;

        public BroadcastService(AppDbContext db, IMatchingEngine engine)
        {
            _db = db;
            _engine = engine;
        }

        // ── CREATE BROADCAST (Phase 1) ────────────────────────────────
        public async Task<BroadcastDto> CreateAsync(
            CreateBroadcastRequest request, Guid hospitalId, Guid doctorId)
        {
            var broadcast = new AnonymousTransferNeed
            {
                Id = Guid.NewGuid(),
                SendingHospitalId = hospitalId,
                SendingDoctorId = doctorId,
                BedTypeRequired = request.BedTypeRequired,
                EquipmentNeeded = request.EquipmentNeeded,
                InsuranceType = request.InsuranceType,
                MaxDistanceMiles = request.MaxDistanceMiles,
                Urgency = request.Urgency,
                Status = BroadcastStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };

            _db.AnonymousTransferNeeds.Add(broadcast);
            await _db.SaveChangesAsync();
            return (await GetByIdAsync(broadcast.Id))!;
        }

        // ── GET BY ID ─────────────────────────────────────────────────
        public async Task<BroadcastDto?> GetByIdAsync(Guid id)
        {
            var b = await _db.AnonymousTransferNeeds
                .Include(b => b.SendingHospital)
                .Include(b => b.Responses).ThenInclude(r => r.ReceivingHospital)
                .FirstOrDefaultAsync(b => b.Id == id);

            return b is null ? null : MapToDto(b);
        }

        // ── MY BROADCASTS (sending doctor view) ───────────────────────
        public async Task<List<BroadcastSummaryDto>> GetMyBroadcastsAsync(Guid hospitalId)
        {
            var list = await _db.AnonymousTransferNeeds
                .Where(b => b.SendingHospitalId == hospitalId)
                .Include(b => b.SendingHospital)
                .Include(b => b.Responses)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return list.Select(MapToSummary).ToList();
        }

        // ── GET MATCHES (Phase 2) ─────────────────────────────────────
        public async Task<MatchingResultDto> GetMatchesAsync(Guid broadcastId, Guid sendingHospitalId)
            => await _engine.FindMatchesAsync(broadcastId, sendingHospitalId);

        // ── NOTIFY HOSPITALS ──────────────────────────────────────────
        // Creates a Pending response row per selected hospital
        public async Task NotifyHospitalsAsync(Guid broadcastId, List<Guid> hospitalIds)
        {
            var existing = await _db.HospitalResponses
                .Where(r => r.BroadcastId == broadcastId)
                .Select(r => r.ReceivingHospitalId)
                .ToListAsync();

            foreach (var hId in hospitalIds.Where(id => !existing.Contains(id)))
            {
                _db.HospitalResponses.Add(new HospitalResponse
                {
                    Id = Guid.NewGuid(),
                    BroadcastId = broadcastId,
                    ReceivingHospitalId = hId,
                    Response = ResponseType.Pending,
                });
            }

            await _db.SaveChangesAsync();
        }

        // ── INCOMING REQUESTS (receiving hospital view) ───────────────
        // Only returns broadcasts where this hospital has a Pending response
        public async Task<List<BroadcastSummaryDto>> GetIncomingRequestsAsync(Guid receivingHospitalId)
        {
            var list = await _db.AnonymousTransferNeeds
                .Where(b =>
                    b.Status == BroadcastStatus.Active &&
                    b.Responses.Any(r =>
                        r.ReceivingHospitalId == receivingHospitalId &&
                        r.Response == ResponseType.Pending))
                .Include(b => b.SendingHospital)
                .Include(b => b.Responses)
                .OrderByDescending(b => b.Urgency)
                .ThenBy(b => b.CreatedAt)
                .ToListAsync();

            return list.Select(MapToSummary).ToList();
        }

        // ── RESPOND (receiving hospital accepts or declines) ──────────
        public async Task<HospitalResponseDto> RespondAsync(
            Guid broadcastId, Guid receivingHospitalId,
            Guid doctorId, RespondToBroadcastRequest request)
        {
            var response = await _db.HospitalResponses
                .Include(r => r.ReceivingHospital)
                .FirstOrDefaultAsync(r =>
                    r.BroadcastId == broadcastId &&
                    r.ReceivingHospitalId == receivingHospitalId)
                ?? throw new InvalidOperationException("No pending request found for this hospital.");

            response.Response = request.Response;
            response.DeclineReason = request.DeclineReason;
            response.RespondedAt = DateTime.UtcNow;
            response.RespondingDoctorId = doctorId;

            // If accepted, mark broadcast as Matched
            if (request.Response == ResponseType.Accepted)
            {
                var broadcast = await _db.AnonymousTransferNeeds.FindAsync(broadcastId);
                if (broadcast is not null)
                    broadcast.Status = BroadcastStatus.Matched;
            }

            // Update performance stats
            await UpdateStatsAsync(receivingHospitalId, response);

            await _db.SaveChangesAsync();

            return new HospitalResponseDto(
                Id: response.Id,
                BroadcastId: response.BroadcastId,
                ReceivingHospitalId: response.ReceivingHospitalId,
                ReceivingHospitalName: response.ReceivingHospital?.Name ?? "",
                Response: response.Response,
                DeclineReason: response.DeclineReason,
                RespondedAt: response.RespondedAt
            );
        }

        // ── CANCEL ────────────────────────────────────────────────────
        public async Task<bool> CancelAsync(Guid broadcastId, Guid hospitalId)
        {
            var broadcast = await _db.AnonymousTransferNeeds
                .FirstOrDefaultAsync(b => b.Id == broadcastId && b.SendingHospitalId == hospitalId);

            if (broadcast is null) return false;

            broadcast.Status = BroadcastStatus.Cancelled;
            await _db.SaveChangesAsync();
            return true;
        }

        // ── STATS UPDATE ──────────────────────────────────────────────
        private async Task UpdateStatsAsync(Guid hospitalId, HospitalResponse response)
        {
            var stats = await _db.HospitalPerformanceStats
                .FirstOrDefaultAsync(s => s.HospitalId == hospitalId);
            if (stats is null) return;

            stats.TotalRequestsReceived++;
            if (response.Response == ResponseType.Accepted) stats.TotalAccepted++;
            if (response.Response == ResponseType.Declined) stats.TotalDeclined++;

            if (response.RespondedAt.HasValue)
            {
                var broadcast = await _db.AnonymousTransferNeeds.FindAsync(response.BroadcastId);
                if (broadcast is not null)
                {
                    double mins = (response.RespondedAt.Value - broadcast.CreatedAt).TotalMinutes;
                    stats.AvgResponseTimeMinutes =
                        (stats.AvgResponseTimeMinutes * (stats.TotalRequestsReceived - 1) + mins)
                        / stats.TotalRequestsReceived;
                }
            }

            stats.LastUpdated = DateTime.UtcNow;
        }

        // ── MAPPERS ───────────────────────────────────────────────────
        private static BroadcastDto MapToDto(AnonymousTransferNeed b) => new(
            Id: b.Id, SendingHospitalId: b.SendingHospitalId,
            SendingHospitalName: b.SendingHospital?.Name ?? "",
            BedTypeRequired: b.BedTypeRequired, EquipmentNeeded: b.EquipmentNeeded,
            InsuranceType: b.InsuranceType, MaxDistanceMiles: b.MaxDistanceMiles,
            Urgency: b.Urgency, Status: b.Status, CreatedAt: b.CreatedAt,
            TotalResponses: b.Responses.Count,
            AcceptedResponses: b.Responses.Count(r => r.Response == ResponseType.Accepted),
            DeclinedResponses: b.Responses.Count(r => r.Response == ResponseType.Declined),
            Responses: b.Responses.Select(r => new HospitalResponseDto(
                r.Id, r.BroadcastId, r.ReceivingHospitalId,
                r.ReceivingHospital?.Name ?? "",
                r.Response, r.DeclineReason, r.RespondedAt
            )).ToList()
        );

        private static BroadcastSummaryDto MapToSummary(AnonymousTransferNeed b) => new(
            Id: b.Id, SendingHospitalName: b.SendingHospital?.Name ?? "",
            BedTypeRequired: b.BedTypeRequired, EquipmentNeeded: b.EquipmentNeeded,
            Urgency: b.Urgency, Status: b.Status, CreatedAt: b.CreatedAt,
            TotalResponses: b.Responses.Count,
            AcceptedResponses: b.Responses.Count(r => r.Response == ResponseType.Accepted)
        );
    }

}
