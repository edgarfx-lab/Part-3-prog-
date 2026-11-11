
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService? _blobStorage;

    public EventsController(RaceDayDbContext db, IBlobStorageService? blobStorage = null)
    {
        _db = db;
        _blobStorage = blobStorage;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? eventType,
        [FromQuery] DateTime? from)
    {
        var query = _db.Events
            .AsNoTracking()
            .Include(e => e.EventType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(
                e => e.EventType.TypeName == eventType);
        }

        if (from.HasValue)
        {
            query = query.Where(
                e => e.EventDate >= from.Value);
        }

        var data = await query
            .OrderBy(e => e.EventDate)
            .Select(e => new EventResponse(
                e.EventId,
                e.Name,
                e.Description,
                e.EventDate,
                e.Location,
                e.DistanceKm,
                e.EventType.TypeName,
                e.OrganiserId,
                e.BannerImageUrl))
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var eventItem = await _db.Events
            .AsNoTracking()
            .Include(e => e.EventType)
            .Include(e => e.Categories)
            .SingleOrDefaultAsync(e => e.EventId == id);

        if (eventItem is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            eventItem.EventId,
            eventItem.Name,
            eventItem.Description,
            eventItem.EventDate,
            eventItem.Location,
            eventItem.DistanceKm,
            eventItem.BannerImageUrl,
            EventType = eventItem.EventType.TypeName,
            eventItem.OrganiserId,
            Categories = eventItem.Categories.Select(c =>
                new CategoryResponse(
                    c.CategoryId,
                    c.EventId,
                    c.Name,
                    c.Description))
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(EventRequest request)
    {
        if (!IsRole("Organiser"))
        {
            return Forbid();
        }

        var eventTypeExists = await _db.EventTypes
            .AnyAsync(x => x.EventTypeId == request.EventTypeId);

        if (!eventTypeExists)
        {
            return BadRequest(
                new { message = "Invalid event type." });
        }

        var eventItem = new Event
        {
            OrganiserId = UserId(),
            EventTypeId = request.EventTypeId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm
        };

        _db.Events.Add(eventItem);
        await _db.SaveChangesAsync();

        await _db.Entry(eventItem)
            .Reference(e => e.EventType)
            .LoadAsync();

        return CreatedAtAction(
            nameof(Get),
            new { id = eventItem.EventId },
            new EventResponse(
                eventItem.EventId,
                eventItem.Name,
                eventItem.Description,
                eventItem.EventDate,
                eventItem.Location,
                eventItem.DistanceKm,
                eventItem.EventType.TypeName,
                eventItem.OrganiserId,
                eventItem.BannerImageUrl));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        EventRequest request)
    {
        if (!IsRole("Organiser"))
        {
            return Forbid();
        }

        var eventItem = await _db.Events
            .Include(e => e.EventType)
            .SingleOrDefaultAsync(e => e.EventId == id);

        if (eventItem is null)
        {
            return NotFound();
        }

        if (eventItem.OrganiserId != UserId())
        {
            return Forbid();
        }

        var eventTypeExists = await _db.EventTypes
            .AnyAsync(x => x.EventTypeId == request.EventTypeId);

        if (!eventTypeExists)
        {
            return BadRequest(
                new { message = "Invalid event type." });
        }

        eventItem.Name = request.Name.Trim();
        eventItem.Description = request.Description?.Trim();
        eventItem.EventDate = request.EventDate;
        eventItem.Location = request.Location.Trim();
        eventItem.DistanceKm = request.DistanceKm;
        eventItem.EventTypeId = request.EventTypeId;

        await _db.SaveChangesAsync();

        await _db.Entry(eventItem)
            .Reference(e => e.EventType)
            .LoadAsync();

        return Ok(new EventResponse(
            eventItem.EventId,
            eventItem.Name,
            eventItem.Description,
            eventItem.EventDate,
            eventItem.Location,
            eventItem.DistanceKm,
            eventItem.EventType.TypeName,
            eventItem.OrganiserId,
            eventItem.BannerImageUrl));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsRole("Organiser"))
        {
            return Forbid();
        }

        var eventItem = await _db.Events.FindAsync(id);

        if (eventItem is null)
        {
            return NotFound();
        }

        if (eventItem.OrganiserId != UserId())
        {
            return Forbid();
        }

        _db.Events.Remove(eventItem);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:int}/banner")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Banner(
        int id,
        IFormFile image)
    {
        if (!IsRole("Organiser"))
        {
            return Forbid();
        }

        var eventItem = await _db.Events.FindAsync(id);

        if (eventItem is null)
        {
            return NotFound();
        }

        if (eventItem.OrganiserId != UserId())
        {
            return Forbid();
        }

        if (image is null || image.Length == 0)
            return BadRequest(new { message = "An image file is required." });
        if (image.ContentType is not ("image/jpeg" or "image/png" or "image/webp" or "image/gif"))
            return BadRequest(new { message = "Only JPEG, PNG, WEBP or GIF images are allowed." });
        if (image.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "Images must be 5 MB or smaller." });
        if (_blobStorage is null || !_blobStorage.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Azure Blob Storage is not configured." });

        var imageUrl = await _blobStorage.UploadAsync(image, "event-banners", HttpContext.RequestAborted);
        eventItem.BannerImageUrl = imageUrl;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Event banner uploaded successfully.", imageUrl });
    }

    private bool IsRole(string role)
    {
        return HttpContext.Session.GetString("Role") == role
            && HttpContext.Session.GetInt32("UserId") is not null;
    }

    private int UserId()
    {
        return HttpContext.Session.GetInt32("UserId")!.Value;
    }
}
