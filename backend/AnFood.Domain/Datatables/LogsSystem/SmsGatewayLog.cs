using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables.LogsSystem
{
    [Table("SmsGatewayLogs")]
    public class SmsGatewayLog
    {
        [Key]
        public Guid Id { get; set; }
        public string MessageType { get; set; } = null!;
        public string? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
        public string? TargetPhone { get; set; }
        public string? MessageBody { get; set; }
        public string? RequestPayloadJson { get; set; }
        public string? GatewayBaseUrl { get; set; }
        public string? GatewayEndpoint { get; set; }
        public DateTime SentAtUtc { get; set; }
        public int? HttpStatusCode { get; set; }
        public string? ResponsePayloadJson { get; set; }
        public DateTime? ReceivedAtUtc { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public long? DurationMs { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
