using LoopGame.Domain.Enums.AuthModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Domain.Entities.SideTask
{
    public class SheetFile
    {
        public int Id { get; set; }
        public int ShiftId { get; set; }
        public string FileName { get; set; }
        public string S3Key { get; set; }
        public SheetFileStatus Status { get; set; }
        public DateTime UploadedAt { get; set; }
        public int UploadedByUserId { get; set; }
    }
}
