using IPTS.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.DTOs
{
    // Hospital
    public record CreateHospitalRequest(
        string Name,
        string Address,
        double Latitude,
        double Longitude,
        string Phone,
        string AcceptedInsuranceTypes
    );

    public record UpdateHospitalRequest(
        string Name,
        string Address,
        double Latitude,
        double Longitude,
        string Phone,
        string AcceptedInsuranceTypes
    );

    public record HospitalDto(
        Guid Id,
        string Name,
        string Address,
        double Latitude,
        double Longitude,
        string Phone,
        string AcceptedInsuranceTypes,
        bool IsActive,
        int TotalBeds,
        int AvailableBeds,
        List<WardDetailDto> Wards
    );

    public record HospitalSummaryDto(
        Guid Id,
        string Name,
        string Address,
        string Phone,
        bool IsActive,
        int TotalBeds,
        int AvailableBeds
    );

    public record HospitalDashboardDto(
        Guid HospitalId,
        string HospitalName,
        int TotalBeds,
        int AvailableBeds,
        int OccupiedBeds,
        int ReservedBeds,
        int MaintenanceBeds,
        int ActiveTransfersToday,
        double AvgResponseTimeMinutes,
        double AcceptanceRate,
        List<WardDetailDto> Wards,
        List<AmbulanceSummaryDto> Ambulances
    );

    public record AmbulanceSummaryDto(
        Guid Id,
        string UnitNumber,
        AmbulanceStatus Status,
        double Latitude,
        double Longitude
    );

    // Ward
    public record CreateWardRequest(
        string Name,
        WardType Type,
        int TotalBeds
        // HospitalId always taken from JWT — never from request body
    );

    public record UpdateWardRequest(
        string Name,
        WardType Type,
        int TotalBeds
    );

    public record WardDetailDto(
        Guid Id,
        Guid HospitalId,
        string HospitalName,
        string Name,
        WardType Type,
        int TotalBeds,
        int AvailableBeds,
        int OccupiedBeds,
        int ReservedBeds,
        int MaintenanceBeds,
        List<BedSummaryDto> Beds
    );

    // Bed
    public record CreateBedRequest(
        Guid WardId,
        string BedNumber
    );

    public record UpdateBedStatusRequest(
        BedStatus Status
    );

    public record BedSummaryDto(
        Guid Id,
        string BedNumber,
        BedStatus Status,
        DateTime LastUpdated
    );

    // Staff
    public record CreateStaffRequest(
        string FullName,
        string Email,
        string Password,
        StaffRole Role
        // HospitalId always taken from JWT — never from request body
    );

    public record UpdateStaffRequest(
        string FullName,
        string Email,
        StaffRole Role
    );

    public record StaffDto(
        Guid Id,
        string FullName,
        string Email,
        StaffRole Role,
        Guid HospitalId,
        string HospitalName,
        bool IsActive,
        DateTime CreatedAt
    );

    public record ChangePasswordRequest(
        string CurrentPassword,
        string NewPassword
    );

}
