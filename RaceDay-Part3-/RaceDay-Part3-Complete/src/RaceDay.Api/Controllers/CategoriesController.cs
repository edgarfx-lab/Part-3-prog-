using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public CategoriesController(RaceDayDbContext db)=>_db=db;

    [HttpGet("events/{eventId:int}/categories")]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if(!await _db.Events.AnyAsync(e=>e.EventId==eventId))return NotFound();
        return Ok(await _db.Categories.AsNoTracking().Where(c=>c.EventId==eventId).Select(c=>new CategoryResponse(c.CategoryId,c.EventId,c.Name,c.Description)).ToListAsync());
    }

    [HttpPost("events/{eventId:int}/categories")]
    public async Task<IActionResult> Create(int eventId, CategoryRequest request)
    {
        if(!IsOrganiser())return Forbid();
        var e=await _db.Events.FindAsync(eventId);if(e is null)return NotFound();if(e.OrganiserId!=UserId())return Forbid();
        var c=new Category{EventId=eventId,Name=request.Name.Trim(),Description=request.Description?.Trim()};
        _db.Categories.Add(c);await _db.SaveChangesAsync();
        return Created($"/api/categories/{c.CategoryId}",new CategoryResponse(c.CategoryId,c.EventId,c.Name,c.Description));
    }

    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        if(!IsOrganiser())return Forbid();
        var c=await _db.Categories.Include(x=>x.Event).SingleOrDefaultAsync(x=>x.CategoryId==id);if(c is null)return NotFound();if(c.Event.OrganiserId!=UserId())return Forbid();
        c.Name=request.Name.Trim();c.Description=request.Description?.Trim();await _db.SaveChangesAsync();
        return Ok(new CategoryResponse(c.CategoryId,c.EventId,c.Name,c.Description));
    }

    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if(!IsOrganiser())return Forbid();
        var c=await _db.Categories.Include(x=>x.Event).SingleOrDefaultAsync(x=>x.CategoryId==id);if(c is null)return NotFound();if(c.Event.OrganiserId!=UserId())return Forbid();
        _db.Categories.Remove(c);await _db.SaveChangesAsync();return NoContent();
    }
    private bool IsOrganiser()=>HttpContext.Session.GetString("Role")=="Organiser"&&HttpContext.Session.GetInt32("UserId") is not null;
    private int UserId()=>HttpContext.Session.GetInt32("UserId")!.Value;
}
