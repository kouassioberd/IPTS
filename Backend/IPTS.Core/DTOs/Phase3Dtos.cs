using IPTS.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.DTOs
{
    // ─────────────────────────────────────────────────────────
    // SENDING DOCTOR submits patient data after acceptance
    // This becomes the AES-256 encrypted payload
    // ─────────────────────────────────────────────────────────
    public record SubmitPatientDataRequest(
        Guid BroadcastId,         // Which broadcast was accepted
        string PatientFullName,
        string DateOfBirth,          // "YYYY-MM-DD"
        string Diagnosis,
        string Allergies,            // "None" if none
        string CurrentMedications,   // "None" if none
        string AdditionalNotes,      // "None" if none
        string FamilyContactName,
        string FamilyContactPhone
    );

    // ─────────────────────────────────────────────────────────
    // Returned after patient data is submitted
    // ─────────────────────────────────────────────────────────
    public record TransferRequestDto(
        Guid Id,
        Guid BroadcastId,
        string SendingHospitalName,
        Guid ReceivingHospitalId,
        string ReceivingHospitalName,
        TransferStatus Status,
        DateTime ConfirmedAt,
        bool PatientDataSubmitted,
        bool PatientDataRevealed,
        DateTime? DeliveredAt
    );

    // ─────────────────────────────────────────────────────────
    // Returned to receiving doctor after decryption
    // ─────────────────────────────────────────────────────────
    public record DecryptedPatientDataDto(
        Guid TransferRequestId,
        string PatientFullName,
        string DateOfBirth,
        string Diagnosis,
        string Allergies,
        string CurrentMedications,
        string AdditionalNotes,
        string FamilyContactName,
        string FamilyContactPhone,
        DateTime RevealedAt
    );

    // ─────────────────────────────────────────────────────────
    // Both hospitals call this to confirm the transfer
    // ─────────────────────────────────────────────────────────
    public record ConfirmTransferRequest(
        Guid TransferRequestId
    );

    // ─────────────────────────────────────────────────────────
    // Audit log entry returned in response
    // ─────────────────────────────────────────────────────────
    public record AuditLogDto(
        Guid Id,
        string Action,
        string PerformedByRole,
        DateTime Timestamp,
        string Details
    );

    // ─────────────────────────────────────────────────────────
    // Internal model — what gets serialized to JSON then encrypted
    // Never exposed directly in any API response
    // ─────────────────────────────────────────────────────────
    public record PatientPayload(
        string PatientFullName,
        string DateOfBirth,
        string Diagnosis,
        string Allergies,
        string CurrentMedications,
        string AdditionalNotes,
        string FamilyContactName,
        string FamilyContactPhone
    );

}
