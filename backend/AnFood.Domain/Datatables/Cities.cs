using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("Cities")]
    public class Cities
    {
        [Key]
        public Guid Id { get; set; }
        [MaxLength(50)]
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsDeleted { get; set; }
        public DateTime? CreatedOnDate { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }
}
