using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("PromotionGiftItem")]
    public class PromotionGiftItem
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PromotionId { get; set; }

        [Required]
        public Guid MenuItemId { get; set; }

        public double GiftQuantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal GiftValue { get; set; } = 0;
    }
}
