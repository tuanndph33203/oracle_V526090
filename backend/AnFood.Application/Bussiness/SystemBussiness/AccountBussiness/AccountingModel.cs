using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AnFood.Application.Bussiness.BranchBussiness;
using AnFood.Application.Bussiness.EmployeeBussiness;
using AnFood.Application.Bussiness.CityBussiness;
using AnFood.Application.Bussiness.DistrictBussiness;
using AnFood.Application.Bussiness.WardBussiness;

namespace AnFood.Application.Bussiness.SystemBussiness.AccountBussiness
{
    public class UpdateAccountModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;
        [Required]
        public List<Guid> RoleIds { get; set; } = null!;
        public Guid UserId { get; set; }
        public string? ApplyBranchCodes { get; set; }
        public string? UserCode { get; set; }
        public string? LastName { get; set; } = "";
        public string? FirstName { get; set; } = "";
        public string? PhoneNumber { get; set; }
    }

    public class CreateAccountSsoModel
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        [Required, EmailAddress]
        public string Email { get; set; } = null!;
        public string? Password { get; set; } = "CafeAdmin123#";
        public string? ConfirmPassword { get; set; } = "CafeAdmin123#";
        [Required]
        public string MailWork { get; set; } = null!;
        public Guid? ApplicationId { get; set; }
        public List<Guid>? RoleIds { get; set; }
        public string ApplyBranchCodes { get; set; } = "00";
        public string UserCode { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class CreateAccountModel
    {
        public string? UserCode { get; set; }
        public string? UserName { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; } = "CafeAdmin123#";
        public DateTime? DateOfBirth { get; set; }
        public int? Gender { get; set; }
        public string? PhoneNumber { get; set; }
        public string? NumberId { get; set; }
        public string? Address { get; set; }
        public string? ApplyBranchCodes { get; set; }
        public string? BranchCode { get; set; }
        public int? Active { get; set; }
        public List<Guid>? RoleIds { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
        public bool IsEmployeeCodeEx { get; set; }
        public string? CityCode { get; set; }
        public string? DistrictCode { get; set; }
        public string? WardCode { get; set; }
        public string? LeaderCode { get; set; }
        public int? IsSales { get; set; }
        public int? IsCashier { get; set; }
        public int? IsDelivery { get; set; }
        public int? IsWarehouse { get; set; }
        public int? IsTechnician { get; set; }
    }

    public class ChangePasswordModel
    {
        public string Email { get; set; } = null!;
        public string OldPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }

    public class GetAllAccountSsoResponse
    {
        public List<AccountSsoDto>? Data { get; set; }
        public int Code { get; set; }
        public string? Message { get; set; }
        public int TotalTime { get; set; }
        public int? DataCount { get; set; }
        public string? Status { get; set; }
        public int? TotalCount { get; set; }
    }

    public class AccountSsoDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? UserId { get; set; }
        public string? MailWork { get; set; }
        public Guid? ApplicationId { get; set; }
        public List<string>? ApplyBranchCodes { get; set; }
        public List<RoleSsoDto>? Roles { get; set; }
        public string? UserCode { get; set; }
        public bool? IsActive { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class RoleSsoDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public Guid ApplicationId { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public List<Guid>? PermissionIds { get; set; }
    }

    public class GetDetailAccountSsoResponse
    {
        public DetailAccountSsoResponse? Data { get; set; }
        public int Code { get; set; }
        public string? Message { get; set; }
        public int TotalTime { get; set; }
        public int? DataCount { get; set; }
        public string? Status { get; set; }
        public int? TotalCount { get; set; }
    }

    public class DetailAccountSsoResponse
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public List<RoleModel>? Roles { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string MailWork { get; set; } = null!;
        public string? UserCode { get; set; }
        public List<string> ApplyBranchCodes { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public bool? IsActive { get; set; }
    }

    public class RoleModel
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
        public string? Code { get; set; }
    }

    public class DetailAccountModel
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public List<RoleModel>? Roles { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public DateTime? DateOfBirth { get; set; }
        public int Gender { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public int Status { get; set; }
        public List<string>? ApplyListBranchCodesString { get; set; }

        public BranchModel? Branch { get; set; }
        public EmployeeModel? Leader { get; set; }
        public string? CustomerCardId { get; set; }
        public string? BankAccount { get; set; }
        public string? Notes { get; set; }
        public CityModel? City { get; set; }
        public DistrictModel? District { get; set; }
        public WardModel? Ward { get; set; }
        public List<BranchModel>? ApplyBranchCodes { get; set; }
        public int? IsSales { get; set; }
        public int? IsCashier { get; set; }
        public int? IsDelivery { get; set; }
        public int? IsWarehouse { get; set; }
        public int? IsTechnician { get; set; }
    }
}
