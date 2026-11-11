
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpPost("events/{eventId:int}/enrolments")]
    public async Task<IActionResult> Enrol(
        int eventId,
        EnrolmentRequest request)
    {
        if (!IsRole("Participant"))
            return Forbid();

        var eventItem = await _db.Events.FindAsync(eventId);

        if (eventItem is null)
            return NotFound();

        var category = await _db.Categories.FindAsync(request.CategoryId);

        if (category is null || category.EventId != eventId)
        {
            return BadRequest(new
            {
                message = "Category does not belong to this event."
            });
        }

        if (await _db.Enrolments.AnyAsync(x =>
            x.ParticipantId == UserId() &&
            x.EventId == eventId))
        {
            return Conflict(new
            {
                message = "Already enrolled in this event."
            });
        }

        var enrolment = new Enrolment
        {
            ParticipantId = UserId(),
            EventId = eventId,
            CategoryId = category.CategoryId,
            Status = "Pending"
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        return Created(
            $"/api/enrolments/{enrolment.EnrolmentId}",
            new
            {
                enrolment.EnrolmentId,
                enrolment.ParticipantId,
                enrolment.EventId,
                enrolment.CategoryId,
                enrolment.EnrolmentDate,
                enrolment.Status
            });
    }

    [HttpGet("users/me/enrolments")]
    public async Task<IActionResult> Mine()
    {
        if (!IsRole("Participant"))
            return Forbid();

        var participantId = UserId();

        var enrolments = await _db.Enrolments
            .AsNoTracking()
            .Where(x => x.ParticipantId == participantId)
            .OrderByDescending(x => x.EnrolmentDate)
            .Select(x => new EnrolmentResponse(
                x.EnrolmentId,
                x.ParticipantId,
                x.Participant.FirstName + " " + x.Participant.LastName,
                x.EventId,
                x.Event.Name,
                x.CategoryId,
                x.Category.Name,
                x.EnrolmentDate,
                x.Status))
            .ToListAsync();

        return Ok(enrolments);
    }

    [HttpGet("events/{eventId:int}/enrolments")]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        if (!IsRole("Organiser"))
            return Forbid();

        var eventItem = await _db.Events.FindAsync(eventId);

        if (eventItem is null)
            return NotFound();

        if (eventItem.OrganiserId != UserId())
            return Forbid();

        var enrolments = await _db.Enrolments
            .AsNoTracking()
            .Where(x => x.EventId == eventId)
            .OrderByDescending(x => x.EnrolmentDate)
            .Select(x => new EnrolmentResponse(
                x.EnrolmentId,
                x.ParticipantId,
                x.Participant.FirstName + " " + x.Participant.LastName,
                x.EventId,
                x.Event.Name,
                x.CategoryId,
                x.Category.Name,
                x.EnrolmentDate,
                x.Status))
            .ToListAsync();

        return Ok(enrolments);
    }

    [HttpPut("enrolments/{id:int}/status")]
    public async Task<IActionResult> Status(
        int id,
        EnrolmentStatusRequest request)
    {
        if (!IsRole("Organiser"))
            return Forbid();

        if (request.Status is not ("Pending" or "Confirmed" or "Cancelled"))
        {
            return BadRequest(new
            {
                message = "Invalid status."
            });
        }

        var enrolment = await _db.Enrolments
            .Include(x => x.Event)
            .SingleOrDefaultAsync(x => x.EnrolmentId == id);

        if (enrolment is null)
            return NotFound();

        if (enrolment.Event.OrganiserId != UserId())
            return Forbid();

        enrolment.Status = request.Status;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            enrolment.EnrolmentId,
            enrolment.Status
        });
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
