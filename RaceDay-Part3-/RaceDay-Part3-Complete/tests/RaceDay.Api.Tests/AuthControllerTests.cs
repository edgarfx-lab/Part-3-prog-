using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public class AuthControllerTests
{
    private readonly PasswordService _passwords=new();

    [Fact]
    public async Task Register_ValidParticipant_Returns201_AndHashesPassword()
    {
        await using var db=TestInfrastructure.CreateDb(Guid.NewGuid().ToString());
        var c=new AuthController(db,_passwords);
        var r=await c.Register(new RegisterRequest("Test","User","test@example.com","Password123!","Participant","0711111111"));
        var created=Assert.IsType<CreatedResult>(r);
        Assert.Equal(201,created.StatusCode);
        var u=db.Users.Single();
        Assert.NotEqual("Password123!",u.PasswordHash);
        Assert.True(_passwords.Verify("Password123!",u.PasswordHash));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        await using var db=TestInfrastructure.CreateDb(Guid.NewGuid().ToString());
        db.Users.Add(new User{FirstName="A",LastName="B",Email="a@b.com",PasswordHash=_passwords.Hash("Password123!"),Role="Participant"});
        await db.SaveChangesAsync();
        var c=new AuthController(db,_passwords);
        var r=await c.Register(new RegisterRequest("A","C","a@b.com","Password123!","Participant",null));
        Assert.IsType<ConflictObjectResult>(r);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        await using var db=TestInfrastructure.CreateDb(Guid.NewGuid().ToString());
        var c=new AuthController(db,_passwords);
        var r=await c.Register(new RegisterRequest("A","B","a@b.com","Password123!","Admin",null));
        Assert.IsType<BadRequestObjectResult>(r);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        await using var db=TestInfrastructure.CreateDb(Guid.NewGuid().ToString());
        db.Users.Add(new User{FirstName="A",LastName="B",Email="a@b.com",PasswordHash=_passwords.Hash("Password123!"),Role="Participant"});
        await db.SaveChangesAsync();
        var c=new AuthController(db,_passwords); c.ControllerContext=new(){HttpContext=TestInfrastructure.Context()};
        var r=await c.Login(new LoginRequest("a@b.com","wrong"));
        Assert.IsType<UnauthorizedObjectResult>(r);
    }

    [Fact]
    public async Task Login_ValidCredentials_CreatesSession()
    {
        await using var db=TestInfrastructure.CreateDb(Guid.NewGuid().ToString());
        db.Users.Add(new User{FirstName="A",LastName="B",Email="a@b.com",PasswordHash=_passwords.Hash("Password123!"),Role="Organiser"});
        await db.SaveChangesAsync();
        var http=TestInfrastructure.Context(); var c=new AuthController(db,_passwords); c.ControllerContext=new(){HttpContext=http};
        var r=await c.Login(new LoginRequest("a@b.com","Password123!"));
        Assert.IsType<OkObjectResult>(r);
        Assert.Equal(1,http.Session.GetInt32("UserId"));
        Assert.Equal("Organiser",http.Session.GetString("Role"));
    }
}
