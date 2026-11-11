using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public ResultsController(RaceDayDbContext db)=>_db=db;

    [HttpPost("enrolments/{enrolmentId:int}/results")]
    public async Task<IActionResult> Create(int enrolmentId, ResultRequest request)
    {
        if(!IsOrganiser())return Forbid();
        var en=await _db.Enrolments.Include(x=>x.Event).Include(x=>x.Category).SingleOrDefaultAsync(x=>x.EnrolmentId==enrolmentId);if(en is null)return NotFound();if(en.Event.OrganiserId!=UserId())return Forbid();
        if(await _db.Results.AnyAsync(x=>x.EnrolmentId==enrolmentId))return Conflict(new{message="A result already exists for this enrolment."});
        if(request.FinishPosition.HasValue&&request.TotalFinishers.HasValue&&request.FinishPosition>request.TotalFinishers)return BadRequest(new{message="Finish position cannot exceed total finishers."});
        var r=new Result{EnrolmentId=enrolmentId,FinishTime=request.FinishTime,FinishPosition=request.FinishPosition,TotalFinishers=request.TotalFinishers};
        _db.Results.Add(r);await _db.SaveChangesAsync();
        return Created($"/api/enrolments/{enrolmentId}/results",new ResultResponse(r.ResultId,enrolmentId,en.Event.Name,en.Event.EventDate,en.Category.Name,r.FinishTime,r.FinishPosition,r.TotalFinishers));
    }

    [HttpGet("users/me/results")]
    public async Task<IActionResult> Mine()
    {
        if(!IsParticipant())return Forbid();
        return Ok(await Query().Where(x=>x.Enrolment.ParticipantId==UserId()).OrderByDescending(x=>x.Enrolment.Event.EventDate)
            .Select(x=>new ResultResponse(x.ResultId,x.EnrolmentId,x.Enrolment.Event.Name,x.Enrolment.Event.EventDate,x.Enrolment.Category.Name,x.FinishTime,x.FinishPosition,x.TotalFinishers)).ToListAsync());
    }

    [HttpGet("events/{eventId:int}/results")]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        if(!IsOrganiser())return Forbid();
        var e=await _db.Events.FindAsync(eventId);if(e is null)return NotFound();if(e.OrganiserId!=UserId())return Forbid();
        return Ok(await Query().Where(x=>x.Enrolment.EventId==eventId)
            .Select(x=>new ResultResponse(x.ResultId,x.EnrolmentId,x.Enrolment.Event.Name,x.Enrolment.Event.EventDate,x.Enrolment.Category.Name,x.FinishTime,x.FinishPosition,x.TotalFinishers)).ToListAsync());
    }

    private IQueryable<Result> Query()=>_db.Results.AsNoTracking().Include(x=>x.Enrolment).ThenInclude(x=>x.Event).Include(x=>x.Enrolment).ThenInclude(x=>x.Category);
    private bool IsOrganiser()=>HttpContext.Session.GetString("Role")=="Organiser"&&HttpContext.Session.GetInt32("UserId") is not null;
    private bool IsParticipant()=>HttpContext.Session.GetString("Role")=="Participant"&&HttpContext.Session.GetInt32("UserId") is not null;
    private int UserId()=>HttpContext.Session.GetInt32("UserId")!.Value;
}
