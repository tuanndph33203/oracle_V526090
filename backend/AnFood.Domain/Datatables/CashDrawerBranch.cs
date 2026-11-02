using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("CashDrawerBranch")]
    public class CashDrawerBranch
    {
        [Key]
        public Guid Id { get; set; }

        public Guid CashDrawerId { get; set; }

        public Guid BranchId { get; set; }
    }
}
