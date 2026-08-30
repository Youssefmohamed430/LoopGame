using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Dtos.AuthServiceDtos
{
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = null!;
    }
    public class ResetPasswordRequest
    {
        public string Email { get; set; } = null!;
        public string Code { get; set; }  = null!;
        public string NewPassword { get; set; } = null!;
    }
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = null!;
    }
}
