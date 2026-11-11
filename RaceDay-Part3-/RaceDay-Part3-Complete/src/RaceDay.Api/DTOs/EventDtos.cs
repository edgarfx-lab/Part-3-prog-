
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public record EventRequest(
    [Required, StringLength(150)] string Name,
    [StringLength(1000)] string? Description,
    DateTime EventDate,
    [Required, StringLength(200)] string Location,
    [Range(0.01, 999.99)] decimal DistanceKm,
    [Range(1, int.MaxValue)] int EventTypeId);

public record EventResponse(
    int EventId,
    string Name,
    string? Description,
    DateTime EventDate,
    string Location,
    decimal DistanceKm,
    string EventType,
    int OrganiserId,
    string? BannerImageUrl);

public record CategoryRequest(
    [Required, StringLength(100)] string Name,
    [StringLength(300)] string? Description);

public record CategoryResponse(
    int CategoryId,
    int EventId,
    string Name,
    string? Description);
