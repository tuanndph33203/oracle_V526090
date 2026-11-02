using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("SalesOrderItem")]
    public class SalesOrderItem
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string BranchCode { get; set; } = default!;

        [Required]
        public Guid OrderId { get; set; }

        public int LineNo { get; set; }

        [Required]
        public Guid ItemId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ItemCode { get; set; } = default!;

        [Required]
        [MaxLength(255)]
        public string ItemName { get; set; } = default!;

        [MaxLength(50)]
        public string? UnitName { get; set; }

        public double Quantity { get; set; }

        public double ActualQuantity { get; set; }    // Số lượng thực tế đã phục vụ

        public double ReturnedQuantity { get; set; }  // Số lượng trả lại

        public double CancelledQuantity { get; set; } // Số lượng đã hủy

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public double DiscountRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        public double VatRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public int IsSentToKitchen { get; set; } // 0 = false, 1 = true
        public int IsReturned { get; set; } // 0 = false, 1 = true
        public int IsChecked { get; set; } // 0 = false, 1 = true
        public int IsCancelled { get; set; } // 0 = false, 1 = true

        [MaxLength(50)]
        public string KitchenStatus { get; set; } = "NotSent"; // NotSent, Sent, PartiallyServed, FullyServed, PartiallySoldOut, FullySoldOut, Cancelled

        public double SentQty { get; set; } = 0;
        public double ServedQty { get; set; } = 0;
        public double SoldOutQty { get; set; } = 0;
        public double RemainingQty { get; set; } = 0;

        [MaxLength(50)]
        public string? PromotionCode { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public string? SelectedOptionsJson { get; set; }

        public DateTime? CreatedOnDate { get; set; }
        public DateTime? UpdatedOnDate { get; set; }
    }
}
