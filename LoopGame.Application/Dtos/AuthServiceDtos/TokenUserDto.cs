using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Dtos.AuthServiceDtos
{
    public class TokenUserDto
    {
        public string Email { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; }

    }
    public class GeneratedRefreshTokenDto
    {
        public string Token { get; set; } = string.Empty;
        public string TokenHash { get; set; } = string.Empty;
    }
}
