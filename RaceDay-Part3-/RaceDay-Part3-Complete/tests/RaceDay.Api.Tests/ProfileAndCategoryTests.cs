using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

public class ProfileAndCategoryTests
{
    [Fact]
    public async Task Unauthenticated_Profile_Returns401()
    {
        await using var db=TestInfrastructure.CreateDb(Guid.NewGuid().ToString());
        var c=new UsersController(db); c.ControllerContext=new(){HttpContext=TestInfrastructure.Context()};
        Assert.IsType<UnauthorizedResult>(await c.Me());
    }

    [Fact]
    public async Task Authenticated_Profile_ReturnsUser()
    {
        await using var db=TestInfrastructure.CreateDb(Guid.NewGuid().ToString());
        db.Users.Add(new User{UserId=2,FirstName="Part",LastName="One",Email="p@x.com",PasswordHash="x",Role="Participant"});await db.SaveChangesAsync();
        var c=new UsersController(db);c.ControllerContext=new(){HttpContext=TestInfrastructure.Context(2,"Participant")};
        var r=Assert.IsType<OkObjectResult>(await c.Me()); Assert.NotNull(r.Value);
    }

    [Fact]
    public async Task Participant_CannotCreateCategory()
    {
        await using var db=TestInfrastructure.CreateDb(Guid.NewGuid().ToString());
        db.Users.Add(new User{UserId=2,FirstName="Part",LastName="One",Email="p@x.com",PasswordHash="x",Role="Participant"});
        db.Events.Add(new Event{EventId=1,OrganiserId=2,EventTypeId=1,Name="Race",EventDate=DateTime.UtcNow,Location="JHB",DistanceKm=10});
        await db.SaveChangesAsync();
        var c=new CategoriesController(db);c.ControllerContext=new(){HttpContext=TestInfrastructure.Context(2,"Participant")};
        Assert.IsType<ForbidResult>(await c.Create(1,new CategoryRequest("10km",null)));
    }

    [Fact]
    public async Task Organiser_CanCreateCategory()
    {
        await using var db=TestInfrastructure.CreateDb(Guid.NewGuid().ToString());
        db.Users.Add(new User{UserId=1,FirstName="Org",LastName="One",Email="o@x.com",PasswordHash="x",Role="Organiser"});
        db.Events.Add(new Event{EventId=1,OrganiserId=1,EventTypeId=1,Name="Race",EventDate=DateTime.UtcNow,Location="JHB",DistanceKm=10});
        await db.SaveChangesAsync();
        var c=new CategoriesController(db);c.ControllerContext=new(){HttpContext=TestInfrastructure.Context(1,"Organiser")};
        Assert.IsType<CreatedResult>(await c.Create(1,new CategoryRequest("10km",null)));
    }
}
