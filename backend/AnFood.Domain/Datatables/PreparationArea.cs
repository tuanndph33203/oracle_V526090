using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("PreparationArea")]
    public class PreparationArea
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = default!;

        public int Type { get; set; } // 1=Bếp, 2=Bar, 3=Cả hai

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = default!;

        public int DeviceType { get; set; } // 1=Máy tính bảng, 2=Máy in, 3=Không sử dụng

        public int? LabelPrintTiming { get; set; } // 1=Sau khi gửi, 2=Sau khi trả món

        public string? Description { get; set; }

        public string? ProcessingArea { get; set; }

        public int Status { get; set; } = 1; // 1 = Active, 2 = Inactive, 0 = Deleted

        public DateTime? CreatedOnDate { get; set; }
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
        public DateTime? UpdatedOnDate { get; set; }
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }
}
