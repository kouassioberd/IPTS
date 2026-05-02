using IPTS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Interfaces
{
    public interface IDispatcherService
    {
        // Get dispatcher dashboard — summary + active transfers
        Task<DispatcherDashboardDto> GetDashboardAsync(Guid hospitalId);

        // Get all available ambulances for this hospital
        Task<List<AmbulanceDetailDto>> GetAvailableAmbulancesAsync(Guid hospitalId);

        // Assign ambulance to a confirmed transfer
        // Sets TransferRequest.Status → AmbulanceAssigned
        // Sets Ambulance.Status → Assigned
        Task<DispatcherTransferDto> AssignAmbulanceAsync(
            AssignAmbulanceRequest request,
            Guid dispatcherUserId,
            Guid hospitalId);

        // Update transfer status as ambulance progresses
        Task<DispatcherTransferDto> UpdateStatusAsync(
            UpdateTransferStatusRequest request,
            Guid dispatcherUserId,
            Guid hospitalId);

        // Get single transfer with full ambulance detail
        Task<DispatcherTransferDto?> GetTransferByIdAsync(
            Guid transferRequestId,
            Guid hospitalId);
    }

}
