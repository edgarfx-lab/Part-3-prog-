
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordService _passwords;
    public AuthController(RaceDayDbContext db, IPasswordService passwords) { _db=db; _passwords=passwords; }

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var role = request.Role.Trim();
        if (role is not ("Organiser" or "Participant"))
            return BadRequest(new { message = "Role must be Organiser or Participant." });
        if (await _db.Users.AnyAsync(u => u.Email == request.Email.Trim().ToLowerInvariant()))
            return Conflict(new { message = "Email is already registered." });

        var user = new User {
            FirstName=request.FirstName.Trim(), LastName=request.LastName.Trim(),
            Email=request.Email.Trim().ToLowerInvariant(), Role=role,
            PhoneNumber=request.PhoneNumber?.Trim(), PasswordHash=_passwords.Hash(request.Password)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Created($"/api/users/{user.UserId}", ToResponse(user));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == request.Email.Trim().ToLowerInvariant());
        if (user is null || !_passwords.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role);
        return Ok(new { message="Login successful.", userId=user.UserId, role=user.Role });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Session ended successfully." });
    }

    private static UserResponse ToResponse(User u) =>
        new(u.UserId,u.FirstName,u.LastName,u.Email,u.Role,u.PhoneNumber,u.ProfilePictureUrl);
}
