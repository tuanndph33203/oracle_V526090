using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("SalesOrderTable")]
    public class SalesOrderTable
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string BranchCode { get; set; } = default!;

        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public Guid TableId { get; set; }

        [Required]
        [MaxLength(50)]
        public string TableCode { get; set; } = default!;

        [Required]
        [MaxLength(255)]
        public string TableName { get; set; } = default!;

        [Required]
        public Guid FloorId { get; set; }

        public DateTime CreatedOnDate { get; set; } = DateTime.UtcNow;
    }
}
