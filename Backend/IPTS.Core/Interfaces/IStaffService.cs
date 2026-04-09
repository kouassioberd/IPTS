using IPTS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Interfaces
{
    public interface IStaffService
    {
        Task<List<StaffDto>> GetAllByHospitalAsync(Guid hospitalId);
        Task<StaffDto?> GetByIdAsync(Guid userId);
        Task<(bool Success, string[] Errors)> CreateAsync(CreateStaffRequest request, Guid hospitalId);
        Task<StaffDto?> UpdateAsync(Guid userId, UpdateStaffRequest request);
        Task<bool> DeactivateAsync(Guid userId);
        Task<bool> ReactivateAsync(Guid userId);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    }
}
