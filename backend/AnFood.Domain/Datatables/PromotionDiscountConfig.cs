using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("PromotionDiscountConfig")]
    public class PromotionDiscountConfig
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PromotionId { get; set; }

        [Required]
        public int DiscountScope { get; set; } // 1 = Order, 2 = Item

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountPercent { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal QrOrderDiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderAmount { get; set; } = 0;
    }
}
