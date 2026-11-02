using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("KitchenRequest")]
    public class KitchenRequest
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string RequestCode { get; set; } = default!;

        [Required]
        public Guid OrderId { get; set; }

        [Required]
        [MaxLength(50)]
        public string OrderCode { get; set; } = default!;

        [Required]
        [MaxLength(50)]
        public string BranchCode { get; set; } = default!;

        [Required]
        public Guid KitchenAreaId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ServiceType { get; set; } = default!; // DineIn, TakeAway, Delivery

        public Guid? TableId { get; set; }

        [MaxLength(255)]
        public string? TableName { get; set; }

        [Required]
        public DateTime RequestedAt { get; set; }

        [MaxLength(255)]
        public string? RequestedBy { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Cancelled
    }
}
