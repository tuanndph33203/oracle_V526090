using Microsoft.AspNetCore.Http;

namespace AnFood.Application.Bussiness.MinIoProcess
{
    public interface IMinioStorage
    {
        Task EnsureBucketAsync(CancellationToken ct = default);
        Task<string> UploadAsync(Stream stream, string objectName, string contentType, CancellationToken ct = default);
        Task<(Stream Stream, string ContentType)> DownloadAsync(string objectName, CancellationToken ct = default);
        Task DeleteAsync(string objectName, CancellationToken ct = default);
        Task<string> PresignedGetUrlAsync(string objectName, int expirySeconds = 3600, CancellationToken ct = default);
        public Task<List<UploadResult>> UploadManyAsync(
        IEnumerable<IFormFile> files,
        string? folderPrefix = null,
        int maxConcurrency = 3,
        bool returnPresignedUrl = true,
        int presignExpirySeconds = 3600,
        CancellationToken ct = default);
    }
}
