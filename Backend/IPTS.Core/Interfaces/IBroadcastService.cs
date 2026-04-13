using IPTS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Interfaces
{
    public interface IBroadcastService
    {
        Task<BroadcastDto> CreateAsync(CreateBroadcastRequest request, Guid hospitalId, Guid doctorId);
        Task<BroadcastDto?> GetByIdAsync(Guid id);
        Task<List<BroadcastSummaryDto>> GetMyBroadcastsAsync(Guid hospitalId);
        Task<MatchingResultDto> GetMatchesAsync(Guid broadcastId, Guid sendingHospitalId);
        Task NotifyHospitalsAsync(Guid broadcastId, List<Guid> hospitalIds);
        Task<List<BroadcastSummaryDto>> GetIncomingRequestsAsync(Guid receivingHospitalId);
        Task<HospitalResponseDto> RespondAsync(Guid broadcastId, Guid receivingHospitalId, Guid doctorId, RespondToBroadcastRequest request);
        Task<bool> CancelAsync(Guid broadcastId, Guid hospitalId);
    }
}
