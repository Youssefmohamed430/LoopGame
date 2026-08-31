using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Dtos.SideTaskDtos
{
    public class SideTaskReferenceScenarioRequest
    {
        public string TemplateKey { get; set; } = string.Empty;
        public string ConceptTag { get; set; } = string.Empty;
        public PlayerRank RankRequired { get; set; } = PlayerRank.Intern;
        public string TitleTemplate { get; set; } = string.Empty;
        public string DescriptionTemplate { get; set; } = string.Empty;

        /// <summary>JSON slot definition schema.</summary>
        public string SlotsSchema { get; set; } = "{}";

        public decimal EgpMin { get; set; } = 500m;
        public decimal EgpMax { get; set; } = 3_000m;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
