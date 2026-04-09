using IPTS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Interfaces
{
    public interface IWardService
    {
        Task<List<WardDetailDto>> GetByHospitalAsync(Guid hospitalId);
        Task<WardDetailDto?> GetByIdAsync(Guid wardId);
        Task<WardDetailDto> CreateAsync(Guid hospitalId, CreateWardRequest request);
        Task<WardDetailDto?> UpdateAsync(Guid wardId, UpdateWardRequest request);
        Task<bool> DeleteAsync(Guid wardId);
    }
}
