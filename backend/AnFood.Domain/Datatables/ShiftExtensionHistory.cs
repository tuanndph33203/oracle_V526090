using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("ShiftExtensionHistory")]
    public class ShiftExtensionHistory
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ShiftId { get; set; }

        public int ExtendedMinutes { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        public DateTime? CreatedOnDate { get; set; }
        [MaxLength(50)]
        public string? CreatedByUserCode { get; set; }
        [MaxLength(255)]
        public string? CreatedByUserName { get; set; }
    }
}
