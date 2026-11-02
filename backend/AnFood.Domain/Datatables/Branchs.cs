using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("Branchs")]
    public class Branchs
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(50)]
        public string Code { get; set; } = default!;

        public string Name { get; set; } = default!;

        [MaxLength(50)]
        public string CityCode { get; set; } = default!;

        [MaxLength(50)]
        public string? DistrictCode { get; set; }

        [MaxLength(50)]
        public string? WardCode { get; set; }

        public string? Address { get; set; }
        public string? MapUrl { get; set; }
        public string? Phone { get; set; }
        public string? Notes { get; set; }
        public string? Email { get; set; }
        public int Status { get; set; }
        public DateTime? CreatedOnDate { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }
}
