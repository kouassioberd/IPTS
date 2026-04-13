using IPTS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Interfaces
{
    public interface IMatchingEngine
    {
        Task<MatchingResultDto> FindMatchesAsync(Guid broadcastId, Guid sendingHospitalId);
    }
}
