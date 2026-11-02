using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("Unit")]
    public class Unit
    {
        [Key]           
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = default!;

        [Required]
        public string Name { get; set; } = default!;

        public int Status { get; set; }
        public DateTime? CreatedOnDate { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }
}
