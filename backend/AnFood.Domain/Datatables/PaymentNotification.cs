using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("PaymentNotifications")]
    public class PaymentNotification
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string BranchCode { get; set; } = default!;

        [Required]
        [MaxLength(100)]
        public string TransactionId { get; set; } = default!;

        [MaxLength(50)]
        public string? DocumentNumber { get; set; }

        [Required]
        public string Message { get; set; } = default!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
