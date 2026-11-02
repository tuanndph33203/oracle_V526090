namespace AnFood.Application.Contant
{
    public class FilterModel
    {
        public int? Page { get; set; } = 1;
        public int? Size { get; set; } = 20;
        public string? TextSearch { get; set; } = null!;
        public bool? IsGetDeleted { get; set; }
        public bool? IsInactive { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? BranchCode { get; set; }
        public List<string>? TransTypeCode { get; set; }

    }
    public enum AuditAction
    {
        Create,
        Update,
        Delete,
        Other
    }

    public class DocumentCodeConst
    {
        public const string CT = "CT1";
        public const string HDS = "HDS";
        public const string HDD = "HDD";
        public const string HDF = "HDF";
        public const string XQ1 = "XQ1";
        public const string SR1 = "SR1";
        public const string NN1 = "NN1";
    }

    public class MaLoaiGDConst {
        public const string TheChinh = "PK_01";
        public const string NapThe = "PK_02";
        public const string SanPham = "SP_05";
        public const string TheTang = "PK_03";
    }
    public class PaymentCardTypeConst
    {
        public const int TheChinh = 1;
        public const int TheTang = 2;
        public const int TheGiay = 3;
    }
}
