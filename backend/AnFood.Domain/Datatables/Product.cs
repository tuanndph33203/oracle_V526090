using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("Product")]
    public class Product
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = default!;

        [Required]
        public string Name { get; set; } = default!;
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Time { get; set; }
        [Required]
        [MaxLength(50)]
        public string UnitCode { get; set; }

        public string? BarCode { get; set; } 
        public string? Origin { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; } // gi� mua 

        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; } // gi� b�n 

        public string ApplyListBranchCodes { get; set; }

        public string? Notes { get; set; }

        public string? ProductTypeCode { get; set; }

        public string? ProductGroupCode { get; set; }

        [Required]
        [MaxLength(50)]
        public string BranchCode { get; set; }

        public string? ImportTaxCode { get; set; } // thu? nh?p
        public string? SpecialTaxCode { get; set; } // Thu? d?c bi?t
        public string? ExportTaxCode { get; set; } // Thu? xu?t
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ExportTaxMoney { get; set; } // ti?n thu? xu?t
        public int Status { get; set; }
        public DateTime? CreatedOnDate { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
        public DateTime? UpdatedOnDate { get; set; }
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
        public string? Description { get; set; } = default!;
        public string? AvatarUrl { get; set; } = default!;
        public bool? IsHaveFood { get; set; }
        public int? Type { get; set; } // 0: s?n ph?m, 1: d?ch v?
    }
}
