using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Dtos.AdminDtos
{
    public class PlayerOverallProgressDto
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;

        public int? CurrentShiftId { get; set; }
        public string CurrentShiftName { get; set; } = string.Empty;

        public int CompletedShifts { get; set; }
        public int TotalShifts { get; set; }

        public decimal OverallProgress => TotalShifts == 0 ? 0 : (decimal)CompletedShifts / TotalShifts * 100;

        public List<PlayerShiftProgressDetailsDto> Shifts { get; set; } = [];
    }
}
