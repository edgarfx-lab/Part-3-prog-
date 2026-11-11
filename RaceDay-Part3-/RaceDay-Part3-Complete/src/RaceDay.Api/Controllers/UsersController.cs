using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService? _blobStorage;
    public UsersController(RaceDayDbContext db, IBlobStorageService? blobStorage = null)
    {
        _db = db;
        _blobStorage = blobStorage;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var id = HttpContext.Session.GetInt32("UserId");
        if (id is null) return Unauthorized();
        var u = await _db.Users.FindAsync(id.Value);
        return u is null ? Unauthorized() : Ok(new UserResponse(u.UserId,u.FirstName,u.LastName,u.Email,u.Role,u.PhoneNumber,u.ProfilePictureUrl));
    }

    [HttpPut("me")]
    public async Task<IActionResult> Update(UpdateProfileRequest request)
    {
        var id = HttpContext.Session.GetInt32("UserId");
        if (id is null) return Unauthorized();
        var u = await _db.Users.FindAsync(id.Value);
        if (u is null) return Unauthorized();
        u.FirstName=request.FirstName.Trim(); u.LastName=request.LastName.Trim(); u.PhoneNumber=request.PhoneNumber?.Trim();
        await _db.SaveChangesAsync();
        return Ok(new UserResponse(u.UserId,u.FirstName,u.LastName,u.Email,u.Role,u.PhoneNumber,u.ProfilePictureUrl));
    }

    [HttpPut("me/profile-picture")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ProfilePicture(IFormFile image)
    {
        var id = HttpContext.Session.GetInt32("UserId");
        if (id is null) return Unauthorized();
        if (image is null || image.Length == 0) return BadRequest(new { message = "An image file is required." });
        if (image.ContentType is not ("image/jpeg" or "image/png" or "image/webp" or "image/gif"))
            return BadRequest(new { message = "Only JPEG, PNG, WEBP or GIF images are allowed." });
        if (image.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "Images must be 5 MB or smaller." });
        if (_blobStorage is null || !_blobStorage.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Azure Blob Storage is not configured." });

        var user = await _db.Users.FindAsync(id.Value);
        if (user is null) return Unauthorized();
        var imageUrl = await _blobStorage.UploadAsync(image, "profile-pictures", HttpContext.RequestAborted);
        user.ProfilePictureUrl = imageUrl;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Profile picture uploaded successfully.", imageUrl });
    }
}
