using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("PromotionGiftCondition")]
    public class PromotionGiftCondition
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PromotionId { get; set; }

        [Required]
        public Guid MenuItemId { get; set; }

        public double RequiredQuantity { get; set; } = 1;
    }
}
