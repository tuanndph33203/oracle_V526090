using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnFood.Domain.Datatables.LogsSystem
{
    [Table("SystemAuditLog")]
    public class SystemAuditLog
    {
        [Key]
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ServiceName { get; set; } = null!;
        public string Environment { get; set; } = null!;
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string ActionName { get; set; } = null!;
        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? DetailsJson { get; set; }
    }

}
