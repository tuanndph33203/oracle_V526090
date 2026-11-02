using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("Shift")]
    public class Shift
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string BranchCode { get; set; } = default!;

        [MaxLength(100)]
        public string? MachineCode { get; set; }

        [Required]
        [MaxLength(50)]
        public string CashierId { get; set; } = default!;

        [Required]
        [MaxLength(255)]
        public string CashierName { get; set; } = default!;

        public Guid ShiftId { get; set; }

        [Required]
        [MaxLength(255)]
        public string ShiftName { get; set; } = default!;

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public int ExtendedMinutes { get; set; }

        [Required]
        public DateTime OpenTime { get; set; }

        public DateTime? CloseTime { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningCash { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ExpectedCash { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualCash { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DifferenceCash { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TransferCash { get; set; }

        public Guid? ReceiverEmployeeId { get; set; }

        public int Status { get; set; } = 1; // 1 = Open, 2 = Closed, 0 = Deleted

        public DateTime? CreatedOnDate { get; set; }
        [MaxLength(50)]
        public string? CreatedByUserCode { get; set; }
        [MaxLength(255)]
        public string? CreatedByUserName { get; set; }

        public DateTime? UpdatedOnDate { get; set; }
        [MaxLength(50)]
        public string? UpdatedByUserCode { get; set; }
        [MaxLength(255)]
        public string? UpdatedByUserName { get; set; }
    }
}
