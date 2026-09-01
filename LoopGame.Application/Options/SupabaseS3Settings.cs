using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Options
{
    public class SupabaseS3Settings
    {
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
    }
}
