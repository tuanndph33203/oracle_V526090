using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("PreparationAreaBranch")]
    public class PreparationAreaBranch
    {
        [Key]
        public Guid Id { get; set; }

        public Guid PreparationAreaId { get; set; }

        public Guid BranchId { get; set; }
    }
}
