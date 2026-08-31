using Amazon.Runtime.Internal;
using LoopGame.Application.Dtos;
using LoopGame.Application.IServices.SystemAndUtilityServices;
using LoopGame.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading;

namespace LoopGame.Controllers;


[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController(IAdminService _adminService ) : ControllerBase
{


    [HttpPost("sheets/upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> UploadSheet([FromForm] IFormFile  file,[FromForm] int shiftId)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new {message = "File is required."});

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase){".docx", ".doc", ".pdf" };
        var extension = Path.GetExtension(file.FileName);
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new{message = "Only Word, and PDF files are allowed."});

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _adminService.UploadAsync(shiftId,adminId , file);

        if (result.IsFailure)
            return result.Error.ToActionResult();

        return Accepted(new
        {
            message = "File uploaded successfully.",
        });
    }

    [HttpGet("sheets/list/{shiftId}")]
    public async Task<ActionResult> ListUploadedFiles(int shiftId)
    {
        var result = await _adminService.ListUploadedFilesAsync(shiftId);

        if (result.IsFailure)
            return result.Error.ToActionResult();

        return Ok(result.Value);
    }
    [HttpDelete("sheets/delete/{fileId}")]
    public async Task<ActionResult> DeleteUploadedFile(int fileId)
    {
        var result = await _adminService.DeleteUploadedFileAsync(fileId);
        if (result.IsFailure)
            return result.Error.ToActionResult();
        return Ok(new { message = "File deleted successfully." });
    }


}