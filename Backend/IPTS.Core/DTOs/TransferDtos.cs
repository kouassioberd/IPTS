using IPTS.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.DTOs
{
    // ══════════════════════════════════════════════════════════════════
    // PHASE 1 — ANONYMOUS BROADCAST DTOs
    // Zero patient PII in any of these models
    // ══════════════════════════════════════════════════════════════════

    public record CreateBroadcastRequest(
        string BedTypeRequired,   // "ICU","ER","General","Surgery","Pediatric","Cardiology"
        string EquipmentNeeded,   // "Ventilator","Defibrillator","None"
        string InsuranceType,     // "Medicare","Medicaid","Aetna","Cigna","BlueCross"
        int MaxDistanceMiles,
        UrgencyLevel Urgency
    // SendingHospitalId + DoctorId always taken from JWT — never from request body
    );

    public record BroadcastDto(
        Guid Id,
        Guid SendingHospitalId,
        string SendingHospitalName,
        string BedTypeRequired,
        string EquipmentNeeded,
        string InsuranceType,
        int MaxDistanceMiles,
        UrgencyLevel Urgency,
        BroadcastStatus Status,
        DateTime CreatedAt,
        int TotalResponses,
        int AcceptedResponses,
        int DeclinedResponses,
        List<HospitalResponseDto> Responses
    );

    public record BroadcastSummaryDto(
        Guid Id,
        string SendingHospitalName,
        string BedTypeRequired,
        string EquipmentNeeded,
        UrgencyLevel Urgency,
        BroadcastStatus Status,
        DateTime CreatedAt,
        int TotalResponses,
        int AcceptedResponses
    );

    // ══════════════════════════════════════════════════════════════════
    // PHASE 2 — MATCHING ENGINE DTOs
    // ══════════════════════════════════════════════════════════════════

    public record HospitalMatchDto(
        Guid HospitalId,
        string HospitalName,
        string Address,
        double DistanceMiles,
        int AvailableBeds,
        bool HasRequiredEquipment,
        bool AcceptsInsurance,
        int Score,               // 0-100 composite score
        int DistanceScore,       // 0-30
        int BedScore,            // 0-30
        int ResponseRateScore,   // 0-20
        int AvgAcceptTimeScore,  // 0-20
        double AvgResponseTimeMinutes,
        double AcceptanceRate
    );

    public record MatchingResultDto(
        Guid BroadcastId,
        List<HospitalMatchDto> Matches,
        int TotalHospitalsChecked,
        int TotalFiltered,
        DateTime GeneratedAt
    );

    // ══════════════════════════════════════════════════════════════════
    // RESPONSE DTOs
    // ══════════════════════════════════════════════════════════════════

    public record RespondToBroadcastRequest(
        ResponseType Response,
        string? DeclineReason
    );

    public record NotifyHospitalsRequest(
        List<Guid> HospitalIds
    );

    public record HospitalResponseDto(
        Guid Id,
        Guid BroadcastId,
        Guid ReceivingHospitalId,
        string ReceivingHospitalName,
        ResponseType Response,
        string? DeclineReason,
        DateTime? RespondedAt
    );
}
