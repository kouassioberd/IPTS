using IPTS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Interfaces
{
    public interface IHospitalService
    {
        Task<List<HospitalSummaryDto>> GetAllAsync();
        Task<HospitalDto?> GetByIdAsync(Guid id);
        Task<HospitalDto> CreateAsync(CreateHospitalRequest request);
        Task<HospitalDto?> UpdateAsync(Guid id, UpdateHospitalRequest request);
        Task<bool> DeactivateAsync(Guid id);
        Task<HospitalDashboardDto?> GetDashboardAsync(Guid hospitalId);
    }
}
