using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("MenuItem")]
    public class MenuItem
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = default!;

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = default!;

        public int ItemType { get; set; }

        public int CourseOrder { get; set; }

        public Guid? MenuGroupId { get; set; }

        public Guid? UnitId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice { get; set; }

        public Guid? TaxRateId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FixedTaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }

        public string? Description { get; set; }
        public string? ImageUrl { get; set; }

        public string? OptionGroupsJson { get; set; }

        public Guid? PreparationAreaId { get; set; }

        public int Status { get; set; } = 1; // 1 = Active, 2 = Inactive, 0 = Deleted

        public bool IsAvailable { get; set; } = true;
        public bool IsMustTry { get; set; } = false;
        public bool IsTrackStock { get; set; } = false;
        public int? StockQuantity { get; set; }
        public int? DailyQuota { get; set; }
        public DateTime? DisabledAt { get; set; }

        [NotMapped]
        public double TaxRatePercent { get; set; }

        [NotMapped]
        public string? UnitName { get; set; }

        public DateTime? CreatedOnDate { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
        public DateTime? UpdatedOnDate { get; set; }
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }
}
