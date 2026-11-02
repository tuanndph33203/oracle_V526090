using System;
using System.ComponentModel.DataAnnotations;

namespace AnFood.Domain.Datatables
{
    public class QrStaffCall
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(50)]
        public string BranchCode { get; set; } = default!;

        [Required, MaxLength(20)]
        public string Type { get; set; } = default!;

        public Guid TableId { get; set; }

        [Required, MaxLength(100)]
        public string TableName { get; set; } = default!;

        public DateTime CreatedAt { get; set; }
    }
}
