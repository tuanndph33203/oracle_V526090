using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("InvoiceDetails")]
    public class InvoiceDetails
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid InvoiceId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ItemCode { get; set; } = default!;

        [Required]
        [MaxLength(255)]
        public string ItemName { get; set; } = default!;

        [MaxLength(50)]
        public string? UnitName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } // Net unit price (already discounted)

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } // Quantity * UnitPrice (before VAT)

        [Column(TypeName = "decimal(18,2)")]
        public decimal VatRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
    }
}
