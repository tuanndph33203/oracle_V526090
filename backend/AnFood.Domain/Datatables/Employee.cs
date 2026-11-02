using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("Employee")]
    public class Employee
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = default!;

        [Required]
        public string Name { get; set; } = default!;

        [MaxLength(50)]
        public string? BranchCode { get; set; }

        [MaxLength(50)]
        public string? LeaderCode { get; set; }

        public DateTime? DateOfBirth { get; set; }
        public int Gender { get; set; }
        public string? CustomerCardId { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? BankAccount { get; set; }
        public string? Notes { get; set; }

        [MaxLength(50)]
        public string? CityCode { get; set; }

        [MaxLength(50)]
        public string? DistrictCode { get; set; }

        [MaxLength(50)]
        public string? WardCode { get; set; }

        [MaxLength(50)]
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
        public int? IsLoyalCustomer { get; set; }
        public int? IsSupplier { get; set; }
        public int? IsEmployee { get; set; }
        public int? IsCollaborator { get; set; }
        public DateTime? CreatedOnDate { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }
}
