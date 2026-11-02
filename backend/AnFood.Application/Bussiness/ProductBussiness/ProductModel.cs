using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AnFood.Infrastructure;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.ProductBussiness
{
    public class ProductDetailModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? BarCode { get; set; }
        public string? Origin { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; }
        public string? ApplyListBranchCodesString { get; set; }
        public string? Notes { get; set; }

        public string? ImportTaxCode { get; set; }
        public string? SpecialTaxCode { get; set; }
        public string? ExportTaxCode { get; set; }
        public decimal? ExportTaxMoney { get; set; }

        public int Status { get; set; }
        public DateTime? CreatedOnDate { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
        public string? Description { get; set; } = default!;
        public string? AvatarUrl { get; set; } = default!;

        public string? ProductTypeName { get; set; }
        public string? ProductGroupName { get; set; }
        public string? BranchName { get; set; }
        public string? UnitName { get; set; }
        public int? Type { get; set; } // 0: product, 1: service
    }

    public class ProductModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? UnitCode { get; set; }
        public string? BarCode { get; set; }
        public string? Origin { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; }

        public string ApplyListBranchCodes { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? ProductTypeCode { get; set; }
        public string? ProductGroupCode { get; set; }
        public string? BranchCode { get; set; }

        public string? ImportTaxCode { get; set; }
        public string? SpecialTaxCode { get; set; }
        public string? ExportTaxCode { get; set; }
        public decimal? ExportTaxMoney { get; set; }

        public int Status { get; set; }
        public DateTime? CreatedOnDate { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
        public DateTime? UpdatedOnDate { get; set; }
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
        public string? Description { get; set; } = default!;
        public string? AvatarUrl { get; set; } = default!;
        public int? Type { get; set; }
    }

    public class CreateProductModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? UnitCode { get; set; }
        public string? BarCode { get; set; }
        public string? Origin { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; }

        public string? ApplyListBranchCodes { get; set; }
        public string? Notes { get; set; }
        public string? ProductTypeCode { get; set; }
        public string? ProductGroupCode { get; set; }
        public string? BranchCode { get; set; }

        public string? ImportTaxCode { get; set; }
        public string? SpecialTaxCode { get; set; }
        public string? ExportTaxCode { get; set; }
        public decimal? ExportTaxMoney { get; set; }

        public int Status { get; set; }
        public DateTime? CreatedOnDate { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
        public DateTime? UpdatedOnDate { get; set; }
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
        public string? Description { get; set; } = default!;
        public string? AvatarUrl { get; set; } = default!;
        public int? Type { get; set; }
    }

    public class UpdateProductModel : ProductModel
    {
    }

    public class ProductFilterModel : FilterModel
    {
        public string? BranchCode { get; set; }
        public int? Type { get; set; }
        public bool? IsProductExclusiveToBranch { get; set; }
        public List<string>? ProductType { get; set; }
        public List<string>? ProductGroup { get; set; }
    }
}
