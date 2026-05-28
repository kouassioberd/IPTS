using IPTS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Interfaces
{
    public interface ICrewService
    {
        Task<CrewAuthResponse?> LoginAsync(CrewLoginRequest request);
        Task<CrewActiveJobDto?> GetActiveJobAsync(Guid ambulanceId);
        Task<VitalsResponseDto> SubmitVitalsAsync(
            SubmitVitalsRequest request, Guid crewId);
        Task<LocationUpdateResponse> UpdateLocationAsync(
            UpdateLocationRequest request, Guid ambulanceId);
        Task<CrewActiveJobDto> UpdateJobStatusAsync(
            UpdateJobStatusRequest request, Guid ambulanceId);

    }

}
