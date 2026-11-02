using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("Invoices")]
    public class Invoices
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OrderId { get; set; }

        [MaxLength(50)]
        public string? InvoiceNo { get; set; }

        [MaxLength(50)]
        public string? InvoiceSymbol { get; set; }

        public int BuyerType { get; set; } // 1: Enterprise, 2: Individual

        [MaxLength(50)]
        public string? TaxCode { get; set; }

        [MaxLength(255)]
        public string? CompanyName { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = default!;

        [MaxLength(50)]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string? IdentityCard { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount { get; set; }

        public int Status { get; set; } // 0: Pending, 1: Processing, 2: Issued, 3: Failed, 4: Manual Processing (Xử lý thủ công)

        public DateTime? IssuedDate { get; set; }

        [MaxLength(100)]
        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        [MaxLength(100)]
        public string? TransactionId { get; set; }

        [MaxLength(500)]
        public string? PdfUrl { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
