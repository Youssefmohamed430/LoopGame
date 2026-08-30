using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Domain.Abstractions
{
    public static class AuthErrors
    {
        public static Error InvalidCredentials()
            => new("Auth.InvalidCredentials", "Invalid credentials provided.");
        
        public static Error TokenGenerationFailed()
            => new("Auth.TokenGenerationFailed", "Failed to generate tokens.");
        
        public static Error RegistrationFailed()
            => new("Auth.RegistrationFailed", "Registration failed.");

        public static Error UserNotFound()
            => new("Auth.UserNotFound", "User not found.");

        public static Error InvalidOtp()
            => new("Auth.InvalidOtp", "Invalid or expired OTP.");

        public static Error OtpUsed()
            => new("Auth.OtpUsed", "OTP has already been used.");
        
        public static Error OtpExpired()
            => new("Auth.OtpExpired", "OTP has expired.");

        public static Error ResetFailed()
            => new("Auth.ResetFailed", "Failed to reset password.");

        public static Error InvalidRefreshToken()
            => new(
                "Invalid refresh token.",
                "AuthErrorCodes.INVALID_REFRESH_TOKEN");

        public static Error ExpiredRefreshToken()
            => new(
                "Refresh token has expired.",
                "AuthErrorCodes.EXPIRED_REFRESH_TOKEN");

        public static Error RefreshTokenAlreadyUsed()
            => new(
                "Refresh token has already been used.",
                "AuthErrorCodes.REFRESH_TOKEN_ALREADY_USED");

        public static Error RefreshTokenRevoked()
            => new(
                "Refresh token has been revoked.",
                "AuthErrorCodes.REVOKED_REFRESH_TOKEN");
    }
}
