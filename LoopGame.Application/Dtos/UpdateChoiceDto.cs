using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Dtos
{
    public class UpdateChoiceDto
    {
        public string? ChoiceText { get; set; }
        public int? ConsequenceId { get; set; }
        public string? ImmediateFeedback { get; set; }
    }
}
