using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Utilities
{
    public interface IFileContentReaderService
    {
        Result<string> ReadFile( Stream fileStream,string fileName);
    }
}
