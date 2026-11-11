using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace RaceDay.Mvc.Models;

public sealed class RegisterViewModel
{
    [Required, StringLength(100)] public string FirstName { get; set; } = "";
    [Required, StringLength(100)] public string LastName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "Participant";
    [Phone] public string? PhoneNumber { get; set; }
}

public sealed class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public sealed class EventFormViewModel
{
    public int EventId { get; set; }
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [StringLength(1000)] public string? Description { get; set; }
    [Required] public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30);
    [Required, StringLength(200)] public string Location { get; set; } = "";
    [Range(0.01, 999.99)] public decimal DistanceKm { get; set; } = 5;
    [Range(1, 3)] public int EventTypeId { get; set; } = 1;
    public IFormFile? BannerImage { get; set; }
}

public sealed class CategoryFormViewModel
{
    public int EventId { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [StringLength(300)] public string? Description { get; set; }
}

public sealed class EnrolmentStatusViewModel
{
    public int EnrolmentId { get; set; }
    [Required] public string Status { get; set; } = "Pending";
}

public sealed class ResultFormViewModel
{
    public int EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    [Range(1, int.MaxValue)] public int? FinishPosition { get; set; }
    [Range(1, int.MaxValue)] public int? TotalFinishers { get; set; }
}

public sealed class ProfileViewModel
{
    [Required, StringLength(100)] public string FirstName { get; set; } = "";
    [Required, StringLength(100)] public string LastName { get; set; } = "";
    [Phone] public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public IFormFile? ProfilePicture { get; set; }
}

public sealed class JsonListViewModel
{
    public string Title { get; set; } = "";
    public List<JsonElement> Items { get; set; } = new();
    public string? Message { get; set; }
}

public sealed class OrganiserEventSummary
{
    public JsonElement Event { get; set; }
    public int EnrolmentCount { get; set; }
}
