using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("MenuItemBranch")]
    public class MenuItemBranch
    {
        [Key]
        public Guid Id { get; set; }

        public Guid MenuItemId { get; set; }

        public Guid BranchId { get; set; }
    }
}
