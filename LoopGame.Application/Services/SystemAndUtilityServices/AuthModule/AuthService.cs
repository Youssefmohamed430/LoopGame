using LoopGame.Application.Dtos.AuthServiceDtos;
using LoopGame.Application.Utilities;
using LoopGame.Domain.Enums.AuthModule;
using LoopGame.Infrastructure.Identity;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace LoopGame.Application.Services.SystemAndUtilityServices.AuthModule
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IEconomyService _economyService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            IEmailService emailService,
            ILogger<AuthService> logger,
            IEconomyService economyService)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _emailService = emailService;
            _economyService = economyService;
            _logger = logger;
        }

        public async Task<Result<UserToReturnDto>> LoginAsync(LoginDto request)
        {
            if (request == null)
                return AuthErrors.InvalidCredentials();

            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
                return AuthErrors.InvalidCredentials();

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
                return AuthErrors.InvalidCredentials();
            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            if (role == null)
                return Result.Failure<UserToReturnDto>(AuthErrors.UserHasNoRole());
            var tokenUser = new TokenUserDto { Email = user.Email!, UserId = user.Id, Role = role };
            var accessTokenResult = await _tokenService.GenerateAccessToken(tokenUser);
            var refreshTokenResult = await _tokenService.GenerateRefreshTokenAsync(user.Id);

            if (accessTokenResult.IsFailure || refreshTokenResult.IsFailure)
                return Result.Failure<UserToReturnDto>(AuthErrors.TokenGenerationFailed());

            //var userToReturn = _mapper.Map<UserToReturnDto>(user);
            var userToReturn = user.Adapt<UserToReturnDto>(); 
            userToReturn.AccessToken = accessTokenResult.Value;
            userToReturn.RefreshToken = refreshTokenResult.Value.Token; 
            userToReturn.AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
            
            return Result.Success(userToReturn);
        }

        public async Task<Result<UserToReturnDto>> RegisterAsync(RegisterDto request)
        {
            if (request == null)
                return AuthErrors.InvalidCredentials();
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                var user = request.Adapt<ApplicationUser>();
                user.EmailConfirmed = true;

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Registration failed for {Email}: {Errors}",
                        request.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    await _unitOfWork.RollbackAsync();
                    return Result.Failure<UserToReturnDto>
                        (AuthErrors.RegistrationFailed(string.Join(", ", result.Errors.Select(e => e.Description))));
                }

                var profile = new Player
                {
                    PlayerId = user.Id,
                    PlayerName = request.UserName,
                };
                var resultRole = await _userManager.AddToRoleAsync(user, "player");
                if(resultRole != null) {
                    _logger.LogError(
                        "Failed to add Player role for {Email}: {Errors}",request.Email,resultRole.Errors);
                    await _unitOfWork.RollbackAsync();
                    return Result.Failure<UserToReturnDto>(AuthErrors.RegistrationFailed(string.Join(", ", resultRole.Errors)));
                }
                var economyResult = await _economyService.InitializePlayerEconomyAsync(user.Id);
                if (economyResult.IsFailure)
                {
                    _logger.LogError("Failed to initialize economy for user {Email}: {Errors}", request.Email, economyResult.Error.Description);
                    await _unitOfWork.RollbackAsync();
                    return Result.Failure<UserToReturnDto>(AuthErrors.RegistrationFailed(economyResult.Error.Description));
                }
                await _unitOfWork.GetRepository<Player>().AddAsync(profile);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitAsync();

                var tokenUser = new TokenUserDto { Email = user.Email!, UserId = user.Id };
                var accessTokenResult = await _tokenService.GenerateAccessToken(tokenUser);
                var refreshTokenResult = await _tokenService.GenerateRefreshTokenAsync(user.Id);

                var userToReturn = user.Adapt<UserToReturnDto>();
            
                if (accessTokenResult.IsSuccess && refreshTokenResult.IsSuccess)
                {
                    userToReturn.AccessToken = accessTokenResult.Value;
                    userToReturn.RefreshToken = refreshTokenResult.Value.Token;
                }
            
                userToReturn.AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
                return Result.Success(userToReturn);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Unexpected error while registering user {Email}", request.Email);
                return Result.Failure<UserToReturnDto>(AuthErrors.RegistrationFailed("An unexpected error occurred"));
            }
        }
        public async Task<Result<AdminDto>> CreateAdminAsync(RegisterDto request)
        {
            if (request == null)
                return AuthErrors.InvalidCredentials();
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                var user = request.Adapt<ApplicationUser>();
                user.EmailConfirmed = true;

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Registration failed for {Email}: {Errors}", request.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    await _unitOfWork.RollbackAsync();
                    return Result.Failure<AdminDto>
                        (AuthErrors.RegistrationFailed(string.Join(", ", result.Errors.Select(e => e.Description))));
                }
                var roleResult = await _userManager.AddToRoleAsync(user, "admin");

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to add Admin role for {Email}: {Errors}", request.Email, errors);

                    await _unitOfWork.RollbackAsync();

                    return Result.Failure<AdminDto>(
                        AuthErrors.RegistrationFailed(errors));
                }

                await _unitOfWork.CommitAsync();
                var userToReturn = user.Adapt<AdminDto>();
                return Result.Success(userToReturn);
            }
            catch(Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex,"Unexpected error while creating admin {Email}",request.Email);

                return Result.Failure<AdminDto>(AuthErrors.RegistrationFailed("An unexpected error occurred"));

            }

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
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                CreatedAt = DateTime.UtcNow,
                IsUsed = false,
                AttemptCount = 0
            };

            await _unitOfWork.GetRepository<OtpRecord>().AddAsync(otpRecord);
            await _unitOfWork.SaveAsync();

            await _emailService.SendEmail(request.Email, code, "PasswordReset");

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
                return Result.Failure(AuthErrors.InvalidOtpAttempts());

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
