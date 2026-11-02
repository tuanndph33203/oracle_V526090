namespace AnFood.Application.Bussiness.MinIoProcess
{
    public class MinioOptions
    {
        public string Endpoint { get; set; } = default!;
        public string AccessKey { get; set; } = default!;
        public string SecretKey { get; set; } = default!;
        public bool UseSSL { get; set; }
        public string Bucket { get; set; } = default!;
    }

    public class MinioResponseModel {
        public string ObjectName { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public string PresignedUrl { get; set; }
        public long FileSize { get; set; }

    }
    public sealed class UploadResult
    {
        public string OriginalFileName { get; set; } = default!;
        public string ObjectName { get; set; } = default!;
        public string ContentType { get; set; } = "application/octet-stream";
        public long Size { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? PresignedUrl { get; set; }
    }
}
