
using AnFood.Application.Bussiness.MinIoProcess;
using AnFood.Application.Helper;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AnFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MinIOController : ControllerBase
    {
        private readonly IMinioStorage _storage;
        private readonly IConfiguration _config;
        public MinIOController(IMinioStorage storage, IConfiguration config)
        {
            _storage = storage;
            _config = config;
        }
        [HttpPost("upload-many")]
        [RequestSizeLimit(500_000_000)]
        public async Task<Response> UploadMany( [FromForm] List<IFormFile> files,  [FromQuery] string? folder,CancellationToken ct)
        {
            var res = await _storage.UploadManyAsync(
                files,
                folderPrefix: folder,    
                maxConcurrency: 3,
                returnPresignedUrl: true,
                presignExpirySeconds: 3600,
                ct: ct);

            return new ResponseList<UploadResult>(res, "Succes", Code.Success);
        }

        [HttpPost("upload")]
        [RequestSizeLimit(200_000_000)] 
        public async Task<Response> Upload(IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0) return new Response(Code.NotFound, "File empty");

            var ext = Path.GetExtension(file.FileName);
            var name = Path.GetFileNameWithoutExtension(file.FileName);

            var safeName = HelperProcess.Slugify(name);
            var shortId = Guid.NewGuid().ToString("N")[..6]; 

            var objectName =
                $"{DateTime.UtcNow:yyyy/MM/dd}/{safeName}-{shortId}{ext}";

            var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType;

            await using var stream = file.OpenReadStream();
            await _storage.UploadAsync(stream, objectName, contentType, ct);

            var url = await _storage.PresignedGetUrlAsync(objectName, 3600, ct);

            var res = new UploadResult()
            {
                OriginalFileName = file.FileName,
                ObjectName = "https://" + _config["Minio:Endpoint"] + "/" + _config["Minio:Bucket"] + "/" +  objectName,

                ContentType = contentType,
                Size = file.Length,
                Success = true,
                PresignedUrl = url
            };

            return new ResponseObject<UploadResult>(res, "Success", Code.Success);
        }

        [HttpGet("download")]
        public async Task<IActionResult> Download([FromQuery] string objectName, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(objectName)) return BadRequest("objectName required");

            var (stream, contentType) = await _storage.DownloadAsync(objectName, ct);
            var downloadName = Path.GetFileName(objectName);

            return File(stream, contentType, downloadName);
        }

        [HttpDelete]
        public async Task<Response> Delete([FromQuery] string objectName, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(objectName)) return new Response(Code.NotFound, "objectName required");

            await _storage.DeleteAsync(objectName, ct);
            return new Response(Code.Success, "Success");
        }
    }
}
