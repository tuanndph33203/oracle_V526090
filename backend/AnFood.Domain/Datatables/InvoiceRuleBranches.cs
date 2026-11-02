using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("InvoiceRuleBranches")]
    public class InvoiceRuleBranches
    {
        [Key]
        [Column(Order = 0)]
        [MaxLength(50)]
        public string RuleCode { get; set; } = default!;

        [Key]
        [Column(Order = 1)]
        [MaxLength(50)]
        public string BranchCode { get; set; } = default!;
    }
}
