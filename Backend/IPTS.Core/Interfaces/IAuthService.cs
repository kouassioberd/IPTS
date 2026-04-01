using IPTS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task<AuthResponse?> RegisterAsync(RegisterRequest request, Guid hospitalId);
        Task<AuthResponse?> RefreshAsync(RefreshTokenRequest request);
        Task RevokeAsync(string refreshToken);
    }

}
