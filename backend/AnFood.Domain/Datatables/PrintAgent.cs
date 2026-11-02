using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("PrintAgent")]
    public class PrintAgent
    {
        [Key]
        public Guid Id { get; set; }

        public Guid BranchId { get; set; }

        [Required]
        [MaxLength(255)]
        public string MachineName { get; set; } = default!;

        public DateTime LastSeenAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
