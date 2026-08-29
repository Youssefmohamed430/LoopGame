using LoopGame.Application.Dtos.AuthServiceDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.IServices.SystemAndUtilityServices
{
    public interface ITokenService
    {
        Task<Result<string>> GenerateAccessToken(TokenUserDto user);
        Task<Result<GeneratedRefreshTokenDto>> GenerateRefreshTokenAsync(int userId);
        Task<Result<UserToReturnDto>> RefreshAccessTokenAsync(string refreshToken);
        Task<Result> RevokeRefreshTokenAsync(string refreshToken);
    }


}
