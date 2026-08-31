using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Utilities
{
    public interface IEmailService
    {
        Task<Result> SendEmail(string email, string Code , string Purpose);
    }
}
