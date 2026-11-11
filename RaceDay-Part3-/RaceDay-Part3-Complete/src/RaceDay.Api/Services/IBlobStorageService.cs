namespace RaceDay.Api.Services;

public interface IBlobStorageService
{
    bool IsConfigured { get; }
    Task<string> UploadAsync(IFormFile file, string containerName, CancellationToken cancellationToken = default);
}
