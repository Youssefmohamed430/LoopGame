using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.IServices.SystemAndUtilityServices
{
    public interface IScenarioGeneratorService
    {
        Task<Result> GenerateForPlayerAsync(int playerId, CancellationToken ct = default);
    }
}
