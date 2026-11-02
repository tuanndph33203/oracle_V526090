using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("DiningTable")]
    public class DiningTable
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = default!;

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = default!;

        [Required]
        public Guid FloorPlanId { get; set; }

        [Required]
        public Guid BranchId { get; set; }

        public int Capacity { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Rotate { get; set; }
        public int SortOrder { get; set; }

        public int Status { get; set; } = 1; // 1 = Active, 2 = Inactive, 0 = Deleted

        public DateTime? CreatedOnDate { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
        public DateTime? UpdatedOnDate { get; set; }
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }
}
