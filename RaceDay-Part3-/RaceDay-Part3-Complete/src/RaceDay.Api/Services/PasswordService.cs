using Microsoft.AspNetCore.Identity;

namespace RaceDay.Api.Services;
public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<object> _hasher = new();
    public string Hash(string password) => _hasher.HashPassword(new object(), password);
    public bool Verify(string password, string hash) =>
        _hasher.VerifyHashedPassword(new object(), hash, password) != PasswordVerificationResult.Failed;
}
