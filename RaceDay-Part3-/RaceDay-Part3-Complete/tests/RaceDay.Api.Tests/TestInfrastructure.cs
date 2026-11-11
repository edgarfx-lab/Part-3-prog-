using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public static class TestInfrastructure
{
    public static RaceDayDbContext CreateDb(string name)
    {
        var options=new DbContextOptionsBuilder<RaceDayDbContext>().UseInMemoryDatabase(name).Options;
        var db=new RaceDayDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
    public static DefaultHttpContext Context(int userId=0,string? role=null)
    {
        var c=new DefaultHttpContext();
        c.Session=new TestSession();
        if(userId>0)c.Session.SetInt32("UserId",userId);
        if(role is not null)c.Session.SetString("Role",role);
        return c;
    }
}
public sealed class TestSession : ISession
{
    private readonly Dictionary<string,byte[]> _data=new();
    public IEnumerable<string> Keys=>_data.Keys;
    public string Id=>"test";
    public bool IsAvailable=>true;
    public IEnumerable<string> Keys2=>_data.Keys;
    public void Clear()=>_data.Clear();
    public Task CommitAsync(CancellationToken cancellationToken=default)=>Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken=default)=>Task.CompletedTask;
    public void Remove(string key)=>_data.Remove(key);
    public void Set(string key,byte[] value)=>_data[key]=value;
    public bool TryGetValue(string key,out byte[]? value)=>_data.TryGetValue(key,out value);
}
