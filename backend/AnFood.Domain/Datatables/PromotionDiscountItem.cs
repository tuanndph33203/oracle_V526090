using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("PromotionDiscountItem")]
    public class PromotionDiscountItem
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PromotionId { get; set; }

        [Required]
        public Guid MenuItemId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinAmount { get; set; } = 0;

        public double MinQuantity { get; set; } = 0;
    }
}
