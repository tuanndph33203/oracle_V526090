using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("PromotionWeekDay")]
    public class PromotionWeekDay
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PromotionId { get; set; }

        [Required]
        public int DayOfWeek { get; set; } // 0 = Sunday, 1 = Monday, ..., 6 = Saturday
    }
}
