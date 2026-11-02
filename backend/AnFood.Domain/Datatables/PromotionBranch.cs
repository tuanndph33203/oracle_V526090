using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("PromotionBranch")]
    public class PromotionBranch
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PromotionId { get; set; }

        [Required]
        public Guid BranchId { get; set; }
    }
}
