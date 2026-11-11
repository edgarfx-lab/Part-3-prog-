using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

public class EnrolmentAndResultTests
{
    private async Task Seed(string dbName)
    {
        var db=TestInfrastructure.CreateDb(dbName);
        db.Users.AddRange(new User{UserId=1,FirstName="Org",LastName="One",Email="o@x.com",PasswordHash="x",Role="Organiser"},
                          new User{UserId=2,FirstName="Part",LastName="One",Email="p@x.com",PasswordHash="x",Role="Participant"});
        db.EventTypes.Add(new EventType{EventTypeId=1,TypeName="Run"});
        db.Events.Add(new Event{EventId=1,OrganiserId=1,EventTypeId=1,Name="Race",EventDate=DateTime.UtcNow,Location="JHB",DistanceKm=10});
        db.Categories.Add(new Category{CategoryId=1,EventId=1,Name="10km Open"});
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Participant_CanEnrol_AndRecordIsCreated()
    {
        var name=Guid.NewGuid().ToString(); await Seed(name); await using var db=TestInfrastructure.CreateDb(name);
        var c=new EnrolmentsController(db); c.ControllerContext=new(){HttpContext=TestInfrastructure.Context(2,"Participant")};
        var r=await c.Enrol(1,new EnrolmentRequest(1));
        Assert.IsType<CreatedResult>(r); Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var name=Guid.NewGuid().ToString(); await Seed(name); await using var db=TestInfrastructure.CreateDb(name);
        var c=new EnrolmentsController(db); c.ControllerContext=new(){HttpContext=TestInfrastructure.Context(1,"Organiser")};
        var r=await c.Enrol(1,new EnrolmentRequest(1));
        Assert.IsType<ForbidResult>(r);
    }

    [Fact]
    public async Task Participant_CannotViewOrganiserEnrolments_Returns403()
    {
        var name=Guid.NewGuid().ToString(); await Seed(name); await using var db=TestInfrastructure.CreateDb(name);
        var c=new EnrolmentsController(db); c.ControllerContext=new(){HttpContext=TestInfrastructure.Context(2,"Participant")};
        var r=await c.ForEvent(1);
        Assert.IsType<ForbidResult>(r);
    }

    [Fact]
    public async Task WrongCategoryEvent_Returns400()
    {
        var name=Guid.NewGuid().ToString(); await Seed(name); await using var db=TestInfrastructure.CreateDb(name);
        db.Events.Add(new Event{EventId=2,OrganiserId=1,EventTypeId=1,Name="Race2",EventDate=DateTime.UtcNow,Location="JHB",DistanceKm=5});
        db.Categories.Add(new Category{CategoryId=2,EventId=2,Name="5km"});
        await db.SaveChangesAsync();
        var c=new EnrolmentsController(db); c.ControllerContext=new(){HttpContext=TestInfrastructure.Context(2,"Participant")};
        var r=await c.Enrol(1,new EnrolmentRequest(2));
        Assert.IsType<BadRequestObjectResult>(r);
    }

    [Fact]
    public async Task Organiser_CanCaptureResult()
    {
        var name=Guid.NewGuid().ToString(); await Seed(name); await using var db=TestInfrastructure.CreateDb(name);
        db.Enrolments.Add(new Enrolment{EnrolmentId=1,ParticipantId=2,EventId=1,CategoryId=1,Status="Confirmed"}); await db.SaveChangesAsync();
        var c=new ResultsController(db); c.ControllerContext=new(){HttpContext=TestInfrastructure.Context(1,"Organiser")};
        var r=await c.Create(1,new ResultRequest(TimeSpan.FromMinutes(95),47,312));
        Assert.IsType<CreatedResult>(r); Assert.Single(db.Results);
    }

    [Fact]
    public async Task Participant_CannotCaptureResult()
    {
        var name=Guid.NewGuid().ToString(); await Seed(name); await using var db=TestInfrastructure.CreateDb(name);
        var c=new ResultsController(db); c.ControllerContext=new(){HttpContext=TestInfrastructure.Context(2,"Participant")};
        var r=await c.Create(1,new ResultRequest(TimeSpan.FromMinutes(95),47,312));
        Assert.IsType<ForbidResult>(r);
    }

    [Fact]
    public async Task DuplicateResult_Returns409()
    {
        var name=Guid.NewGuid().ToString(); await Seed(name); await using var db=TestInfrastructure.CreateDb(name);
        db.Enrolments.Add(new Enrolment{EnrolmentId=1,ParticipantId=2,EventId=1,CategoryId=1,Status="Confirmed"});
        db.Results.Add(new Result{ResultId=1,EnrolmentId=1,FinishPosition=10}); await db.SaveChangesAsync();
        var c=new ResultsController(db); c.ControllerContext=new(){HttpContext=TestInfrastructure.Context(1,"Organiser")};
        var r=await c.Create(1,new ResultRequest(TimeSpan.FromMinutes(90),11,100));
        Assert.IsType<ConflictObjectResult>(r);
    }
}
