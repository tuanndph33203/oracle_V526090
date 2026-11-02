using AnFood.Application.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using System.Collections.Concurrent;

namespace AnFood.Application.Bussiness.MinIoProcess
{
    public class MinioStorage : IMinioStorage
    {
        private readonly MinioClient _minio;
        private readonly MinioOptions _opt;
        private readonly IConfiguration _config;
        public MinioStorage(
            MinioClient minio,
            IOptions<MinioOptions> opt,
            IConfiguration config)
        {
            _minio = minio;
            _opt = opt.Value;
            _config = config;
        }

        public async Task EnsureBucketAsync(CancellationToken ct = default)
        {
            var bucket = _opt.Bucket;

            var exists = await _minio.BucketExistsAsync(new BucketExistsArgs()
                .WithBucket(bucket), ct);

            if (!exists)
            {
                await _minio.MakeBucketAsync(new MakeBucketArgs()
                    .WithBucket(bucket), ct);
            }
        }

        public async Task<string> UploadAsync(Stream stream, string objectName, string contentType, CancellationToken ct = default)
        {
            await EnsureBucketAsync(ct);

            
            if (!stream.CanSeek)
            {
                var ms = new MemoryStream();
                await stream.CopyToAsync(ms, ct);
                ms.Position = 0;
                stream = ms;
            }

            var putArgs = new PutObjectArgs()
                .WithBucket(_opt.Bucket)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType);

            await _minio.PutObjectAsync(putArgs, ct);

            return objectName;
        }

        public async Task<(Stream Stream, string ContentType)> DownloadAsync(string objectName, CancellationToken ct = default)
        {
            await EnsureBucketAsync(ct);

            string contentType = "application/octet-stream";
            var ms = new MemoryStream();

           
            var stat = await _minio.StatObjectAsync(new StatObjectArgs()
                .WithBucket(_opt.Bucket)
                .WithObject(objectName), ct);

            contentType = stat.ContentType ?? contentType;

            var getArgs = new GetObjectArgs()
                .WithBucket(_opt.Bucket)
                .WithObject(objectName)
                .WithCallbackStream(s => s.CopyTo(ms));

            await _minio.GetObjectAsync(getArgs, ct);

            ms.Position = 0;
            return (ms, contentType);
        }

        public async Task DeleteAsync(string objectName, CancellationToken ct = default)
        {
            await _minio.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(_opt.Bucket)
                .WithObject(objectName), ct);
        }

        public async Task<string> PresignedGetUrlAsync(string objectName, int expirySeconds = 3600, CancellationToken ct = default)
        {
            await EnsureBucketAsync(ct);

            var args = new PresignedGetObjectArgs()
                .WithBucket(_opt.Bucket)
                .WithObject(objectName)
                .WithExpiry(expirySeconds);

            return await _minio.PresignedGetObjectAsync(args);
        }

        public async Task<List<UploadResult>> UploadManyAsync(
            IEnumerable<IFormFile> files,
            string? folderPrefix = null,
            int maxConcurrency = 3,
            bool returnPresignedUrl = true,
            int presignExpirySeconds = 3600,
            CancellationToken ct = default)
        {
            if (files == null) return new List<UploadResult>();

            var list = files.Where(f => f != null && f.Length > 0).ToList();
            if (list.Count == 0) return new List<UploadResult>();

            await EnsureBucketAsync(ct);

            folderPrefix = (folderPrefix ?? "").Trim().Trim('/');
            var results = new ConcurrentBag<UploadResult>();
            using var gate = new SemaphoreSlim(Math.Max(1, maxConcurrency));

            var tasks = list.Select(async file =>
            {
                await gate.WaitAsync(ct);
                try
                {
                    var ext = Path.GetExtension(file.FileName);
                    var name = Path.GetFileNameWithoutExtension(file.FileName);

                    var safeName = HelperProcess.Slugify(name);
                    var shortId = Guid.NewGuid().ToString("N")[..6];

                    var objectNameCore =
                        $"{DateTime.UtcNow:yyyy/MM/dd}/{safeName}-{shortId}{ext}";
                    var objectName = string.IsNullOrEmpty(folderPrefix)
                        ? objectNameCore
                        : $"{folderPrefix}/{objectNameCore}";

                    var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                        ? "application/octet-stream"
                        : file.ContentType;

                    await using var stream = file.OpenReadStream();

                    await UploadAsync(stream, objectName, contentType, ct);

                    string? url = null;
                    if (returnPresignedUrl)
                        url = await PresignedGetUrlAsync(objectName, presignExpirySeconds, ct);

                    results.Add(new UploadResult
                    {
                        OriginalFileName = file.FileName,
                        ObjectName = "https://" + _config["Minio:Endpoint"] + "/" + _config["Minio:Bucket"] + "/"+ objectName,
                        ContentType = contentType,
                        Size = file.Length,
                        Success = true,
                        PresignedUrl = url
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new UploadResult
                    {
                        OriginalFileName = file.FileName,
                        ObjectName = "",
                        ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                        Size = file.Length,
                        Success = false,
                        Error = ex.Message
                    });
                }
                finally
                {
                    gate.Release();
                }
            });

            await Task.WhenAll(tasks);
            return results.ToList();
        }
    }
}
