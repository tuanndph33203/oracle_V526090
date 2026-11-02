using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.BranchBussiness
{
    public class BranchDetailModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string CityCode { get; set; } = default!;
        public string? DistrictCode { get; set; }
        public string? WardCode { get; set; }
        public string? Address { get; set; }
        public string? MapUrl { get; set; }
        public string? Phone { get; set; }
        public string? Notes { get; set; }
        public string? Email { get; set; }
        public int? Status { get; set; }
    }

    public class BranchModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string CityCode { get; set; } = default!;
        public string? DistrictCode { get; set; }
        public string? WardCode { get; set; }
        public string? Address { get; set; }
        public string? MapUrl { get; set; }
        public string? Phone { get; set; }
        public string? Notes { get; set; }
        public string? Email { get; set; }
        public int Status { get; set; }
    }

    public class BranchCreateModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string CityCode { get; set; } = default!;
        public string? DistrictCode { get; set; }
        public string? WardCode { get; set; }
        public string? Address { get; set; }
        public string? MapUrl { get; set; }
        public string? Phone { get; set; }
        public string? Notes { get; set; }
        public string? Email { get; set; }
        public string? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class BranchUpdateModel : BranchModel
    {
        public string? UpdatedByUserId { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class BranchFilterModel : FilterModel
    {
    }
}
