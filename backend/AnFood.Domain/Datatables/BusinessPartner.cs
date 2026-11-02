using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("BusinessPartner")]
    public class BusinessPartner
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public int PartnerTypes { get; set; } // 1 = Customer, 2 = Supplier, 3 = Both (Flags/Combo)

        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(255)]
        public string? Name { get; set; }

        public Guid? PartnerGroupId { get; set; }

        [MaxLength(50)]
        public string? IdentityNumber { get; set; }

        [MaxLength(50)]
        public string? MobilePhone { get; set; }

        public DateTime? DateOfBirth { get; set; }
        
        public int? Gender { get; set; } // 1 = Nam, 2 = Nữ, 3 = Khác

        [MaxLength(255)]
        public string? CompanyName { get; set; }

        [MaxLength(50)]
        public string? TaxCode { get; set; }

        [MaxLength(255)]
        public string? Email { get; set; }

        public string? Address { get; set; }
        public string? Note { get; set; }

        public int Status { get; set; } // 1 = Active, 2 = Inactive, 0 = Deleted

        public DateTime? CreatedOnDate { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
        public DateTime? UpdatedOnDate { get; set; }
        public string? UpdatedByUserCode { get; set; }
    }
}
