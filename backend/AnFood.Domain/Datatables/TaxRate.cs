using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("TaxRate")]
    public class TaxRate
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = default!;

        [Column(TypeName = "decimal(5,2)")]
        public decimal Rate { get; set; }

        public string? Description { get; set; }

        public int Status { get; set; } = 1; // 1 = Active, 2 = Inactive, 0 = Deleted

        public DateTime? CreatedOnDate { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
        public DateTime? UpdatedOnDate { get; set; }
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }
}
