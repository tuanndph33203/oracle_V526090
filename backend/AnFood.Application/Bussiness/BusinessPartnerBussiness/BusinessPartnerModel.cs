using System;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.BusinessPartnerBussiness
{
    public class BusinessPartnerModel
    {
        public Guid Id { get; set; }
        public int PartnerTypes { get; set; } // 1 = Customer, 2 = Supplier, 3 = Both
        public string? Code { get; set; }
        public string? Name { get; set; }
        public Guid? PartnerGroupId { get; set; }
        public string? PartnerGroupName { get; set; }
        public string? IdentityNumber { get; set; }
        public string? MobilePhone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Gender { get; set; }
        public string? CompanyName { get; set; }
        public string? TaxCode { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Note { get; set; }
        public int Status { get; set; }
    }

    public class BusinessPartnerCreateModel
    {
        public int PartnerTypes { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public Guid? PartnerGroupId { get; set; }
        public string? IdentityNumber { get; set; }
        public string? MobilePhone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Gender { get; set; }
        public string? CompanyName { get; set; }
        public string? TaxCode { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Note { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class BusinessPartnerUpdateModel : BusinessPartnerModel
    {
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class BusinessPartnerFilterModel : FilterModel
    {
        public int? PartnerTypes { get; set; }
        public Guid? PartnerGroupId { get; set; }
        public int? Gender { get; set; }
        public int? Status { get; set; }
    }
}