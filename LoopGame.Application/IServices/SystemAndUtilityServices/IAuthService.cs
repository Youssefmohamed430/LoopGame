using LoopGame.Application.Dtos.AuthServiceDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Services.SystemAndUtilityServices
{
    public interface IAuthService
    {
        Task<Result<UserToReturnDto>> RegisterAsync(RegisterDto request);
        Task<Result<UserToReturnDto>> LoginAsync(LoginDto request);
        Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<Result> ResetPasswordAsync(ResetPasswordRequest request);
        Task<Result<UserToReturnDto>> RefreshTokenAsync(RefreshTokenRequest request);
        Task LogoutAsync(string userId, string? refreshToken = null);
    }
}
