using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("Districts")]
    public class Districts
    {
        [Key]
        public Guid Id { get; set; }
        [MaxLength(50)]
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public Guid CityId { get; set; }
        public bool IsDelete { get; set; }
    }
}
