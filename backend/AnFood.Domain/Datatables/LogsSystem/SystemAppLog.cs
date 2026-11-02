using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnFood.Domain.Datatables.LogsSystem
{
    [Table("SystemAppLog")]
    public class SystemAppLog
    {
        [Key]
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ServiceName { get; set; } = null!;
        public string Environment { get; set; } = null!;
        public string Level { get; set; } = null!;
        public string? LogType { get; set; }
        public string? RequestId { get; set; }
        public string Message { get; set; } = null!;
        public string? Exception { get; set; }
        public string? DataJson { get; set; }

    }
}
