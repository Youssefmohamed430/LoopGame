using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Dtos.AdminDtos
{
    //GET /api/admin/shifts/{shiftId}/students/progress
    public class PlayerShiftProgressDto
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
