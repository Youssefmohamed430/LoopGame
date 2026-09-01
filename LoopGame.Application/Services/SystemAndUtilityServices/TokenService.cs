using LoopGame.Application.Dtos.AuthServiceDtos;
using LoopGame.Application.IServices.SystemAndUtilityServices;
using LoopGame.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Services.SystemAndUtilityServices
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public TokenService(IOptions<JwtSettings> jwtOptions, IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {

            _jwtSettings = jwtOptions.Value;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }
        public async Task<Result<string>> GenerateAccessToken(TokenUserDto user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Email, user.Email!),
                new(ClaimTypes.Role, user.Role.ToString()),
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                signingCredentials: creds
            );

            return Result.Success(new JwtSecurityTokenHandler().WriteToken(token));
        }

        public async Task<Result<GeneratedRefreshTokenDto>> GenerateRefreshTokenAsync(int userId)
        {
            var tokenBytes = new byte[64];

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(tokenBytes);

            var token = Convert.ToBase64String(tokenBytes);

            var tokenHash = HashRefreshToken(token);

            var refreshToken = new RefreshToken
            {
                UserId = userId,
                TokenHash = tokenHash,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
            };

            await _unitOfWork.GetRepository<RefreshToken>().AddAsync(refreshToken);
            await _unitOfWork.SaveAsync();

            return Result.Success( new GeneratedRefreshTokenDto
            {
                Token = token,
                TokenHash = tokenHash
            });
        }

        public async Task<Result<UserToReturnDto>> RefreshAccessTokenAsync(string refreshToken)
        {
            var repository = _unitOfWork.GetRepository<RefreshToken>();
            
            var tokenHash = HashRefreshToken(refreshToken);

            var storedToken = repository.FindWithTracking(
            t =>
                t.TokenHash == tokenHash &&
                t.ExpiresAt > DateTime.UtcNow);

            if (storedToken == null)
                return Result.Failure<UserToReturnDto>(AuthErrors.InvalidRefreshToken());

            var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());

            if (storedToken.RevokedAt != null)
                return Result.Failure<UserToReturnDto>(
                    AuthErrors.RefreshTokenRevoked());

            storedToken.RevokedAt = DateTime.UtcNow;

            var newRefreshTokenResult = await GenerateRefreshTokenAsync(storedToken.UserId);

            if (newRefreshTokenResult.IsFailure)
                return Result.Failure<UserToReturnDto>(newRefreshTokenResult.Error);

            var tokenUserDto = new TokenUserDto
            {
                UserId = storedToken.UserId,
                Email = user.Email,
                Role = user.Role.ToString(),
            };

            var accessTokenResult = await GenerateAccessToken(tokenUserDto);

            if (accessTokenResult.IsFailure)
                return Result.Failure<UserToReturnDto>(accessTokenResult.Error);

            return Result.Success(new UserToReturnDto {
                AccessToken= accessTokenResult.Value,
                RefreshToken= newRefreshTokenResult.Value.Token,
                AccessTokenExpiresAt= DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                UserId= storedToken.UserId,
                Email= user.Email,
                FullName= user.DisplayName,
                Role= user.Role
            });
        }

        public async Task<Result> RevokeRefreshTokenAsync(string refreshToken)
        {
            var tokenHash = HashRefreshToken(refreshToken);
            var token =  _unitOfWork.GetRepository<RefreshToken>()
             .Find(t => t.TokenHash == tokenHash);

            if (token != null)
            {
                token.RevokedAt = DateTime.UtcNow;
                await _unitOfWork.SaveAsync();
            }

            return Result.Success();
        }


        private static string HashRefreshToken(string token)
        {
            var hashBytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(token));

            return Convert.ToHexString(hashBytes);
        }
    }
}
