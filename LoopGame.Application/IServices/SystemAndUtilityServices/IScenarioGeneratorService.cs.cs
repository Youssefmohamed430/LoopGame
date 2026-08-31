using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.IServices.SystemAndUtilityServices
{
    public interface IBackgroundJob
    {
        Task<Result> ProcessAsync(int sheetFileId);
    }
}
