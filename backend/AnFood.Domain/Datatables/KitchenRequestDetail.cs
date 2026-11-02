using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("KitchenRequestDetail")]
    public class KitchenRequestDetail
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid KitchenRequestId { get; set; }

        [Required]
        public Guid OrderDetailId { get; set; } // SalesOrderItem.Id

        [Required]
        public Guid MenuItemId { get; set; }

        [Required]
        [MaxLength(255)]
        public string MenuItemName { get; set; } = default!;

        [Required]
        public double RequestQty { get; set; }

        [Required]
        public double ServedQty { get; set; }

        [Required]
        public double SoldOutQty { get; set; }

        [Required]
        public double RemainingQty { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, PartialServed, Served, SoldOut, Completed
    }
}
