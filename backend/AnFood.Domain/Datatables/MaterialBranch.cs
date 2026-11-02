using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("MaterialBranch")]
    public class MaterialBranch
    {
        [Key]
        public Guid Id { get; set; }

        public Guid MaterialId { get; set; }

        public Guid BranchId { get; set; }
    }
}
