using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

namespace LoopGame.Application.Utilities
{
    public class FileContentReaderService : IFileContentReaderService
    {
        public Result<string> ReadAsync(Stream fileStream, string fileName)
        {
            var extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => ReadPdf(fileStream),

                ".docx" =>  ReadWord(fileStream),

                _ => throw new NotSupportedException(
                    $"File type '{extension}' is not supported.")
            };
        }
        private Result<string> ReadPdf(Stream stream)
        {
            using var document = PdfDocument.Open(stream);
            var text = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                if (!string.IsNullOrWhiteSpace(page.Text))
                    text.AppendLine(page.Text);
            }

            if (string.IsNullOrWhiteSpace(text.ToString()))
                return Result.Failure<string>(FileErrors.FileEmpty);

            return Result.Success(text.ToString());
        }

        private Result<string> ReadWord(Stream stream)
        {
            using var document =  WordprocessingDocument.Open(stream, false);
            var body = document.MainDocumentPart?.Document?.Body;
            if (body == null)
                return Result.Failure<string>(FileErrors.FileEmpty);
            var text = new StringBuilder();
            foreach(var paragraph in body.Elements<Paragraph>())
            {
                if (!string.IsNullOrWhiteSpace(paragraph.InnerText))
                    text.AppendLine(paragraph.InnerText);
            }
            var content = text.ToString();
            if (string.IsNullOrWhiteSpace(content))
                return Result.Failure<string>(FileErrors.FileEmpty);
            return Result.Success(content);
        }
    }
}
