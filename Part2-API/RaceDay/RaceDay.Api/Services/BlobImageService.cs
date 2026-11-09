using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public class BlobImageService
{
    private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly BlobContainerClient _container;
    private bool _ready;

    public BlobImageService(IConfiguration config)
    {
        var connectionString = config["BlobStorage:ConnectionString"] ?? "UseDevelopmentStorage=true";
        var containerName = config["BlobStorage:ContainerName"] ?? "raceday-images";
        _container = new BlobContainerClient(connectionString, containerName);
    }

    /// <summary>Returns an error message if the file is not an acceptable image, otherwise null.</summary>
    public static string? Validate(IFormFile? file)
    {
        if (file == null || file.Length == 0) return "Please choose an image file.";
        if (file.Length > MaxBytes) return "The image must be 5 MB or smaller.";
        if (!AllowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            return "Only JPG, PNG, WEBP or GIF images are allowed.";
        return null;
    }

    /// <summary>Uploads the image to Blob Storage and returns its public URL.</summary>
    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        if (!_ready)
        {
            await _container.CreateIfNotExistsAsync();
            await _container.SetAccessPolicyAsync(PublicAccessType.Blob);
            _ready = true;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blob = _container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{extension}");

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        return blob.Uri.ToString();
    }
}