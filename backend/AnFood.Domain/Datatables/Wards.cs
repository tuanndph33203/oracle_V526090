using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("Wards")]
    public class Wards
    {
        [Key]
        public Guid Id { get; set; }
        [MaxLength(50)]
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public Guid CityId { get; set; }
        public bool IsDelete { get; set; }
        public DateTime? CreatedOnDate { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }
}
