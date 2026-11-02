using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("BranchConfigs")]
    public class BranchConfigs
    {
        [Key]
        [MaxLength(50)]
        public string BranchCode { get; set; } = default!;

        [MaxLength(255)]
        public string? InvoiceAccount { get; set; }

        [MaxLength(255)]
        public string? InvoicePassword { get; set; }

        [MaxLength(50)]
        public string? InvoiceSymbol { get; set; }

        [MaxLength(50)]
        public string? TaxCode { get; set; }

        [MaxLength(500)]
        public string? MisaAppKey { get; set; }

        public bool IsApplied { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
