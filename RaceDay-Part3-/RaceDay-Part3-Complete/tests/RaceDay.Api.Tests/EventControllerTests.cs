using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

public class EventControllerTests
{
    private async Task Seed(RaceDay.Api.Data.RaceDayDbContext db)
    {
        db.Users.AddRange(new User{UserId=1,FirstName="Org",LastName="One",Email="o@x.com",PasswordHash="x",Role="Organiser"},
                          new User{UserId=2,FirstName="Part",LastName="One",Email="p@x.com",PasswordHash="x",Role="Participant"});
        db.EventTypes.Add(new EventType{EventTypeId=1,TypeName="Run"});
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Create_Unauthenticated_Returns403()
    {
        await using var db=TestInfrastructure.CreateDb(Guid.NewGuid().ToString()); await Seed(db);
        var c=new EventsController(db); c.ControllerContext=new(){HttpContext=TestInfrastructure.Context()};
        var r=await c.Create(new EventRequest("Race",null,DateTime.UtcNow,"Pretoria",10,1));
        Assert.IsType<ForbidResult>(r);
    }

    [Fact]
    public async Task Create_Participant_Returns403()
    {
        await using var db=TestInfrastructure.CreateDb(Guid.NewGuid().ToString()); await Seed(db);
        var c=new EventsController(db); c.ControllerContext=new(){HttpContext=TestInfrastructure.Context(2,"Participant")};
        var r=await c.Create(new EventRequest("Race",null,DateTime.UtcNow,"Pretoria",10,1));
        Assert.IsType<ForbidResult>(r);
    }

    [Fact]
    public async Task Create_Organiser_Returns201()
    {
        await using var db=TestInfrastructure.CreateDb(Guid.NewGuid().ToString()); await Seed(db);
        var c=new EventsController(db); c.ControllerContext=new(){HttpContext=TestInfrastructure.Context(1,"Organiser")};
        var r=await c.Create(new EventRequest("Race","Test",DateTime.UtcNow,"Pretoria",10,1));
        Assert.IsType<CreatedAtActionResult>(r);
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task Update_NonOwner_Returns403()
    {
        await using var db=TestInfrastructure.CreateDb(Guid.NewGuid().ToString()); await Seed(db);
        db.Events.Add(new Event{EventId=1,OrganiserId=1,EventTypeId=1,Name="Race",EventDate=DateTime.UtcNow,Location="JHB",DistanceKm=10});
        await db.SaveChangesAsync();
        var c=new EventsController(db); c.ControllerContext=new(){HttpContext=TestInfrastructure.Context(2,"Organiser")};
        var r=await c.Update(1,new EventRequest("Changed",null,DateTime.UtcNow,"JHB",10,1));
        Assert.IsType<ForbidResult>(r);
    }

    [Fact]
    public async Task Delete_OrganiserOwner_Returns204()
    {
        await using var db=TestInfrastructure.CreateDb(Guid.NewGuid().ToString()); await Seed(db);
        db.Events.Add(new Event{EventId=1,OrganiserId=1,EventTypeId=1,Name="Race",EventDate=DateTime.UtcNow,Location="JHB",DistanceKm=10});
        await db.SaveChangesAsync();
        var c=new EventsController(db); c.ControllerContext=new(){HttpContext=TestInfrastructure.Context(1,"Organiser")};
        var r=await c.Delete(1);
        Assert.IsType<NoContentResult>(r);
    }
}
