using AnFood.Application.Bussiness.BranchBussiness;
using AnFood.Application.Bussiness.CityBussiness;
using AnFood.Application.Bussiness.DistrictBussiness;
using AnFood.Application.Bussiness.WardBussiness;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.EmployeeBussiness
{
    public class EmployeeDetailModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public BranchModel? Branch { get; set; }
        public EmployeeModel? Leader { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int Gender { get; set; }
        public string? CustomerCardId { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? BankAccount { get; set; }
        public string? Notes { get; set; }
        public string? CityCode { get; set; }
        public string? DistrictCode { get; set; }
        public string? WardCode { get; set; }
        public CityModel? City { get; set; }
        public DistrictModel? District { get; set; }
        public WardModel? Ward { get; set; }
        public string? CommissionGroupCode { get; set; }
        public int Status { get; set; }
        public List<BranchModel>? ApplyBranchCodes { get; set; }
        public string? ApplyListBranchCodesString { get; set; }
        public int? IsSales { get; set; }
        public int? IsCashier { get; set; }
        public int? IsDelivery { get; set; }
        public int? IsWarehouse { get; set; }
        public int? IsTechnician { get; set; }
        public int? DutyType { get; set; }
        public string? UserId { get; set; }
        public int? IsLoyalCustomer { get; set; }
        public int? IsSupplier { get; set; }
        public int? IsEmployee { get; set; }
        public int? IsCollaborator { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
        public DateTime? CreatedOnDate { get; set; }
    }

    public class EmployeeModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? BranchCode { get; set; }
        public string? LeaderCode { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int Gender { get; set; }
        public string? CustomerCardId { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? BankAccount { get; set; }
        public string? Notes { get; set; }
        public string? CityCode { get; set; }
        public string? DistrictCode { get; set; }
        public string? WardCode { get; set; }
        public string? CommissionGroupCode { get; set; }
        public int Status { get; set; }
        public string? ApplyBranchCodes { get; set; }
        public int? IsSales { get; set; }
        public int? IsCashier { get; set; }
        public int? IsDelivery { get; set; }
        public int? IsWarehouse { get; set; }
        public int? IsTechnician { get; set; }
        public int? DutyType { get; set; }
        public string? UserId { get; set; }
        public int? IsCustomer { get; set; }
        public int? IsSupplier { get; set; }
        public int? IsEmployee { get; set; }
        public int? IsCollaborator { get; set; }
    }

    public class CreateEmployeeModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? BranchCode { get; set; }
        public string? LeaderCode { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int Gender { get; set; }
        public string? CustomerCardId { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? BankAccount { get; set; }
        public string? Notes { get; set; }
        public string? CityCode { get; set; }
        public string? DistrictCode { get; set; }
        public string? WardCode { get; set; }
        public string? CommissionGroupCode { get; set; }
        public string? ApplyBranchCodes { get; set; }
        public int? IsSales { get; set; }
        public int? IsCashier { get; set; }
        public int? IsDelivery { get; set; }
        public int? IsWarehouse { get; set; }
        public int? IsTechnician { get; set; }
        public int? DutyType { get; set; }
        public string? UserId { get; set; }
        public int? IsCustomer { get; set; }
        public int? IsSupplier { get; set; }
        public int? IsEmployee { get; set; }
        public int? IsCollaborator { get; set; }
        public string? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class UpdateEmployeeModel : EmployeeModel
    {
        public string? UpdatedByUserId { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class EmployeeFilterModel : FilterModel
    {
        public int? DutyType { get; set; }
        public string? UserId { get; set; }
        public int? IsEmployee { get; set; }
        public int? IsCustomer { get; set; }
        public int? IsSupplier { get; set; }
        public int? IsCollaborator { get; set; }
    }
}
