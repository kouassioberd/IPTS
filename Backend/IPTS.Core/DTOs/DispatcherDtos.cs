using IPTS.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.DTOs
{
    // ─────────────────────────────────────────────────────────
    // Ambulance summary for the dispatcher dropdown
    // Shows only ambulances that belong to the dispatcher's hospital
    // ─────────────────────────────────────────────────────────
    public record AmbulanceDetailDto(
        Guid Id,
        string UnitNumber,
        AmbulanceStatus Status,
        int CrewCount,
        string HospitalName
    );

    // ─────────────────────────────────────────────────────────
    // Dispatcher assigns an ambulance to a confirmed transfer
    // ─────────────────────────────────────────────────────────
    public record AssignAmbulanceRequest(
        Guid TransferRequestId,
        Guid AmbulanceId
    );

    // ─────────────────────────────────────────────────────────
    // Dispatcher updates the transfer status as it progresses
    // Valid transitions:
    //   AmbulanceAssigned → EnRoute
    //   EnRoute           → PatientOnBoard
    //   PatientOnBoard    → InTransit
    //   InTransit         → Delivered
    //   Any               → Cancelled
    // ─────────────────────────────────────────────────────────
    public record UpdateTransferStatusRequest(
        Guid TransferRequestId,
        TransferStatus NewStatus,
        string? Notes        // optional notes for the audit log
    );

    // ─────────────────────────────────────────────────────────
    // Full transfer detail returned to dispatcher
    // Extends TransferRequestDto with ambulance info
    // ─────────────────────────────────────────────────────────
    public record DispatcherTransferDto(
        Guid Id,
        Guid BroadcastId,
        string SendingHospitalName,
        Guid ReceivingHospitalId,
        string ReceivingHospitalName,
        TransferStatus Status,
        DateTime ConfirmedAt,
        DateTime? DeliveredAt,
        bool PatientDataSubmitted,
        // Ambulance info — null if not yet assigned
        Guid? AssignedAmbulanceId,
        string? AssignedAmbulanceUnit,
        AmbulanceStatus? AmbulanceStatus
    );

    // ─────────────────────────────────────────────────────────
    // Summary for the dispatcher dashboard list
    // ─────────────────────────────────────────────────────────
    public record DispatcherDashboardDto(
        int TotalConfirmed,
        int TotalAmbulanceAssigned,
        int TotalEnRoute,
        int TotalDeliveredToday,
        int AvailableAmbulances,
        List<DispatcherTransferDto> ActiveTransfers
    );
}
