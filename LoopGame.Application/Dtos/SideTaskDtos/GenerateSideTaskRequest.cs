using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Dtos.SideTaskDtos
{
    public class GenerateSideTaskRequest
    {
        public SideTaskReferenceScenarioRequest ReferenceScenario { get; set; } = null!;
        public string ProblemsContent { get; set; } = null!;
    }
}
