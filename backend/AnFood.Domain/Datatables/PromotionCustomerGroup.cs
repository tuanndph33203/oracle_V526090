using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("PromotionCustomerGroup")]
    public class PromotionCustomerGroup
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PromotionId { get; set; }

        [Required]
        public Guid CustomerGroupId { get; set; }
    }
}
