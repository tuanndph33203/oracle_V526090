using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("SalesOrderPayment")]
    public class SalesOrderPayment
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string BranchCode { get; set; } = default!;

        [Required]
        public Guid OrderId { get; set; }

        [MaxLength(255)]
        public string? TransactionId { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentType { get; set; } = default!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaymentAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashReceivedAmount { get; set; } // Tiền khách đưa

        [Column(TypeName = "decimal(18,2)")]
        public decimal ChangeAmount { get; set; }       // Tiền thối lại

        public DateTime CreatedOnDate { get; set; } = DateTime.UtcNow;
    }
}
