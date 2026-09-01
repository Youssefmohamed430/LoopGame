using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Dtos.AdminDtos
{
    public class PlayerShiftProgressDetailsDto
    {
        public int ShiftId { get; set; }
        public string ShiftName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

    }
}
