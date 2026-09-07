using LoopGame.Application.Dtos.AuthServiceDtos;

namespace LoopGame.Application.IServices.SystemAndUtilityServices
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
