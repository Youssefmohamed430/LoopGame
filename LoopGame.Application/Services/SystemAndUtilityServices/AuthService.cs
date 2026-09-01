using LoopGame.Application.Dtos.AuthServiceDtos;
using LoopGame.Application.Utilities;
using LoopGame.Domain.Enums.AuthModule;
using LoopGame.Infrastructure.Identity;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace LoopGame.Application.Services.SystemAndUtilityServices
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;
        private readonly IMapper _mapper;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            IEmailService emailService,
            ILogger<AuthService> logger,
            IMapper mapper)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _emailService = emailService;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<Result<UserToReturnDto>> LoginAsync(LoginDto request)
        {
            if (request == null)
                return AuthErrors.InvalidCredentials();
                
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return AuthErrors.InvalidCredentials();

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
                return AuthErrors.InvalidCredentials();

            var tokenUser = new TokenUserDto { Email = user.Email!, UserId = user.Id, Role = user.Role.ToString() };
            var accessTokenResult = await _tokenService.GenerateAccessToken(tokenUser);
            var refreshTokenResult = await _tokenService.GenerateRefreshTokenAsync(user.Id);

            if (accessTokenResult.IsFailure || refreshTokenResult.IsFailure)
                return Result.Failure<UserToReturnDto>(AuthErrors.TokenGenerationFailed());

            var userToReturn = _mapper.Map<UserToReturnDto>(user);
            userToReturn.AccessToken = accessTokenResult.Value;
            userToReturn.RefreshToken = refreshTokenResult.Value.TokenHash; // Assuming TokenHash is returned as the string or hash here
            userToReturn.AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
            
            return Result.Success(userToReturn);
        }

        public async Task<Result<UserToReturnDto>> RegisterAsync(RegisterDto request)
        {
            if (request == null)
                return AuthErrors.InvalidCredentials();
                
            var user = _mapper.Map<ApplicationUser>(request);
            user.Role = Roles.Player;
            user.EmailConfirmed = true;

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Registration failed for {Email}: {Errors}",
                    request.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return Result.Failure<UserToReturnDto>(AuthErrors.RegistrationFailed());
            }

            var profile = _mapper.Map<Player>(request);

            await _unitOfWork.GetRepository<Player>().AddAsync(profile);
            await _unitOfWork.SaveAsync();

            var tokenUser = new TokenUserDto { Email = user.Email!, UserId = user.Id, Role = user.Role.ToString() };
            var accessTokenResult = await _tokenService.GenerateAccessToken(tokenUser);
            var refreshTokenResult = await _tokenService.GenerateRefreshTokenAsync(user.Id);

            var userToReturn = _mapper.Map<UserToReturnDto>(user);
            
            if (accessTokenResult.IsSuccess && refreshTokenResult.IsSuccess)
            {
                userToReturn.AccessToken = accessTokenResult.Value;
                userToReturn.RefreshToken = refreshTokenResult.Value.TokenHash;
            }
            
            userToReturn.AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
            return Result.Success(userToReturn);
        }

        public async Task LogoutAsync(string userId, string? refreshToken = null)
        {
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _tokenService.RevokeRefreshTokenAsync(refreshToken);
            }
            // Return nothing as the return type is Task
        }

        public async Task<Result<UserToReturnDto>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            return await _tokenService.RefreshAccessTokenAsync(request.RefreshToken);
        }

        public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Result.Success();

            var oldOtps = _unitOfWork.GetRepository<OtpRecord>().FindAll(o =>
                o.Email == request.Email &&
                !o.IsUsed);

            foreach (var otp in oldOtps)
            {
                otp.IsUsed = true;
                await _unitOfWork.GetRepository<OtpRecord>().UpdateAsync(otp);
            }

            var code = GenerateRandomCode(6);
            
            var otpRecord = new OtpRecord
            {
                Email = request.Email,
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow,
                IsUsed = false,
                AttemptCount = 0
            };

            await _unitOfWork.GetRepository<OtpRecord>().AddAsync(otpRecord);
            await _unitOfWork.SaveAsync();

            await _emailService.SendEmail(request.Email, code, "Password Reset");

            return Result.Success();
        }

        public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Result.Failure(AuthErrors.UserNotFound());

            var otpRecord = await _unitOfWork.GetRepository<OtpRecord>()
                .FindAsync(o => o.Email == request.Email && !o.IsUsed );

            if (otpRecord == null)
                return Result.Failure(AuthErrors.InvalidOtp());

            if (otpRecord.ExpiresAt < DateTime.UtcNow)
                return Result.Failure(AuthErrors.OtpExpired());

            if (otpRecord.AttemptCount >= 5)
                return Result.Failure(AuthErrors.InvalidOtp());

            if (otpRecord.Code != request.Code)
            {
                otpRecord.AttemptCount++;

                await _unitOfWork.GetRepository<OtpRecord>()
                    .UpdateAsync(otpRecord);

                await _unitOfWork.SaveAsync();

                return Result.Failure(AuthErrors.InvalidOtp());
            }
            await _unitOfWork.BeginTransactionAsync();
            try
            {

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var resetResult = await _userManager.ResetPasswordAsync(
                    user,
                    token,
                    request.NewPassword);

                if (!resetResult.Succeeded)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Failure(AuthErrors.ResetFailed());

                }

                otpRecord.IsUsed = true;

                await _unitOfWork.GetRepository<OtpRecord>().UpdateAsync(otpRecord);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitAsync();
                return Result.Success();
            }
            catch 
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
            
        }


        private string GenerateRandomCode(int length)
        {
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[RandomNumberGenerator.GetInt32(s.Length)]).ToArray());
        }
    }
}
