using IPTS.Core.DTOs;
using IPTS.Core.Entities;
using IPTS.Core.Interfaces;
using IPTS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IPTS.API.Services
{
    public class StaffService : IStaffService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _db;

        public StaffService(UserManager<ApplicationUser> userManager, AppDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public async Task<List<StaffDto>> GetAllByHospitalAsync(Guid hospitalId)
        {
            var staff = await _db.Users
                .Where(u => u.HospitalId == hospitalId)
                .Include(u => u.Hospital)
                .OrderBy(u => u.Role).ThenBy(u => u.FullName)
                .ToListAsync();

            return staff.Select(MapToDto).ToList();
        }

        public async Task<StaffDto?> GetByIdAsync(Guid userId)
        {
            var user = await _db.Users
                .Include(u => u.Hospital)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user is null ? null : MapToDto(user);
        }

        public async Task<(bool Success, string[] Errors)> CreateAsync(
            CreateStaffRequest request, Guid hospitalId)
        {
            if (await _userManager.FindByEmailAsync(request.Email) is not null)
                return (false, ["Email address is already in use."]);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                UserName = request.Email,
                HospitalId = hospitalId,
                Role = request.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return (false, result.Errors.Select(e => e.Description).ToArray());

            await _userManager.AddToRoleAsync(user, request.Role.ToString());
            return (true, []);
        }

        public async Task<StaffDto?> UpdateAsync(Guid userId, UpdateStaffRequest request)
        {
            var user = await _db.Users
                .Include(u => u.Hospital)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null) return null;

            user.FullName = request.FullName;
            user.Email = request.Email;
            user.UserName = request.Email;

            if (user.Role != request.Role)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, request.Role.ToString());
                user.Role = request.Role;
            }

            await _userManager.UpdateAsync(user);
            return MapToDto(user);
        }

        public async Task<bool> DeactivateAsync(Guid userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user is null) return false;

            user.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReactivateAsync(Guid userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user is null) return false;

            user.IsActive = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null) return false;

            var result = await _userManager.ChangePasswordAsync(
                user, request.CurrentPassword, request.NewPassword);

            return result.Succeeded;
        }

        private static StaffDto MapToDto(ApplicationUser u) => new(
            Id: u.Id,
            FullName: u.FullName,
            Email: u.Email ?? "",
            Role: u.Role,
            HospitalId: u.HospitalId,
            HospitalName: u.Hospital?.Name ?? "",
            IsActive: u.IsActive,
            CreatedAt: u.CreatedAt
        );
    }
}
