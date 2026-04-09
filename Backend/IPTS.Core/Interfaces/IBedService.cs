using IPTS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Interfaces
{
    public interface IBedService
    {
        Task<BedSummaryDto> CreateBedAsync(CreateBedRequest request);
        Task<BedSummaryDto?> UpdateBedStatusAsync(Guid bedId, UpdateBedStatusRequest request);
        Task<bool> DeleteBedAsync(Guid bedId);
    }
}
