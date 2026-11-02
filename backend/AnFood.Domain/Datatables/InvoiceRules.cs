using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("InvoiceRules")]
    public class InvoiceRules
    {
        [Key]
        [MaxLength(50)]
        public string RuleCode { get; set; } = default!;

        [Required]
        [MaxLength(255)]
        public string RuleName { get; set; } = default!;

        [Required]
        [MaxLength(5)]
        public string EndOfDayCutoffTime { get; set; } = "0";

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
