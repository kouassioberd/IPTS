using IPTS.Core.DTOs;
using IPTS.Core.Entities;
using IPTS.Core.Enums;
using IPTS.Core.Interfaces;
using IPTS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography.Xml;

namespace IPTS.API.Services
{
    public class DispatcherService : IDispatcherService
    {
        private readonly AppDbContext _db;

        public DispatcherService(AppDbContext db) => _db = db;

        //GetDashboardAsync — summary counts + active transfers
        public async Task<DispatcherDashboardDto> GetDashboardAsync(Guid hospitalId)
        {
            // Load all transfers where this hospital is SENDING
            // (Dispatcher manages outgoing ambulances, not incoming)
            var transfers = await _db.TransferRequests
                .Where(t => t.SendingHospitalId == hospitalId)
                .Include(t => t.ReceivingHospital)
                .Include(t => t.AssignedAmbulance)
                .OrderByDescending(t => t.ConfirmedAt)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;

            // Count available ambulances at this hospital
            var availableAmbs = await _db.Ambulances
                .CountAsync(a => a.HospitalId == hospitalId
                              && a.Status == AmbulanceStatus.Available);

            return new DispatcherDashboardDto(
                TotalConfirmed: transfers.Count(t => t.Status == TransferStatus.Confirmed),
                TotalAmbulanceAssigned: transfers.Count(t => t.Status == TransferStatus.AmbulanceAssigned),
                TotalEnRoute: transfers.Count(t =>
                    t.Status == TransferStatus.EnRoute ||
                    t.Status == TransferStatus.PatientOnBoard ||
                    t.Status == TransferStatus.InTransit),
                TotalDeliveredToday: transfers.Count(t =>
                    t.Status == TransferStatus.Delivered &&
                    t.DeliveredAt.HasValue &&
                    t.DeliveredAt.Value.Date == today),
                AvailableAmbulances: availableAmbs,
                ActiveTransfers: transfers.Select(MapToDto).ToList()
            );
        }

        //GetAvailableAmbulancesAsync — dropdown for dispatcher
        public async Task<List<AmbulanceDetailDto>> GetAvailableAmbulancesAsync(
        Guid hospitalId)
        {
            var ambulances = await _db.Ambulances
                .Where(a => a.HospitalId == hospitalId
                         && a.Status == AmbulanceStatus.Available)
                .Include(a => a.Hospital)
                .Include(a => a.Crew)
                .OrderBy(a => a.UnitNumber)
                .ToListAsync();

            return ambulances.Select(a => new AmbulanceDetailDto(
                Id: a.Id,
                UnitNumber: a.UnitNumber,
                Status: a.Status,
                CrewCount: a.Crew.Count(c => c.IsActive),
                HospitalName: a.Hospital?.Name ?? ""
            )).ToList();
        }

        //AssignAmbulanceAsync — the most important method
        public async Task<DispatcherTransferDto> AssignAmbulanceAsync(
        AssignAmbulanceRequest request,
        Guid dispatcherUserId,
        Guid hospitalId)
        {
            // 1. Load the transfer — must belong to this hospital
            var transfer = await _db.TransferRequests
                .Include(t => t.ReceivingHospital)
                .Include(t => t.AssignedAmbulance)
                .FirstOrDefaultAsync(t =>
                    t.Id == request.TransferRequestId &&
                    t.SendingHospitalId == hospitalId)
                ?? throw new InvalidOperationException(
                    "Transfer not found or does not belong to your hospital.");

            // 2. Must be in Confirmed status to assign
            if (transfer.Status != TransferStatus.Confirmed)
                throw new InvalidOperationException(
                    $"Cannot assign ambulance. Transfer status is {transfer.Status}, expected Confirmed.");

            // 3. Load the ambulance — must be Available and same hospital
            var ambulance = await _db.Ambulances
                .FirstOrDefaultAsync(a =>
                    a.Id == request.AmbulanceId &&
                    a.HospitalId == hospitalId &&
                    a.Status == AmbulanceStatus.Available)
                ?? throw new InvalidOperationException(
                    "Ambulance not found, not available, or belongs to another hospital.");

            // 4. Assign
            transfer.AssignedAmbulanceId = ambulance.Id;
            transfer.Status = TransferStatus.AmbulanceAssigned;
            ambulance.Status = AmbulanceStatus.Assigned;

            // 5. Audit log
            _db.AuditLogs.Add(new TransferAuditLog
            {
                Id = Guid.NewGuid(),
                TransferRequestId = transfer.Id,
                Action = "AmbulanceAssigned",
                PerformedByUserId = dispatcherUserId,
                PerformedByRole = "Dispatcher",
                Timestamp = DateTime.UtcNow,
                Details = $"Ambulance {ambulance.UnitNumber} assigned to transfer.",
            });

            await _db.SaveChangesAsync();
            return MapToDto(transfer);
        }

        //UpdateStatusAsync — progress the transfer through statuses
        public async Task<DispatcherTransferDto> UpdateStatusAsync(
        UpdateTransferStatusRequest request,
        Guid dispatcherUserId,
        Guid hospitalId)
        {
            var transfer = await _db.TransferRequests
                .Include(t => t.ReceivingHospital)
                .Include(t => t.AssignedAmbulance)
                .FirstOrDefaultAsync(t =>
                    t.Id == request.TransferRequestId &&
                    t.SendingHospitalId == hospitalId)
                ?? throw new InvalidOperationException("Transfer not found.");

            // Validate the status transition
            var validTransitions = new Dictionary<TransferStatus, TransferStatus[]>
            {
                [TransferStatus.AmbulanceAssigned] = [TransferStatus.EnRoute, TransferStatus.Cancelled],
                [TransferStatus.EnRoute] = [TransferStatus.PatientOnBoard, TransferStatus.Cancelled],
                [TransferStatus.PatientOnBoard] = [TransferStatus.InTransit, TransferStatus.Cancelled],
                [TransferStatus.InTransit] = [TransferStatus.Delivered, TransferStatus.Cancelled],
            };

            if (validTransitions.TryGetValue(transfer.Status, out var allowed))
            {
                if (!allowed.Contains(request.NewStatus))
                    throw new InvalidOperationException(
                        $"Invalid transition: {transfer.Status} → {request.NewStatus}.");
            }
            else
            {
                throw new InvalidOperationException(
                    $"Transfer status {transfer.Status} cannot be updated.");
            }

            var previousStatus = transfer.Status;
            transfer.Status = request.NewStatus;

            // On Delivered: free the ambulance + record delivery time
            if (request.NewStatus == TransferStatus.Delivered)
            {
                transfer.DeliveredAt = DateTime.UtcNow;
                if (transfer.AssignedAmbulance is not null)
                    transfer.AssignedAmbulance.Status = AmbulanceStatus.Available;

                // Update hospital performance stats
                var stats = await _db.HospitalPerformanceStats
                    .FirstOrDefaultAsync(s => s.HospitalId == hospitalId);
                if (stats is not null)
                {
                    stats.TotalTransfersHandled++;
                    stats.LastUpdated = DateTime.UtcNow;
                }
            }

            // On Cancelled: also free the ambulance
            if (request.NewStatus == TransferStatus.Cancelled &&
                transfer.AssignedAmbulance is not null)
            {
                transfer.AssignedAmbulance.Status = AmbulanceStatus.Available;
            }

            // Audit log
            _db.AuditLogs.Add(new TransferAuditLog
            {
                Id = Guid.NewGuid(),
                TransferRequestId = transfer.Id,
                Action = $"StatusUpdated:{request.NewStatus}",
                PerformedByUserId = dispatcherUserId,
                PerformedByRole = "Dispatcher",
                Timestamp = DateTime.UtcNow,
                Details = $"{previousStatus} → {request.NewStatus}" +
                    (string.IsNullOrWhiteSpace(request.Notes) ? "" : $". Notes: {request.Notes}"),
            });

            await _db.SaveChangesAsync();
            return MapToDto(transfer);
        }

        //GetTransferByIdAsync + MapToDto helper
        public async Task<DispatcherTransferDto?> GetTransferByIdAsync(
        Guid transferRequestId, Guid hospitalId)
        {
            var t = await _db.TransferRequests
                .Where(t => t.Id == transferRequestId &&
                            t.SendingHospitalId == hospitalId)
                .Include(t => t.ReceivingHospital)
                .Include(t => t.AssignedAmbulance)
                .FirstOrDefaultAsync();
            return t is null ? null : MapToDto(t);
        }

        // ── PRIVATE MAPPER ─────────────────────────────────────
        private static DispatcherTransferDto MapToDto(TransferRequest t) => new(
            Id: t.Id,
            BroadcastId: t.BroadcastId,
            SendingHospitalName: t.SendingHospital?.Name ?? "",
            ReceivingHospitalId: t.ReceivingHospitalId,
            ReceivingHospitalName: t.ReceivingHospital?.Name ?? "",
            Status: t.Status,
            ConfirmedAt: t.ConfirmedAt,
            DeliveredAt: t.DeliveredAt,
            PatientDataSubmitted: t.PatientRecord is not null,
            AssignedAmbulanceId: t.AssignedAmbulanceId,
            AssignedAmbulanceUnit: t.AssignedAmbulance?.UnitNumber,
            AmbulanceStatus: t.AssignedAmbulance?.Status
        );
    }
}
