using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Dtos.AuthServiceDtos
{
    public class AdminDto
    {
        public string UserName { get; set; } = null!;
        public int Id { get; set; }
        public string Email { get; set; } = null!;
    }
}
