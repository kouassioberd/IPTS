using IPTS.Core.DTOs;
using IPTS.Core.Entities;
using IPTS.Core.Interfaces;
using IPTS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            AppDbContext db,
            IConfiguration config)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _db = db;
            _config = config;
        }

        /// <summary>
        /// Validates credentials and returns a JWT + refresh token pair.
        /// </summary>
        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null || !user.IsActive) return null;

            var valid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!valid) return null;

            return await BuildAuthResponseAsync(user);
        }

        /// <summary>
        /// Registers a new hospital staff member and returns tokens immediately.
        /// </summary>
        public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
        {
            var user = new ApplicationUser
            {
                FullName = request.FullName,
                Email = request.Email,
                UserName = request.Email,
                HospitalId = request.HospitalId,
                Role = request.Role,
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded) return null;

            await _userManager.AddToRoleAsync(user, request.Role.ToString());
            return await BuildAuthResponseAsync(user);
        }

        /// <summary>
        /// Rotates the refresh token. Old token is revoked, new pair issued.
        /// </summary>
        public async Task<AuthResponse?> RefreshAsync(RefreshTokenRequest request)
        {
            var stored = await _db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

            if (stored is null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
                return null;

            var user = await _userManager.FindByIdAsync(stored.UserId.ToString());
            if (user is null || !user.IsActive) return null;

            // Revoke old token
            stored.IsRevoked = true;
            await _db.SaveChangesAsync();

            return await BuildAuthResponseAsync(user);
        }

        /// <summary>
        /// Invalidates a refresh token (logout).
        /// </summary>
        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var stored = await _db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken);

            if (stored is not null)
            {
                stored.IsRevoked = true;
                await _db.SaveChangesAsync();
            }
        }



        private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var expiryDays = int.Parse(_config["JwtSettings:RefreshTokenExpiryDays"]!);

            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            });
            await _db.SaveChangesAsync();

            return new AuthResponse(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                ExpiresAt: DateTime.UtcNow.AddMinutes(60),
                FullName: user.FullName,
                Role: roles.FirstOrDefault() ?? string.Empty,
                HospitalId: user.HospitalId
            );
        }
    }

}
