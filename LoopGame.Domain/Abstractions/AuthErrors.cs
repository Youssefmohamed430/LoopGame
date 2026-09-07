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
        
        public static Error RegistrationFailed(string msg)
            => new("Auth.RegistrationFailed", $"Registration failed {msg}.");

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
            => new("Auth.InvalidRefreshToken", "Invalid refresh token.");

        public static Error ExpiredRefreshToken()
            => new("Auth.ExpiredRefreshToken", "Refresh token has expired.");

        public static Error RefreshTokenAlreadyUsed()
            => new("Auth.RefreshTokenAlreadyUsed", "Refresh token has already been used.");

        public static Error RefreshTokenRevoked()
            => new("Auth.RefreshTokenRevoked", "Refresh token has been revoked.");
        public static Error UserHasNoRole()
            => new("Auth.UserHasNoRole", "User has no role assigned.");
    }
}
