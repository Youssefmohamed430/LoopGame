using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Domain.Abstractions
{
    public static class FileErrors
    {
        public static readonly Error FileAlreadyExists = new("FileAlreadyExists", "The uploaded file already exists.");
        public static readonly Error FileUploadFailed = new("FileUploadFailed", "The file upload failed.");
        public static readonly Error FileNotFound = new("FileNotFound", "The file was not found.");
        public static readonly Error FileEmpty = new("FileEmpty", "The PDF does not contain readable text.");
    }
}
