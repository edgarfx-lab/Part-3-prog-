using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace RaceDay.Api.Services;

public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly string? _connectionString;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        _connectionString = configuration["AzureBlobStorage:ConnectionString"];
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    public async Task<string> UploadAsync(IFormFile file, string containerName, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Azure Blob Storage is not configured. Set AzureBlobStorage:ConnectionString.");

        var extension = Path.GetExtension(Path.GetFileName(file.FileName));
        var safeName = $"{Guid.NewGuid():N}{extension}";
        var container = new BlobContainerClient(_connectionString, containerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(safeName);
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType }, cancellationToken: cancellationToken);

        if (!blob.CanGenerateSasUri)
            throw new InvalidOperationException("The Azure Storage connection string must include an account key to generate read-only image links.");

        var sas = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = safeName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddDays(30)
        };
        sas.SetPermissions(BlobSasPermissions.Read);
        return blob.GenerateSasUri(sas).ToString();
    }
}
