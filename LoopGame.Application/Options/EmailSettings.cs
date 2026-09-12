using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Options
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } 
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } 
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } 
    }
}
