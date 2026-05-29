using IPTS.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.DTOs
{
    // Crew login — verified against AmbulanceCrew.PasswordHash (BCrypt)
    public record CrewLoginRequest(
        string Email,
        string Password
    );

    public record CrewAuthResponse(
        string AccessToken,
        string FullName,
        string Role,          // "Driver" or "Paramedic"
        Guid CrewId,
        Guid AmbulanceId,
        string AmbulanceUnit, // e.g. "AMB-001"
        DateTime ExpiresAt
    );

    // Active job returned to the crew's Android app
    public record CrewActiveJobDto(
        Guid TransferRequestId,
        string SendingHospitalName,
        string ReceivingHospitalName,
        string ReceivingHospitalAddress,
        double ReceivingHospitalLatitude,
        double ReceivingHospitalLongitude,
        TransferStatus Status,
        string AmbulanceUnit,
        DateTime ConfirmedAt,
        bool HasVitalsSubmitted
    );

    // Crew submits vitals during transport
    public record SubmitVitalsRequest(
        Guid TransferRequestId,
        string BloodPressure,    // e.g. "120/80"
        int HeartRate,
        int OxygenSaturation, // 0-100
        int GlasgowComaScale, // 3-15
        string Notes
    );

    public record VitalsResponseDto(
        Guid Id,
        Guid TransferRequestId,
        string BloodPressure,
        int HeartRate,
        int OxygenSaturation,
        int GlasgowComaScale,
        string Notes,
        DateTime RecordedAt
    );
    public record UpdateLocationRequest(
        double Latitude,
        double Longitude
    );

    public record LocationUpdateResponse(
        Guid AmbulanceId,
        double Latitude,
        double Longitude,
        DateTime UpdatedAt
    );

    public record UpdateJobStatusRequest(
        Guid TransferRequestId,
        TransferStatus NewStatus
    );

}
