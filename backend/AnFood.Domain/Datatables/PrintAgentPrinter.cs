using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("PrintAgentPrinter")]
    public class PrintAgentPrinter
    {
        [Key]
        public Guid Id { get; set; }

        public Guid AgentId { get; set; }

        [Required]
        [MaxLength(255)]
        public string PrinterName { get; set; } = default!;

        public bool IsAvailable { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
