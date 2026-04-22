using IPTS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Interfaces
{
    public interface ITransferService
    {
        // Sending doctor submits patient data after acceptance
        // Encrypts + creates TransferRequest + writes audit log
        Task<TransferRequestDto> SubmitPatientDataAsync(
            SubmitPatientDataRequest request,
            Guid sendingDoctorId,
            Guid sendingHospitalId);

        // Receiving doctor retrieves decrypted patient data
        // Only works if caller is the accepting hospital
        Task<DecryptedPatientDataDto?> GetPatientDataAsync(
            Guid transferRequestId,
            Guid callerHospitalId,
            Guid callerDoctorId);

        // Get a transfer request by ID
        Task<TransferRequestDto?> GetByIdAsync(Guid transferRequestId);

        // Get all transfer requests for a hospital (both sending and receiving)
        Task<List<TransferRequestDto>> GetByHospitalAsync(Guid hospitalId);

        // Get audit trail for a transfer request
        Task<List<AuditLogDto>> GetAuditLogAsync(
            Guid transferRequestId,
            Guid callerHospitalId);
    }

}
