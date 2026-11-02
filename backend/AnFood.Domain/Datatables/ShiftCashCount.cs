using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("ShiftCashCount")]
    public class ShiftCashCount
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ShiftId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Denomination { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime? CreatedOnDate { get; set; }
        [MaxLength(50)]
        public string? CreatedByUserCode { get; set; }
        [MaxLength(255)]
        public string? CreatedByUserName { get; set; }
    }
}
