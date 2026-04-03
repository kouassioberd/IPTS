using IPTS.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.DTOs
{
    public record LoginRequest(
        string Email,
        string Password
    );

    public record RegisterRequest(
        string FullName,
        string Email,
        string Password,
        StaffRole Role,
        Guid HospitalId
    );

    public record AuthResponse(
        string AccessToken,
        string RefreshToken,
        DateTime ExpiresAt,
        string FullName,
        string Role,
        Guid HospitalId
    );

    public record RefreshTokenRequest(
        string RefreshToken
    );
}
