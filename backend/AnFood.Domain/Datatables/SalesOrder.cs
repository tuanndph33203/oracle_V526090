using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("SalesOrder")]
    public class SalesOrder
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string BranchCode { get; set; } = default!;

        [Required]
        [MaxLength(50)]
        public string OrderCode { get; set; } = default!;

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        public DateTime OpenTime { get; set; }

        public DateTime? PaymentTime { get; set; }

        public bool IncludesQrOrder { get; set; } = false;

        public Guid? MergedIntoOrderId { get; set; }
        public int? RatingStars { get; set; }
        [MaxLength(1000)]
        public string? RatingContent { get; set; }

        [MaxLength(50)]
        public string? CashierUserId { get; set; }

        [MaxLength(255)]
        public string? CashierUserName { get; set; }

        public Guid? CustomerId { get; set; }

        [MaxLength(50)]
        public string? CustomerCode { get; set; }

        [MaxLength(255)]
        public string? CustomerName { get; set; }

        public Guid? WaiterId { get; set; }

        [MaxLength(255)]
        public string? WaiterName { get; set; }

        [Required]
        public Guid FloorId { get; set; }

        [MaxLength(255)]
        public string? FloorName { get; set; }

        public int CustomerCount { get; set; }

        [Required]
        [MaxLength(50)]
        public string ServiceType { get; set; } = "DineIn"; // DineIn, TakeAway, Delivery

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "WaitingService"; // WaitingService, Serving, Reserved, WaitingPayment, Paid, Merged, Cancelled

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(500)]
        public string? DeliveryAddress { get; set; }

        [MaxLength(500)]
        public string? DeliveryNote { get; set; }

        [MaxLength(255)]
        public string? ShippingFeeNote { get; set; }

        [MaxLength(500)]
        public string? CancelReason { get; set; }

        [MaxLength(1000)]
        public string? Note { get; set; }

        public DateTime? CreatedOnDate { get; set; }
        [MaxLength(50)]
        public string? CreatedByUserCode { get; set; }
        [MaxLength(255)]
        public string? CreatedByUserName { get; set; }

        public DateTime? UpdatedOnDate { get; set; }
        [MaxLength(50)]
        public string? UpdatedByUserCode { get; set; }
        [MaxLength(255)]
        public string? UpdatedByUserName { get; set; }

        [MaxLength(50)]
        public string? SelectedPromotionCode { get; set; }

        [MaxLength(100)]
        public string? TherapyVoucherCode { get; set; }
    }
}
