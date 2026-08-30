using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Infrastructure.Email
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = "ShiftOS";
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }
}
