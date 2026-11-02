using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("TableQrOrderToken")]
    public class TableQrOrderToken
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid TableId { get; set; }

        [Required]
        public Guid BranchId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Token { get; set; } = default!;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOnDate { get; set; } = DateTime.Now;
        public string? CreatedByUserCode { get; set; }
        public DateTime? UpdatedOnDate { get; set; }
        public string? UpdatedByUserCode { get; set; }
    }
}
