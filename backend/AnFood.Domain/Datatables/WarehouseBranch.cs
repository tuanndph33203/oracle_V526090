using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("WarehouseBranch")]
    public class WarehouseBranch
    {
        [Key]
        public Guid Id { get; set; }

        public Guid WarehouseId { get; set; }

        public Guid BranchId { get; set; }
    }
}
