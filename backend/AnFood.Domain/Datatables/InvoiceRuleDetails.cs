using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("InvoiceRuleDetails")]
    public class InvoiceRuleDetails
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string RuleCode { get; set; } = default!;

        [Required]
        [MaxLength(50)]
        public string PaymentType { get; set; } = default!; // Cash, BankTransfer, Card

        public int HasDiscount { get; set; } // 0: Ignore (Không xét), 1: Has discount (Có), 2: No discount (Không)

        public bool IncludeInInvoiceData { get; set; }

        public bool AutoIssueInvoice { get; set; }

        public bool IsActive { get; set; }
    }
}
